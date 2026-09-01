using System.Security.Claims;

namespace Landscape.Tsi.Application.Identity;

/// <summary>
/// Public boundary used by application modules to query identity capabilities.
/// Persistence details remain owned by the Identity and Access infrastructure.
/// </summary>
public interface IIdentityAccessService
{
    bool HasPermission(ClaimsPrincipal principal, string permission);
}
