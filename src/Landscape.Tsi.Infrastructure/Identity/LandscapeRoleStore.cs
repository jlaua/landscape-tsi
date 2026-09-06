using Landscape.Tsi.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class LandscapeRoleStore(IdentityDbContext dbContext) : IRoleStore<IamRol>
{
    public void Dispose() { }
    public async Task<IdentityResult> CreateAsync(IamRol role, CancellationToken cancellationToken) { dbContext.BusinessRoles.Add(role); await dbContext.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
    public async Task<IdentityResult> UpdateAsync(IamRol role, CancellationToken cancellationToken) { dbContext.BusinessRoles.Update(role); await dbContext.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
    public async Task<IdentityResult> DeleteAsync(IamRol role, CancellationToken cancellationToken) { dbContext.BusinessRoles.Remove(role); await dbContext.SaveChangesAsync(cancellationToken); return IdentityResult.Success; }
    public Task<string> GetRoleIdAsync(IamRol role, CancellationToken cancellationToken) => Task.FromResult(role.Id.ToString());
    public Task<string?> GetRoleNameAsync(IamRol role, CancellationToken cancellationToken) => Task.FromResult<string?>(role.Name);
    public Task SetRoleNameAsync(IamRol role, string? roleName, CancellationToken cancellationToken) { role.Name = roleName ?? string.Empty; return Task.CompletedTask; }
    public Task<string?> GetNormalizedRoleNameAsync(IamRol role, CancellationToken cancellationToken) => Task.FromResult<string?>(role.Code.ToUpperInvariant());
    public Task SetNormalizedRoleNameAsync(IamRol role, string? normalizedName, CancellationToken cancellationToken) { return Task.CompletedTask; }
    public Task<IamRol?> FindByIdAsync(string roleId, CancellationToken cancellationToken) => Guid.TryParse(roleId, out var id) ? dbContext.BusinessRoles.SingleOrDefaultAsync(role => role.Id == id, cancellationToken) : Task.FromResult<IamRol?>(null);
    public Task<IamRol?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) => dbContext.BusinessRoles.SingleOrDefaultAsync(role => role.Code == normalizedRoleName, cancellationToken);
}
