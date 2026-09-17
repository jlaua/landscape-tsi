using System.Text.Json;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class OrganizationScopeAdministrationService(IdentityDbContext dbContext) : IOrganizationScopeAdministration
{
    public async Task<IReadOnlyList<OrganizationScopeRow>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.UserOrganizations.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ValidFromUtc)
            .Select(x => new OrganizationScopeRow(x.Id, x.IsCorporateScope, x.EmpresaSubsidiariaId, x.ValidFromUtc, x.ValidUntilUtc, x.ApprovedByUserId, x.Justification))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrganizationScopeApprover>> ListApproversAsync(Guid beneficiaryUserId, CancellationToken cancellationToken = default)
    {
        var roleId = await dbContext.BusinessRoles.Where(x => x.Code == SystemRoles.AdministratorCode).Select(x => x.Id).SingleAsync(cancellationToken);
        return await dbContext.UserRoles.AsNoTracking()
            .Where(x => x.RoleId == roleId && x.UserId != beneficiaryUserId && x.User.IsActive)
            .OrderBy(x => x.User.UserName).Select(x => new OrganizationScopeApprover(x.UserId, x.User.UserName!)).ToListAsync(cancellationToken);
    }

    public async Task AssignCorporateAsync(AssignCorporateScopeCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command.Justification, command.CorrelationId);
        if (command.UserId == command.ApprovedByUserId) throw new InvalidOperationException("El beneficiario no puede aprobar su propio alcance.");
        if (command.ValidUntilUtc is not null && command.ValidUntilUtc <= command.ValidFromUtc) throw new InvalidOperationException("La vigencia del alcance no es válida.");

        var beneficiary = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == command.UserId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("El usuario beneficiario no existe o está inactivo.");
        var approverRoleId = await dbContext.BusinessRoles.Where(x => x.Code == SystemRoles.AdministratorCode).Select(x => x.Id).SingleAsync(cancellationToken);
        var approverIsAdmin = await dbContext.UserRoles.AnyAsync(x => x.UserId == command.ApprovedByUserId && x.RoleId == approverRoleId, cancellationToken);
        if (!approverIsAdmin) throw new InvalidOperationException("El aprobador debe tener SYSTEM_ADMINISTRATOR.");

        var existing = await dbContext.UserOrganizations.SingleOrDefaultAsync(x => x.UserId == command.UserId && x.IsCorporateScope && x.ValidUntilUtc == null, cancellationToken);
        if (existing is not null) return;

        var scope = new IamUsuarioOrganizacion { UserId = beneficiary.Id, IsCorporateScope = true, EmpresaSubsidiariaId = null, ValidFromUtc = command.ValidFromUtc, ValidUntilUtc = command.ValidUntilUtc, ApprovedByUserId = command.ApprovedByUserId, Justification = command.Justification };
        scope.Validate();
        dbContext.UserOrganizations.Add(scope);
        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = command.RequestedByUserId,
            BeneficiaryUserId = command.UserId,
            ApprovedByUserId = command.ApprovedByUserId,
            EventType = "OrganizationScopeAssigned",
            Result = "Succeeded",
            ResourceType = nameof(IamUsuarioOrganizacion),
            ResourceId = scope.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { scope.IsCorporateScope, scope.ValidFromUtc, scope.ValidUntilUtc }),
            Justification = command.Justification,
            CorrelationId = command.CorrelationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(Guid scopeId, Guid executedByUserId, string justification, string correlationId, CancellationToken cancellationToken = default)
    {
        Validate(justification, correlationId);
        var scope = await dbContext.UserOrganizations.SingleOrDefaultAsync(x => x.Id == scopeId, cancellationToken) ?? throw new KeyNotFoundException("El alcance no existe.");
        if (scope.ValidUntilUtc is not null && scope.ValidUntilUtc <= DateTime.UtcNow) return;
        scope.ValidUntilUtc = DateTime.UtcNow;
        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion { ActorUserId = executedByUserId, BeneficiaryUserId = scope.UserId, ApprovedByUserId = scope.ApprovedByUserId, EventType = "OrganizationScopeRevoked", Result = "Succeeded", ResourceType = nameof(IamUsuarioOrganizacion), ResourceId = scope.Id.ToString(), Justification = justification, CorrelationId = correlationId, AfterJson = JsonSerializer.Serialize(new { scope.ValidUntilUtc }) });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(string justification, string correlationId) { ArgumentException.ThrowIfNullOrWhiteSpace(justification); ArgumentException.ThrowIfNullOrWhiteSpace(correlationId); }
}