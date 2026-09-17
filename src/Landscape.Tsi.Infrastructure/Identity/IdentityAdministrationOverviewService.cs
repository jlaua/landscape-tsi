using Landscape.Tsi.Application.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class IdentityAdministrationOverviewService(IdentityDbContext dbContext)
    : IIdentityAdministrationOverview
{
    public async Task<IdentityAdministrationOverview> GetAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users.AsNoTracking().OrderBy(user => user.UserName).ToListAsync(cancellationToken);
        var assignments = await dbContext.UserRoles.AsNoTracking()
            .Include(item => item.Role)
            .ToListAsync(cancellationToken);
        var scopes = await dbContext.UserOrganizations.AsNoTracking().ToListAsync(cancellationToken);
        var userRows = users.Select(user => new AdministrationUserRow(
            user.Id,
            user.UserName ?? string.Empty,
            user.IsActive,
            user.ValidFromUtc,
            user.ValidUntilUtc,
            assignments.Where(item => item.UserId == user.Id).Select(item => item.Role.Name).Order().ToArray(),
            scopes.Where(item => item.UserId == user.Id)
                .Select(item => item.IsCorporateScope ? "Corporativo" : $"Subsidiaria {item.EmpresaSubsidiariaId}")
                .Order().ToArray()))
            .ToArray();

        var roles = await dbContext.BusinessRoles.AsNoTracking()
            .Include(role => role.Permissions).ThenInclude(item => item.Permission)
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
        var roleRows = roles.Select(role => new AdministrationRoleRow(
            role.Name,
            role.Permissions.Select(item => item.Permission.Code).Order().ToArray()))
            .ToArray();

        return new IdentityAdministrationOverview(userRows, roleRows);
    }
}