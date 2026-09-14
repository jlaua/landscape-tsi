using System.Net;
using System.Text.RegularExpressions;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class DocumentationControllerTests
{
    [Fact]
    public async Task Documentation_RedirectsAnonymousUserToLogin()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Documentation");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Documentation_AuthenticatedUser_CanViewArchitecturePageAndDiagram()
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
        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "jean",
            ["Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        // Verify documentation page
        var docResponse = await client.GetAsync("/Documentation");
        docResponse.EnsureSuccessStatusCode();
        var html = await docResponse.Content.ReadAsStringAsync();

        Assert.Contains("Arquitectura de Landscape TSI", html);
        Assert.Contains("/Documentation/Diagram", html);
        Assert.Contains("Hecho Comprobado", html);
        Assert.Contains("Persistencia y SQL Server", html);

        // Verify diagram endpoint returns HTML
        var diagramResponse = await client.GetAsync("/Documentation/Diagram");
        diagramResponse.EnsureSuccessStatusCode();
        Assert.Equal("text/html", diagramResponse.Content.Headers.ContentType?.MediaType);
        var diagramHtml = await diagramResponse.Content.ReadAsStringAsync();
        Assert.Contains("Arquitectura de Landscape TSI", diagramHtml);

        // Verify navbar structure: Documentación dropdown contains Vista de entidades
        Assert.Contains("Vista de entidades", html);
        Assert.Contains("Reportería", html);

        // Verify EntityView is accessible under Documentation route
        var entityViewResponse = await client.GetAsync("/Documentation/EntityView");
        entityViewResponse.EnsureSuccessStatusCode();
        var entityViewHtml = await entityViewResponse.Content.ReadAsStringAsync();
        Assert.Contains("Vista de entidades", entityViewHtml);
        Assert.Contains("Documentación", entityViewHtml);

        // Verify Reporting is accessible
        var reportingResponse = await client.GetAsync("/reporteria");
        reportingResponse.EnsureSuccessStatusCode();
    }

    private static WebApplicationFactory<Program> CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = "false",
            ["Authentication:OAuth:Enabled"] = "false",
            ["ConnectionStrings:LandscapeTsiDb"] = string.Empty,
            ["Identity:InMemoryDatabaseName"] = $"web-doc-test-{Guid.NewGuid():N}"
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
