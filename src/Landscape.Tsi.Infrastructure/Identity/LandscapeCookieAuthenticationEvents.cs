using System.Security.Claims;

using Landscape.Tsi.Domain.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class LandscapeCookieAuthenticationEvents(
    UserManager<IamUsuario> userManager,
    IUserClaimsPrincipalFactory<IamUsuario> principalFactory) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = Guid.TryParse(userId, out var parsedUserId)
            ? await userManager.FindByIdAsync(parsedUserId.ToString())
            : null;

        if (user is null || !user.CanAuthenticateAt(DateTime.UtcNow))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
            return;
        }

        var authenticationMethod = context.Principal?.FindFirstValue(
            Landscape.Tsi.Application.Identity.CustomClaimTypes.AuthenticationMethod);
        var refreshed = await principalFactory.CreateAsync(user);
        if (!string.IsNullOrWhiteSpace(authenticationMethod) && refreshed.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(
                Landscape.Tsi.Application.Identity.CustomClaimTypes.AuthenticationMethod,
                authenticationMethod));
        }

        context.ReplacePrincipal(refreshed);
        context.ShouldRenew = true;
    }
}