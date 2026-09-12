using System.Net;
using System.Text.RegularExpressions;

using Landscape.Tsi.Web.Controllers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class ThemeHttpTests
{
    [Fact]
    public async Task LoginPage_WithoutThemeCookie_DefaultsToLightDataTheme()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("data-theme=\"light\"", html, StringComparison.Ordinal);
        Assert.Contains("theme-selector-group", html, StringComparison.Ordinal);
        Assert.Contains("Ligero", html, StringComparison.Ordinal);
        Assert.Contains("Corporativo", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginPage_WithCorporateCookie_RendersCorporateThemeAndLogo()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{AccountController.ThemeCookieName}=corporate");

        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("data-theme=\"corporate\"", html, StringComparison.Ordinal);
        Assert.Contains("CREDICORP", html, StringComparison.Ordinal);
        Assert.Contains("credicorp-symbol", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetTheme_ValidCorporateRequest_SetsCookieAndReturnsSuccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var requestContent = new StringContent("{\"theme\":\"corporate\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/Account/SetTheme", requestContent);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"theme\":\"corporate\"", json, StringComparison.Ordinal);

        Assert.True(response.Headers.Contains("Set-Cookie"));
        var setCookie = string.Join(";", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains($"{AccountController.ThemeCookieName}=corporate", setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetTheme_InvalidTheme_SanitizesAndDefaultsToLight()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var requestContent = new StringContent("{\"theme\":\"<script>alert('xss')</script>\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/Account/SetTheme", requestContent);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"theme\":\"light\"", json, StringComparison.Ordinal);

        Assert.True(response.Headers.Contains("Set-Cookie"));
        var setCookie = string.Join(";", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains($"{AccountController.ThemeCookieName}=light", setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preferences_UnauthenticatedUser_RedirectsToLogin()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Account/Preferencias");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preferences_AuthenticatedUser_CanViewAndSubmitPreference()
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

        // 1. Iniciar sesión como bootstrap admin
        var (token, _) = await GetLoginTokenAsync(client);
        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["SelectedTheme"] = "corporate",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        // 2. Consultar Preferencias
        var prefPageResponse = await client.GetAsync("/Account/Preferencias");
        Assert.Equal(HttpStatusCode.OK, prefPageResponse.StatusCode);
        var prefHtml = await prefPageResponse.Content.ReadAsStringAsync();
        Assert.Contains("Estilo Visual de la Plataforma", prefHtml, StringComparison.Ordinal);
        Assert.Contains("themeToggle", prefHtml, StringComparison.Ordinal);

        // 3. Enviar cambio de tema a Ligero desde Preferencias
        var prefToken = WebUtility.HtmlDecode(Regex.Match(prefHtml,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

        var updateResponse = await client.PostAsync("/Account/Preferencias", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["SelectedTheme"] = "light",
            ["UserName"] = "jean",
            ["__RequestVerificationToken"] = prefToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, updateResponse.StatusCode);
        Assert.True(updateResponse.Headers.Contains("Set-Cookie"));
        var setCookie = string.Join(";", updateResponse.Headers.GetValues("Set-Cookie"));
        Assert.Contains($"{AccountController.ThemeCookieName}=light", setCookie, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "false",
            ["Authentication:OAuth:Enabled"] = "false",
            ["ConnectionStrings:LandscapeTsiDb"] = string.Empty,
            ["Identity:InMemoryDatabaseName"] = $"theme-test-{Guid.NewGuid():N}"
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