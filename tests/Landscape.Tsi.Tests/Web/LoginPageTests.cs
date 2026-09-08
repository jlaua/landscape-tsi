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
        var homeHtml = await home.Content.ReadAsStringAsync();
        Assert.Contains("Cerrar sesión", homeHtml);
        Assert.Contains("Administración", homeHtml);
        Assert.Contains("Mapa del Catálogo Landscape TSI", homeHtml);
        Assert.Contains("data-catalog-code=\"building-block\"", homeHtml);
        Assert.Contains("data-catalog-code=\"building-block\"", homeHtml);
        Assert.DoesNotContain("data-catalog-graph", homeHtml);
        var administration = await client.GetAsync("/Administration");
        administration.EnsureSuccessStatusCode();
        var administrationHtml = await administration.Content.ReadAsStringAsync();
        Assert.Contains("jean", administrationHtml);
        Assert.Contains("Administrador del Sistema", administrationHtml);
        Assert.Contains("Audit.View", administrationHtml);
        Assert.Contains("Sin alcance asignado", administrationHtml);
        Assert.Contains("Vigencia", administrationHtml);
        var masterTables = await client.GetAsync("/Administration/MasterTables");
        masterTables.EnsureSuccessStatusCode();
        var masterTablesHtml = WebUtility.HtmlDecode(await masterTables.Content.ReadAsStringAsync());
        Assert.Contains("Administración de Tablas Maestras", masterTablesHtml);
        Assert.Contains("Dominio", masterTablesHtml);
        Assert.Contains("Building Block", masterTablesHtml);
        Assert.Contains("Tecnología TSI", masterTablesHtml);
        var domains = await client.GetAsync("/Administration/MasterTables/Domain");
        domains.EnsureSuccessStatusCode();
        Assert.Contains("Sin resultados", await domains.Content.ReadAsStringAsync());

        await using var scope = factory.Services.CreateAsyncScope();
        var audit = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Contains(audit.AuthenticationEvents, x => x.EventType == "Login" && x.Result == "Succeeded");
        var securityArchitectPermissions = await audit.RolePermissions
            .Where(assignment => assignment.Role.Code == SystemRoles.SecurityArchitectCode)
            .Select(assignment => assignment.Permission.Code)
            .ToListAsync();
        Assert.Contains(Permissions.CatalogView, securityArchitectPermissions);
        Assert.Contains(Permissions.CatalogCreate, securityArchitectPermissions);
        Assert.Contains(Permissions.CatalogEdit, securityArchitectPermissions);
    }

    [Fact]
    public async Task MasterCatalog_RedirectsAnonymousUserBeforeDataAccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/MasterTables/building-block");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task TechnologyMappingRoutes_ExistAndProtectBeforeDataAccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var path in new[]
        {
            "/Administration/TechnologyMapping",
            "/Administration/TechnologyMapping/Unassigned",
            "/Administration/TechnologyMapping/Technology/1/Relations",
            "/Administration/TechnologyMapping/BuildingBlock/1/Relations",
            "/Administration/TechnologyMapping/Family/1/BuildingBlocks"
        })
        {
            var response = await client.GetAsync(path);
            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
    }

    [Fact]
    public async Task ReportingRoutes_RequireCatalogViewBeforeDataAccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var path in new[]
        {
            "/reporteria",
            "/reporteria/empresas-ciso",
            "/reporteria/empresas-ciso?allCiso=true&search=Prima&page=2",
            "/reporteria/api/catalogos",
            "/reporteria/api/catalogos/dominio/detalle",
            "/reporteria/api/catalogos/dominio/registros/1/kpis",
            "/reporteria/api/catalogos/dominio/relaciones"
        })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
        }
    }

    [Fact]
    public async Task MasterTableRoutes_RequireCatalogViewBeforeDataAccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var path in new[]
        {
            "/Administration/MasterTables",
            "/Administration/MasterTables/building-block",
            "/Administration/MasterTables/building-block/details/1",
            "/Administration/MasterTables/building-block/create",
            "/Administration/MasterTables/building-block/edit/1"
        })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
        }
    }

    [Fact]
    public async Task InvalidLocalLogin_UsesGenericMessage()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = WebUtility.HtmlDecode(Regex.Match(loginPage,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

        var attemptedPassword = $"Aa1!{Guid.NewGuid():N}";
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = $"missing-{Guid.NewGuid():N}",
            ["Password"] = attemptedPassword,
            ["__RequestVerificationToken"] = token
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Usuario o contrase&#xF1;a incorrectos.", html);
        Assert.Contains("aria-live=\"assertive\"", html);
        Assert.DoesNotContain(attemptedPassword, html, StringComparison.Ordinal);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.DoesNotContain(dbContext.AuthenticationEvents, item =>
            item.UserIdentifier == attemptedPassword || item.CorrelationId == attemptedPassword);
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
        var home = await client.GetStringAsync("/");
        Assert.DoesNotContain("href=\"/Administration\"", home, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditPage_FiltersAuthorizedSubsidiaryAndHidesUnauthorizedScope()
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
        await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
            var user = await manager.FindByNameAsync("jean");
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            dbContext.UserOrganizations.Add(new IamUsuarioOrganizacion
            {
                UserId = user!.Id,
                EmpresaSubsidiariaId = 101,
                ApprovedByUserId = Guid.NewGuid()
            });
            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                EmpresaSubsidiariaId = 101,
                OccurredAtUtc = DateTime.UtcNow,
                EventType = "AuthorizedEvent",
                Result = "Succeeded",
                CorrelationId = "web-audit"
            });
            await dbContext.SaveChangesAsync();
        }

        var allowed = await client.GetAsync("/Audit?empresaSubsidiariaId=101");
        var allowedHtml = await allowed.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Contains("AuthorizedEvent", allowedHtml);
        Assert.Contains("aria-label=\"Eventos de auditoría autorizados\"", allowedHtml);
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local).ToString("yyyy-MM-dd");
        Assert.Contains($"name=\"dateFrom\" type=\"date\" value=\"{today}\"", allowedHtml);
        Assert.Contains($"name=\"dateTo\" type=\"date\" value=\"{today}\"", allowedHtml);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Audit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Audit?action=DELETE")).StatusCode);
        var emptyAudit = await client.GetAsync("/Audit?entity=funcionalidad");
        Assert.Equal(HttpStatusCode.OK, emptyAudit.StatusCode);
        Assert.Contains("No se encontraron eventos para los filtros seleccionados.", await emptyAudit.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Audit?dateFrom={today}&dateTo={today}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/Audit?entity=not-allowed")).StatusCode);

        var denied = await client.GetAsync("/Audit?empresaSubsidiariaId=202");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.DoesNotContain("AuthorizedEvent", await denied.Content.ReadAsStringAsync());
    }

    private static WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "false",
            ["Authentication:OAuth:Enabled"] = "false",
            ["ConnectionStrings:LandscapeTsiDb"] = string.Empty,
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
