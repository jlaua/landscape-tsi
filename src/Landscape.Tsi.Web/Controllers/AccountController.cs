using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Landscape.Tsi.Web.Controllers;

public sealed class AccountController(
    SignInManager<IamUsuario> signInManager,
    UserManager<IamUsuario> userManager,
    IAuthenticationAuditWriter auditWriter,
    IConfiguration configuration) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel
    {
        ReturnUrl = returnUrl,
        OAuthEnabled = configuration.GetValue<bool>("Authentication:OAuth:Enabled")
    });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        model.OAuthEnabled = configuration.GetValue<bool>("Authentication:OAuth:Enabled");
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByNameAsync(model.UserName);
        var result = user is { IsActive: true }
            ? await signInManager.PasswordSignInAsync(user, model.Password, false, lockoutOnFailure: true)
            : Microsoft.AspNetCore.Identity.SignInResult.Failed;

        await auditWriter.WriteAsync("Login", "Local", result.Succeeded ? "Succeeded" : "Failed",
            model.UserName, HttpContext.TraceIdentifier, cancellationToken);

        if (result.Succeeded)
        {
            return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
        }

        ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult CorporateLogin(string? returnUrl = null)
    {
        if (!configuration.GetValue<bool>("Authentication:OAuth:Enabled"))
        {
            return NotFound();
        }

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCompleted), new { returnUrl = SafeReturnUrl(returnUrl) })
        }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> OAuthCompleted(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var succeeded = User.Identity?.IsAuthenticated == true;
        await auditWriter.WriteAsync("Login", "OAuth", succeeded ? "Succeeded" : "Failed",
            User.Identity?.Name, HttpContext.TraceIdentifier, cancellationToken);
        return succeeded ? LocalRedirect(SafeReturnUrl(returnUrl)) : RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var federated = User.HasClaim(CustomClaimTypes.AuthenticationMethod, "oidc");
        await signInManager.SignOutAsync();
        if (federated && configuration.GetValue<bool>("Authentication:OAuth:Enabled"))
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = Url.Action(nameof(Login)) },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Home")!;
}