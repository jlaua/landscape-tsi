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
    public async Task CompanyAdoptionByTechnology_Authorized_Returns200AndRendersSections()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/reporteria/adopcion-empresas-tecnologia");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Adopción de Empresas por Tecnología", html);
        Assert.Contains("Adopción de Empresas por Dominio", html);
        Assert.Contains("Alineación a Tecnología Corporativa", html);
    }

    [Fact]
    public async Task CreateUser_Get_RendersFormattedCardLayout()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/Usuarios/Nuevo");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Crear usuario local", html);
        Assert.Contains("admin-shell", html);
        Assert.Contains("IDENTIDAD LOCAL", html);
        Assert.Contains("Justificación de creación", html);
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

    [Fact]
    public async Task ReportingIndex_DisplaysOnlyReportingGroupsInFilter()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/reporteria");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<option value=\"Arquitectura de seguridad\">", html);
        Assert.Contains("<option value=\"Tecnología\">", html);
        Assert.DoesNotContain("<option value=\"Organización\">", html);
        Assert.DoesNotContain("<option value=\"Operación\">", html);
    }

    [Fact]
    public async Task ReportingIndex_DefaultsToDomainDistribution()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/reporteria");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Distribución de Dominios", html);
        Assert.Contains("id=\"tab-dominios-btn\"", html);
        Assert.Contains("class=\"nav-link active fw-bold px-3 py-2\" id=\"tab-dominios-btn\"", html);
    }

    [Fact]
    public async Task HomeIndex_ReturnsLandscapeTsiTreemap()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mapa Landscape TSI", html);
        Assert.Contains("treemap-layout-container", html);
    }

    [Fact]
    public async Task ReportingSubRoutes_ReturnSuccess()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var distResp = await client.GetAsync("/reporteria/distribucion-dominios");
        Assert.Equal(HttpStatusCode.OK, distResp.StatusCode);

        var catResp = await client.GetAsync("/reporteria/catalogos");
        Assert.Equal(HttpStatusCode.OK, catResp.StatusCode);

        var htmlCat = WebUtility.HtmlDecode(await catResp.Content.ReadAsStringAsync());
        Assert.Contains("class=\"nav-link active fw-bold px-3 py-2\" id=\"tab-catalogos-btn\"", htmlCat);
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
            if (!Request.Query.ContainsKey("noPermission"))
            {
                claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogView));
                claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.UserManage));
                claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.UsersCreate));
            }
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
        public Task<CompanyAdoptionReport> GetCompanyAdoptionReportAsync(CompanyAdoptionReportQuery query, CancellationToken cancellationToken = default)
        {
            var kpis = new CompanyAdoptionSummaryKpis(1, 1, 0, 0, 0, 0);
            return Task.FromResult(new CompanyAdoptionReport(query, kpis, [], [], 0, 1, [], [], []));
        }
    }
}