using System.Text.Json;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class LocalUserAdministration(
    IdentityDbContext dbContext,
    UserManager<IamUsuario> userManager,
    RoleManager<IamRol> roleManager) : ILocalUserAdministration
{
    public async Task<LocalUserPage> ListAsync(LocalUserListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 5, 50);
        var users = dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user => user.UserName!.Contains(search));
        }
        if (query.IsActive.HasValue) users = users.Where(user => user.IsActive == query.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(query.RoleCode))
            users = users.Where(user => dbContext.UserRoles.Any(link => link.UserId == user.Id && link.Role.Code == query.RoleCode));

        var total = await users.CountAsync(cancellationToken);
        var rows = await users.OrderBy(user => user.UserName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new { user.Id, user.UserName, user.IsActive, user.ValidFromUtc, user.ValidUntilUtc })
            .ToListAsync(cancellationToken);
        var ids = rows.Select(row => row.Id).ToArray();
        var roleLinks = await dbContext.UserRoles.AsNoTracking().Include(link => link.Role).Where(link => ids.Contains(link.UserId)).ToListAsync(cancellationToken);
        var result = rows.Select(row => ToRow(row.Id, row.UserName!, row.IsActive, row.ValidFromUtc, row.ValidUntilUtc, roleLinks.Where(link => link.UserId == row.Id).Select(link => new LocalUserRole(link.RoleId, link.Role.Code, link.Role.Name)).ToArray())).ToArray();
        return new LocalUserPage(result, page, pageSize, total);
    }

    public async Task<LocalUserDetail?> FindAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return null;
        var roles = await dbContext.UserRoles.AsNoTracking().Include(link => link.Role).Where(link => link.UserId == userId).Select(link => new LocalUserRole(link.RoleId, link.Role.Code, link.Role.Name)).ToArrayAsync(cancellationToken);
        return new LocalUserDetail(user.Id, user.UserName!, user.IsActive, user.ValidFromUtc, user.ValidUntilUtc, roles);
    }

    public async Task<IReadOnlyList<LocalRoleOption>> GetRolesAsync(CancellationToken cancellationToken = default) => await dbContext.BusinessRoles.AsNoTracking().OrderBy(role => role.Name).Select(role => new LocalRoleOption(role.Code, role.Name)).ToListAsync(cancellationToken);

    public async Task<Guid> CreateAsync(CreateLocalUserCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command.UserName, command.Password, command.Justification, command.CorrelationId);
        var user = new IamUsuario { UserName = command.UserName.Trim(), IsActive = true, ValidFromUtc = command.ValidFromUtc, ValidUntilUtc = command.ValidUntilUtc };
        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded) throw new IdentityValidationException(result.Errors.Select(error => error.Code));
        AddAudit(command.ActorUserId, user.Id, "UserCreated", command.Justification, command.CorrelationId, new { user.UserName, user.IsActive });
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task UpdateAsync(UpdateLocalUserCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command.UserName, command.Justification, command.CorrelationId);
        var user = await userManager.FindByIdAsync(command.UserId.ToString()) ?? throw new KeyNotFoundException("El usuario no existe.");
        var result = await userManager.SetUserNameAsync(user, command.UserName.Trim());
        if (!result.Succeeded) throw new IdentityValidationException(result.Errors.Select(error => error.Code));
        user.ValidFromUtc = command.ValidFromUtc; user.ValidUntilUtc = command.ValidUntilUtc;
        AddAudit(command.ActorUserId, user.Id, "UserUpdated", command.Justification, command.CorrelationId, new { user.UserName, user.ValidFromUtc, user.ValidUntilUtc });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid userId, bool active, Guid actorUserId, string justification, string correlationId, CancellationToken cancellationToken = default)
    {
        Validate(justification, correlationId);
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new KeyNotFoundException("El usuario no existe.");
        user.IsActive = active;
        await userManager.UpdateSecurityStampAsync(user);
        AddAudit(actorUserId, user.Id, active ? "UserActivated" : "UserDeactivated", justification, correlationId, new { user.IsActive });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ResetPasswordAsync(ResetLocalUserPasswordCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command.Password, command.Justification, command.CorrelationId);
        var user = await userManager.FindByIdAsync(command.UserId.ToString()) ?? throw new KeyNotFoundException("El usuario no existe.");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, command.Password);
        if (!result.Succeeded) return result.Errors.Select(error => error.Code).Distinct(StringComparer.Ordinal).ToArray();
        AddAudit(command.ActorUserId, user.Id, "LocalPasswordReset", command.Justification, command.CorrelationId, new { CredentialUpdated = true });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Array.Empty<string>();
    }

    public async Task ChangeRoleAsync(ChangeLocalUserRoleCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command.RoleCode, command.Justification, command.CorrelationId);
        var user = await userManager.FindByIdAsync(command.UserId.ToString()) ?? throw new KeyNotFoundException("El usuario no existe.");
        var role = await roleManager.FindByNameAsync(command.RoleCode.ToUpperInvariant()) ?? throw new KeyNotFoundException("El rol no existe.");
        var link = await dbContext.UserRoles.SingleOrDefaultAsync(item => item.UserId == user.Id && item.RoleId == role.Id, cancellationToken);
        if (command.Assign && link is null) dbContext.UserRoles.Add(new IamUsuarioRol { UserId = user.Id, RoleId = role.Id, RequestedByUserId = command.ActorUserId, ApprovedByUserId = command.ActorUserId, ExecutedByUserId = command.ActorUserId, Justification = command.Justification });
        if (!command.Assign && link is not null) dbContext.UserRoles.Remove(link);
        await userManager.UpdateSecurityStampAsync(user);
        AddAudit(command.ActorUserId, user.Id, command.Assign ? "RoleAssigned" : "RoleRemoved", command.Justification, command.CorrelationId, new { Role = role.Code, command.Assign });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocalUserAuditRow>> GetAuditAsync(Guid userId, CancellationToken cancellationToken = default) => await dbContext.AuthorizationAuditEvents.AsNoTracking().Where(item => item.BeneficiaryUserId == userId).OrderByDescending(item => item.OccurredAtUtc).Select(item => new LocalUserAuditRow(item.OccurredAtUtc, item.EventType, item.Result, item.ActorUserId, item.Justification)).ToListAsync(cancellationToken);

    private void AddAudit(Guid actor, Guid beneficiary, string type, string justification, string correlation, object after) => dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion { ActorUserId = actor, BeneficiaryUserId = beneficiary, EventType = type, Result = "Succeeded", ResourceType = nameof(IamUsuario), ResourceId = beneficiary.ToString(), AfterJson = JsonSerializer.Serialize(after), Justification = justification, CorrelationId = correlation });
    private static LocalUserRow ToRow(Guid id, string name, bool active, DateTime? from, DateTime? until, IReadOnlyList<LocalUserRole> roles) => new(id, name, active, from, until, roles);
    private static void Validate(params string[] values) { foreach (var value in values) ArgumentException.ThrowIfNullOrWhiteSpace(value); }
}