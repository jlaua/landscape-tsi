using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Application.Reporting;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class CompanyCisoHttpTests
{
    [Fact]
    public async Task AuthorizedReport_Returns200AndPreservesFiltersAndNavigation()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/reporteria/empresas-ciso?search=Prima&allCiso=true&page=2");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Empresas y CISO", html);
        Assert.Contains("name=\"search\"", html);
        Assert.Contains("Todos los CISO", html);
        Assert.Contains("Ver empresa", html);
        Assert.Contains("Ver CISO", html);
        Assert.Contains("search=Prima", html);
        Assert.Contains("allCiso=True", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MissingPermission_Returns403()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/reporteria/empresas-ciso?noPermission=true");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidAndNonexistentCompanyFilters_Return200Without404()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var invalid = await client.GetAsync("/reporteria/empresas-ciso?companyId=not-a-number");
        var nonexistent = await client.GetAsync("/reporteria/empresas-ciso?companyId=999999");

        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nonexistent.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IReportingService>();
                services.AddSingleton<IReportingService, StubReportingService>();
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "CompanyCisoTest";
                    options.DefaultChallengeScheme = "CompanyCisoTest";
                }).AddScheme<AuthenticationSchemeOptions, CompanyCisoTestAuthHandler>("CompanyCisoTest", _ => { });
            });
        });
    }

    private sealed class CompanyCisoTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Query.ContainsKey("anonymous")) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            if (!Request.Query.ContainsKey("noPermission")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogView));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    private sealed class StubReportingService : IReportingService
    {
        public Task<CompanyCisoReport> GetCompanyCisoReportAsync(CompanyCisoReportQuery query, CancellationToken cancellationToken = default)
        {
            var row = new CompanyCisoReportRow(1, "Empresa de prueba", null, null, null, null, null, 2, "CISO de prueba", "ciso@example.test", "Seguridad", true, true, true, false, "/Administration/MasterTables/empresa-subsidiaria/details/1", "/Administration/MasterTables/ciso/details/2");
            return Task.FromResult(new CompanyCisoReport(query, new(1, 1, 0, 1, 0), [row], 1, [(1, "Empresa de prueba")], [], [], [], ["CISO de prueba"]));
        }

        public Task<IReadOnlyList<CatalogReportPoint>> GetCatalogTotalsAsync(string? group, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogReportPoint>>([]);
        public Task<CatalogReportDetail> GetCatalogDetailAsync(string catalogCode, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogReportRelation>> GetCatalogRelationsAsync(string catalogCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogReportRelation>>([]);
        public Task<CatalogReportDetail> GetRelatedCatalogDetailAsync(string parentCode, string childCode, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CatalogContextKpi>> GetCatalogContextKpisAsync(string catalogCode, int recordId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogContextKpi>>([]);
    }
}