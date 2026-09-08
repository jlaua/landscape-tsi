using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class AuthorizationAuditQuery(
    IdentityDbContext dbContext,
    IEffectiveAccessService effectiveAccess) : IAuthorizationAuditQuery
{
    public async Task<IReadOnlyList<AuthorizationAuditSummary>> ListAsync(
        Guid actorUserId,
        int empresaSubsidiariaId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (!await effectiveAccess.IsAuthorizedAsync(
            actorUserId, Permissions.AuditView, empresaSubsidiariaId, cancellationToken))
        {
            throw new UnauthorizedAccessException("No autorizado.");
        }

        var events = await dbContext.AuthorizationAuditEvents.AsNoTracking()
            .Where(item => item.EmpresaSubsidiariaId == empresaSubsidiariaId)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Select(item => new AuthorizationAuditSummary(
                item.Id, item.OccurredAtUtc, item.EventType, item.Result,
                item.ActorUserId, item.EmpresaSubsidiariaId))
            .ToListAsync(cancellationToken);

        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = actorUserId,
            EmpresaSubsidiariaId = empresaSubsidiariaId,
            EventType = "AuditViewed",
            PermissionCode = Permissions.AuditView,
            Result = "Succeeded",
            ResourceType = nameof(IamEventoAuditoriaAutorizacion),
            Justification = "Consulta autorizada dentro del alcance.",
            CorrelationId = correlationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return events;
    }

    public async Task<AuditPage> SearchAsync(
        Guid actorUserId,
        AuditFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (filter.EmpresaSubsidiariaId.HasValue && !await effectiveAccess.IsAuthorizedAsync(
            actorUserId, Permissions.AuditView, filter.EmpresaSubsidiariaId.Value, cancellationToken))
        {
            throw new UnauthorizedAccessException("No autorizado.");
        }
        var query = ApplyScope(dbContext.AuthorizationAuditEvents.AsNoTracking(), actorUserId, filter.EmpresaSubsidiariaId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(item => (item.EventType ?? "").Contains(search) ||
                (item.ResourceType ?? "").Contains(search) ||
                (item.ResourceId ?? "").Contains(search) ||
                (item.Justification ?? "").Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = filter.Action switch
            {
                "CREATE" => query.Where(item => item.EventType.Contains("Created")),
                "UPDATE" => query.Where(item => item.EventType.Contains("Updated")),
                "DELETE" => query.Where(item => item.EventType.Contains("Deleted")),
                "RESTORE" => query.Where(item => item.EventType.Contains("Restored") || item.EventType == "RESTORE"),
                "LOGIN" => query.Where(item => item.EventType == "Login"),
                "LOGIN_FAILED" => query.Where(item => item.EventType == "LoginFailed"),
                "LOGOUT" => query.Where(item => item.EventType == "Logout"),
                "AUTHORIZATION_DENIED" => query.Where(item => item.EventType == "AuthorizationDenied"),
                "RELATION_ADD" => query.Where(item => item.EventType.Contains("Associated")),
                "RELATION_REMOVE" => query.Where(item => item.EventType.Contains("Disassociated")),
                _ => query.Where(item => false)
            };
        }
        if (!string.IsNullOrWhiteSpace(filter.Entity)) query = query.Where(item => item.ResourceType == filter.Entity);
        if (filter.ActorUserId.HasValue) query = query.Where(item => item.ActorUserId == filter.ActorUserId);
        if (filter.FromUtc.HasValue) query = query.Where(item => item.OccurredAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) query = query.Where(item => item.OccurredAtUtc < filter.ToUtc.Value);

        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 100);
        var authorizationItems = await query
            .Select(item => new AuditEventRow(
                item.Id, item.OccurredAtUtc, item.EventType, item.Result,
                item.ResourceType, item.ResourceId, item.Justification,
                item.ActorUserId, null,
                item.EmpresaSubsidiariaId, 1, item.EventType == "DELETE"))
            .ToListAsync(cancellationToken);
        var operations = ApplyOperationScope(dbContext.AuditOperations.AsNoTracking(), actorUserId, filter.EmpresaSubsidiariaId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            operations = operations.Where(item => (item.ActionType ?? "").Contains(search) ||
                (item.EntityCode ?? "").Contains(search) || (item.PhysicalTableName ?? "").Contains(search) ||
                (item.RootDisplayName ?? "").Contains(search) || (item.Description ?? "").Contains(search) ||
                (item.CorrelationId ?? "").Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(filter.Action)) operations = operations.Where(item => item.ActionType == filter.Action);
        if (!string.IsNullOrWhiteSpace(filter.Entity)) operations = operations.Where(item => item.PhysicalTableName == filter.Entity || item.EntityCode == filter.Entity);
        if (filter.ActorUserId.HasValue) operations = operations.Where(item => item.ActorUserId == filter.ActorUserId);
        if (filter.FromUtc.HasValue) operations = operations.Where(item => item.OccurredAtUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) operations = operations.Where(item => item.OccurredAtUtc < filter.ToUtc.Value);
        var operationItems = await operations.Select(item => new AuditEventRow(
            0, item.OccurredAtUtc, item.ActionType, item.Status, item.PhysicalTableName ?? item.EntityCode,
            item.RootRecordId.HasValue ? item.RootRecordId.Value.ToString() : null, item.RootDisplayName,
            item.ActorUserId, item.ActorUserNameSnapshot, item.EmpresaSubsidiariaId,
            item.AffectedRecordCount, item.ActionType == "DELETE") { OperationId = item.OperationId }).ToListAsync(cancellationToken);
        var merged = authorizationItems.Concat(operationItems).OrderByDescending(item => item.OccurredAtUtc).ToArray();
        var total = merged.Length;
        var items = merged.Skip((page - 1) * size).Take(size).ToList();
        var actorIds = items.Where(x => x.ActorUserId.HasValue && string.IsNullOrWhiteSpace(x.ActorUserName))
            .Select(x => x.ActorUserId!.Value).Distinct().ToArray();
        var names = await dbContext.Users.AsNoTracking().Where(x => actorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UserName, cancellationToken);
        items = items.Select(x => x.ActorUserId.HasValue && names.TryGetValue(x.ActorUserId.Value, out var name)
            ? x with { ActorUserName = name } : x).ToList();
        return new AuditPage(items, total, page, size);
    }

    private IQueryable<AuditOperation> ApplyOperationScope(IQueryable<AuditOperation> query, Guid actorUserId, int? subsidiaryId)
    {
        var scopes = dbContext.UserOrganizations.AsNoTracking()
            .Where(x => x.UserId == actorUserId && x.ValidFromUtc <= DateTime.UtcNow &&
                (x.ValidUntilUtc == null || x.ValidUntilUtc > DateTime.UtcNow));
        if (subsidiaryId.HasValue)
            return query.Where(x => x.EmpresaSubsidiariaId == subsidiaryId.Value);
        return query.Where(x => x.EmpresaSubsidiariaId == null ||
            scopes.Any(scope => scope.IsCorporateScope || scope.EmpresaSubsidiariaId == x.EmpresaSubsidiariaId));
    }

    public async Task<AuditEventRow?> GetAsync(Guid actorUserId, long eventId, CancellationToken cancellationToken = default)
    {
        var item = await ApplyScope(dbContext.AuthorizationAuditEvents.AsNoTracking(), actorUserId, null)
            .Where(x => x.Id == eventId)
            .Select(item => new AuditEventRow(
                item.Id, item.OccurredAtUtc, item.EventType, item.Result,
                item.ResourceType, item.ResourceId, item.Justification,
                item.ActorUserId, null,
                item.EmpresaSubsidiariaId, 1, item.EventType == "DELETE"))
            .SingleOrDefaultAsync(cancellationToken);
        if (item?.ActorUserId is not Guid actorId) return item;
        var actor = await dbContext.Users.AsNoTracking().Where(x => x.Id == actorId).Select(x => x.UserName).SingleOrDefaultAsync(cancellationToken);
        return item with { ActorUserName = actor };
    }

    public async Task<AuditEventRow?> GetOperationAsync(Guid actorUserId, Guid operationId, CancellationToken cancellationToken = default)
    {
        var item = await ApplyOperationScope(dbContext.AuditOperations.AsNoTracking(), actorUserId, null)
            .Where(operation => operation.OperationId == operationId)
            .Select(operation => new AuditEventRow(0, operation.OccurredAtUtc, operation.ActionType, operation.Status,
                operation.PhysicalTableName ?? operation.EntityCode,
                operation.RootRecordId.HasValue ? operation.RootRecordId.Value.ToString() : null,
                operation.RootDisplayName, operation.ActorUserId, operation.ActorUserNameSnapshot,
                operation.EmpresaSubsidiariaId, operation.AffectedRecordCount, operation.ActionType == "DELETE")
            { OperationId = operation.OperationId })
            .SingleOrDefaultAsync(cancellationToken);
        return item;
    }

    public async Task<AuditFilterOptions> GetFilterOptionsAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var authorizationActorIds = await ApplyScope(dbContext.AuthorizationAuditEvents.AsNoTracking(), actorUserId, null)
            .Where(item => item.ActorUserId.HasValue)
            .Select(item => item.ActorUserId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var operationActorIds = await ApplyOperationScope(dbContext.AuditOperations.AsNoTracking(), actorUserId, null)
            .Where(item => item.ActorUserId.HasValue)
            .Select(item => item.ActorUserId!.Value)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var actorIds = authorizationActorIds.Concat(operationActorIds).Distinct().ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .OrderBy(user => user.UserName)
            .Select(user => new AuditFilterOption(user.Id.ToString(), user.UserName ?? user.Id.ToString()))
            .ToListAsync(cancellationToken);

        var scopes = await dbContext.UserOrganizations.AsNoTracking()
            .Where(item => item.UserId == actorUserId && item.ValidFromUtc <= DateTime.UtcNow &&
                (item.ValidUntilUtc == null || item.ValidUntilUtc > DateTime.UtcNow))
            .Select(item => new { item.IsCorporateScope, item.EmpresaSubsidiariaId })
            .ToListAsync(cancellationToken);
        var corporate = scopes.Any(scope => scope.IsCorporateScope);
        var allowedSubsidiaries = scopes.Where(scope => scope.EmpresaSubsidiariaId.HasValue)
            .Select(scope => scope.EmpresaSubsidiariaId!.Value).ToHashSet();
        var subsidiaryQuery = dbContext.Set<EmpresaSubsidiariaReference>().AsNoTracking();
        if (!corporate)
            subsidiaryQuery = subsidiaryQuery.Where(item => allowedSubsidiaries.Contains(item.Id));
        var subsidiaries = await subsidiaryQuery.OrderBy(item => item.Name)
            .Select(item => new AuditFilterOption(item.Id.ToString(), item.Name ?? item.Id.ToString()))
            .ToListAsync(cancellationToken);

        return new AuditFilterOptions(AuditEntityRegistry.VisibleEntities, users, subsidiaries);
    }

    private IQueryable<IamEventoAuditoriaAutorizacion> ApplyScope(
        IQueryable<IamEventoAuditoriaAutorizacion> query,
        Guid actorUserId,
        int? subsidiaryId)
    {
        var scopes = dbContext.UserOrganizations.AsNoTracking()
            .Where(x => x.UserId == actorUserId && x.ValidFromUtc <= DateTime.UtcNow &&
                (x.ValidUntilUtc == null || x.ValidUntilUtc > DateTime.UtcNow));
        if (subsidiaryId.HasValue)
            return query.Where(x => x.EmpresaSubsidiariaId == subsidiaryId.Value);
        return query.Where(x => x.EmpresaSubsidiariaId == null ||
            scopes.Any(scope => scope.IsCorporateScope || scope.EmpresaSubsidiariaId == x.EmpresaSubsidiariaId));
    }

}
