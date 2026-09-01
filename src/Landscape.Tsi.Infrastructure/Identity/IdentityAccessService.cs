using System.Security.Claims;

using Landscape.Tsi.Application.Identity;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class IdentityAccessService : IIdentityAccessService
{
    public bool HasPermission(ClaimsPrincipal principal, string permission) =>
        principal.Identity?.IsAuthenticated == true &&
        principal.HasClaim(CustomClaimTypes.Permission, permission);
}
