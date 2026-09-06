using Landscape.Tsi.Application.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class EffectiveAccessService(IdentityDbContext dbContext) : IEffectiveAccessService
{
    public async Task<bool> IsAuthorizedAsync(
        Guid userId,
        string permission,
        int empresaSubsidiariaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        var utcNow = DateTime.UtcNow;

        var hasPermission = await dbContext.UserRoles
            .Where(assignment => assignment.UserId == userId)
            .Where(assignment => assignment.User.IsActive)
            .Where(assignment => assignment.User.ValidFromUtc == null || assignment.User.ValidFromUtc <= utcNow)
            .Where(assignment => assignment.User.ValidUntilUtc == null || assignment.User.ValidUntilUtc > utcNow)
            .Where(assignment => assignment.ValidFromUtc <= utcNow)
            .Where(assignment => assignment.ValidUntilUtc == null || assignment.ValidUntilUtc > utcNow)
            .AnyAsync(assignment => assignment.Role.Permissions.Any(rolePermission =>
                rolePermission.Permission.Code == permission), cancellationToken);
        if (!hasPermission)
        {
            return false;
        }

        return await dbContext.UserOrganizations
            .Where(scope => scope.UserId == userId)
            .Where(scope => scope.ValidFromUtc <= utcNow)
            .Where(scope => scope.ValidUntilUtc == null || scope.ValidUntilUtc > utcNow)
            .AnyAsync(scope => scope.IsCorporateScope || scope.EmpresaSubsidiariaId == empresaSubsidiariaId, cancellationToken);
    }
}
