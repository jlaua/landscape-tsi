using System.Net;
using System.Text.RegularExpressions;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class LoginPageTests
{
    [Fact]
    public async Task LoginPage_ShowsLocalFormWithoutSecret()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Usuario", html);
        Assert.Contains("name=\"Password\"", html);
        Assert.Contains("Ingresar", html);
        Assert.Contains("lang=\"es\"", html);
        Assert.Contains("href=\"#main-content\"", html);
        Assert.DoesNotContain("BootstrapAdmin:Password", html);
    }

    [Fact]
    public async Task ProtectedHome_RedirectsAnonymousUserToLogin()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task LocalLogin_BootstrapAdministrator_CreatesAuthenticatedSession()
    {
        var password = $"Aa1!{Guid.NewGuid():N}";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "true",
            ["BootstrapAdmin:Password"] = password
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = WebUtility.HtmlDecode(Regex.Match(loginPage,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
        await using (var verificationScope = factory.Services.CreateAsyncScope())
        {
            var manager = verificationScope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
            var bootstrapUser = await manager.FindByNameAsync("jean");
            Assert.NotNull(bootstrapUser);
            Assert.True(await manager.CheckPasswordAsync(bootstrapUser, password));
        }

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["ReturnUrl"] = "/",
            ["__RequestVerificationToken"] = token
        }));

        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Se esperaba redirección y se recibió {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        var home = await client.GetAsync("/");
        home.EnsureSuccessStatusCode();
        Assert.Contains("Cerrar sesión", await home.Content.ReadAsStringAsync());
        var administration = await client.GetAsync("/Administration");
        administration.EnsureSuccessStatusCode();

        await using var scope = factory.Services.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Contains(audit.AuthenticationEvents, x => x.EventType == "Login" && x.Result == "Succeeded");
    }

    [Fact]
    public async Task InvalidLocalLogin_UsesGenericMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = WebUtility.HtmlDecode(Regex.Match(loginPage,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = $"missing-{Guid.NewGuid():N}",
            ["Password"] = $"Aa1!{Guid.NewGuid():N}",
            ["__RequestVerificationToken"] = token
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Usuario o contrase&#xF1;a incorrectos.", html);
        Assert.Contains("aria-live=\"assertive\"", html);
    }

    [Fact]
    public async Task AccessDeniedPage_UsesTextualAccessibleMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Account/AccessDenied");

        Assert.Contains("<h1 id=\"denied-title\">Acceso denegado</h1>", html);
        Assert.Contains("No dispone de permisos", html);
        Assert.Contains("aria-labelledby=\"denied-title\"", html);
    }

    [Fact]
    public async Task LocalLogin_WithoutAntiforgeryToken_IsRejected()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "usuario",
            ["Password"] = $"Aa1!{Guid.NewGuid():N}"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LocalLogin_SetsSecureHttpOnlySameSiteCookie()
    {
        var password = $"Aa1!{Guid.NewGuid():N}";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "true",
            ["BootstrapAdmin:Password"] = password
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var (token, _) = await GetLoginTokenAsync(client);

        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        var authenticationCookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("__Host-LandscapeTsi.Auth=", StringComparison.Ordinal));
        Assert.Contains("secure", authenticationCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", authenticationCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", authenticationCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_WhenOAuthEnabled_ShowsCorporateOption()
    {
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:OAuth:Enabled"] = "true",
            ["Authentication:OAuth:Authority"] = "https://identity.invalid",
            ["Authentication:OAuth:ClientId"] = "landscape-tsi-test"
        });
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Account/Login");

        Assert.Contains("Continuar con cuenta corporativa", html);
        Assert.True(
            html.IndexOf("Continuar con cuenta corporativa", StringComparison.Ordinal) <
            html.IndexOf("name=\"UserName\"", StringComparison.Ordinal));
    }

    [Fact]
    public void AuthenticationStyles_ProvideResponsiveFocusAndHighContrastRules()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var css = File.ReadAllText(Path.Combine(repositoryRoot, "src", "Landscape.Tsi.Web", "wwwroot", "css", "site.css"));

        Assert.Contains("@media (max-width: 600px)", css, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", css, StringComparison.Ordinal);
        Assert.Contains("@media (forced-colors: active)", css, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: no-preference)", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocalLogin_RepeatedFailures_LocksAccount()
    {
        var password = $"Aa1!{Guid.NewGuid():N}";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "true",
            ["BootstrapAdmin:Password"] = password,
            ["Authentication:Local:MaxFailedAccessAttempts"] = "3"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var (token, _) = await GetLoginTokenAsync(client);
            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = "jean",
                ["Password"] = $"Wrong1!{Guid.NewGuid():N}",
                ["__RequestVerificationToken"] = token
            }));
            Assert.Contains("Usuario o contrase&#xF1;a incorrectos.", await response.Content.ReadAsStringAsync());
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var user = await manager.FindByNameAsync("jean");
        Assert.True(await manager.IsLockedOutAsync(user!));
    }

    [Fact]
    public async Task ActiveSession_IsRejectedAfterUserIsSuspended()
    {
        var password = $"Aa1!{Guid.NewGuid():N}";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "true",
            ["BootstrapAdmin:Password"] = password
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var (token, _) = await GetLoginTokenAsync(client);
        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
            var user = await manager.FindByNameAsync("jean");
            user!.IsActive = false;
            Assert.True((await manager.UpdateAsync(user)).Succeeded);
        }

        var protectedResponse = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Equal("/Account/Login", protectedResponse.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task ExistingSession_LosesProtectedAccessAfterPermissionRevocation()
    {
        var password = $"Aa1!{Guid.NewGuid():N}";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "true",
            ["BootstrapAdmin:Password"] = password
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        var (token, _) = await GetLoginTokenAsync(client);
        var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration")).StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var assignment = await dbContext.RolePermissions
                .SingleAsync(x => x.Permission.Code == Permissions.UserManage);
            dbContext.RolePermissions.Remove(assignment);
            await dbContext.SaveChangesAsync();
        }

        var protectedResponse = await client.GetAsync("/Administration");

        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Equal("/Account/AccessDenied", protectedResponse.Headers.Location?.AbsolutePath);
    }

    private static WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "false",
            ["Authentication:OAuth:Enabled"] = "false",
            ["Identity:InMemoryDatabaseName"] = $"web-test-{Guid.NewGuid():N}"
        };
        if (overrides is not null)
        {
            foreach (var item in overrides)
            {
                values[item.Key] = item.Value;
            }
        }

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureLogging(logging => logging.ClearProviders());
            foreach (var item in values)
            {
                builder.UseSetting(item.Key, item.Value);
            }
        });
    }

    private static async Task<(string Token, string Html)> GetLoginTokenAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/Account/Login");
        var token = WebUtility.HtmlDecode(Regex.Match(html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
        return (token, html);
    }
}
