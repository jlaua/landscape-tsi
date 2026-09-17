using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class ThemeAccessibilityTests
{
    [Fact]
    public async Task LoginThemeSelector_HasAccessibleRadioGroupAndAriaAttributes()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Account/Login");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        // Verificar role="radiogroup" y aria-label
        Assert.Contains("role=\"radiogroup\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Seleccionar estilo visual\"", html, StringComparison.Ordinal);

        // Verificar role="radio" y aria-checked
        Assert.Contains("role=\"radio\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-checked=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-checked=\"false\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PreferencesThemeToggle_HasAccessibleSwitchRoleAndAssociatedLabel()
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

        // Autenticar
        var (token, _) = await GetLoginTokenAsync(client);
        await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));

        var response = await client.GetAsync("/Account/Preferencias");
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        // Verificar switch accesible role="switch" y label asociado
        Assert.Contains("role=\"switch\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"themeToggle\"", html, StringComparison.Ordinal);
        Assert.Contains("for=\"themeToggle\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-checked=", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SiteCss_ContainsCorporateAndLightTokensWithAccessibleContrastDefinitions()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/css/site.css");
        var css = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        // Tokens del tema Ligero
        Assert.Contains(":root, [data-theme=\"light\"]", css, StringComparison.Ordinal);
        Assert.Contains("--md-primary: #315da8", css, StringComparison.Ordinal);

        // Tokens del tema Corporativo Credicorp
        Assert.Contains("[data-theme=\"corporate\"]", css, StringComparison.Ordinal);
        Assert.Contains("--md-primary: #002a86", css, StringComparison.Ordinal);
        Assert.Contains("--brand-accent: #2ad2c9", css, StringComparison.Ordinal);
        Assert.Contains("--navbar-bg: #0a1b3a", css, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "false",
            ["Authentication:OAuth:Enabled"] = "false",
            ["ConnectionStrings:LandscapeTsiDb"] = string.Empty,
            ["Identity:InMemoryDatabaseName"] = $"theme-a11y-{Guid.NewGuid():N}"
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