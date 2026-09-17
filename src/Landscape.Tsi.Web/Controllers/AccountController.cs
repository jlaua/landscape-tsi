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
    public const string ThemeCookieName = "landscape_theme";
    public const string ThemeLight = "light";
    public const string ThemeCorporate = "corporate";

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var currentTheme = Request.Cookies[ThemeCookieName] == ThemeCorporate ? ThemeCorporate : ThemeLight;
        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            OAuthEnabled = configuration.GetValue<bool>("Authentication:OAuth:Enabled"),
            SelectedTheme = currentTheme
        });
    }

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
            if (!string.IsNullOrWhiteSpace(model.SelectedTheme))
            {
                var themeToPersist = model.SelectedTheme.Trim().ToLowerInvariant() == ThemeCorporate ? ThemeCorporate : ThemeLight;
                Response.Cookies.Append(ThemeCookieName, themeToPersist, new CookieOptions
                {
                    Path = "/",
                    MaxAge = TimeSpan.FromDays(365),
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    HttpOnly = false
                });
            }

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

    [HttpPost("Account/SetTheme")]
    [HttpPost("SetTheme")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult SetTheme([FromBody] SetThemeRequest? request)
    {
        var rawTheme = request?.Theme?.Trim().ToLowerInvariant()
                       ?? Request.Form["theme"].FirstOrDefault()?.Trim().ToLowerInvariant()
                       ?? ThemeLight;

        var validatedTheme = rawTheme == ThemeCorporate ? ThemeCorporate : ThemeLight;

        Response.Cookies.Append(ThemeCookieName, validatedTheme, new CookieOptions
        {
            Path = "/",
            MaxAge = TimeSpan.FromDays(365),
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            HttpOnly = false
        });

        if (Request.Headers.Accept.ToString().Contains("application/json") || Request.ContentType?.Contains("application/json") == true)
        {
            return Json(new { success = true, theme = validatedTheme });
        }

        var returnUrl = request?.ReturnUrl ?? Request.Form["returnUrl"].FirstOrDefault();
        return LocalRedirect(SafeReturnUrl(returnUrl));
    }

    [HttpGet("Account/Preferences")]
    [HttpGet("Account/Preferencias")]
    [HttpGet("Preferencias")]
    [Authorize]
    public IActionResult Preferences()
    {
        var currentTheme = Request.Cookies[ThemeCookieName] == ThemeCorporate ? ThemeCorporate : ThemeLight;
        return View(new AccountPreferencesViewModel
        {
            UserName = User.Identity?.Name ?? string.Empty,
            SelectedTheme = currentTheme,
            StatusMessage = TempData["StatusMessage"] as string
        });
    }

    [HttpPost("Account/Preferences")]
    [HttpPost("Account/Preferencias")]
    [HttpPost("Preferencias")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult Preferences(AccountPreferencesViewModel model)
    {
        var validatedTheme = model.SelectedTheme?.Trim().ToLowerInvariant() == ThemeCorporate
            ? ThemeCorporate
            : ThemeLight;

        Response.Cookies.Append(ThemeCookieName, validatedTheme, new CookieOptions
        {
            Path = "/",
            MaxAge = TimeSpan.FromDays(365),
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            HttpOnly = false
        });

        TempData["StatusMessage"] = $"Preferencia de tema visual actualizada a '{(validatedTheme == ThemeCorporate ? "Corporativo (Credicorp)" : "Ligero")}'.";
        return RedirectToAction(nameof(Preferences));
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action("Index", "Home")!;
}