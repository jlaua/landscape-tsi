using System.Security.Claims;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class LandscapeClaimsPrincipalFactory(
    UserManager<IamUsuario> userManager,
    IOptions<IdentityOptions> options,
    IdentityDbContext dbContext)
    : UserClaimsPrincipalFactory<IamUsuario>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(IamUsuario user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var roles = await dbContext.UserRoles
            .Where(x => x.UserId == user.Id)
            .Select(x => new { x.Role.Name, Permissions = x.Role.Permissions.Select(p => p.Permission.Code) })
            .ToListAsync();

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
            foreach (var permission in role.Permissions.Distinct(StringComparer.Ordinal))
            {
                identity.AddClaim(new Claim(CustomClaimTypes.Permission, permission));
            }
        }

        return identity;
    }
}