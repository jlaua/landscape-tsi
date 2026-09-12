using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class AdoptionProcessHttpTests
{
    [Fact]
    public async Task AnonymousAccess_ToAdoptionProcess_ReturnsUnauthorized()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess?anonymous=true");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WithoutPermission_ToAdoptionProcess_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess?noPermission=true");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authorized_ListProcesses_Returns200AndRendersView()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Procesos de Adopción TSI", content);
        Assert.Contains("PROC-TEST-001", content);
    }

    [Fact]
    public async Task CreateProcessGet_WithEditPermission_Returns200()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Iniciar Proceso de Adopción TSI", content);
    }

    [Fact]
    public async Task CreateProcessGet_WithoutEditPermission_ReturnsForbidden()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/Create?noEdit=true");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProcessDetails_Returns200_AndRendersAllSections()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("PROC-TEST-001", content);
        Assert.Contains("Estándar Tecnológico Corporativo", content);
        Assert.Contains("Convocatoria a Empresas Subsidiarias", content);
        Assert.Contains("Tech Corp Principal", content);
        Assert.Contains("CT-2026-001", content);
        Assert.Contains("AD-2026-001", content);
        Assert.Contains("Licencias E5", content);
    }

    [Fact]
    public async Task Authorized_EvaluationsIndex_Returns200AndRendersView()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/Evaluations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Evaluaciones de Tecnologías TSI", content);
        Assert.Contains("Nueva Solicitud de Evaluación", content);
        Assert.Contains("Búsqueda rápida", content);
        Assert.Contains("PROC-TEST-001", content);
    }

    [Fact]
    public async Task Authorized_CreateEvaluationGet_Returns200AndRendersWizard()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/Evaluations/Create");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Nueva Solicitud de Evaluación Técnica", content);
        Assert.Contains("1. Definición & Taxonomía TSI", content);
        Assert.Contains("2. Subsidiarias Participantes (Checkboxes)", content);
        Assert.Contains("3. Diagnóstico AS-IS & Contratos", content);
        Assert.Contains("4. Estándar Corporativo & Convergencia", content);
    }

    [Fact]
    public async Task Authorized_CapabilitiesPreview_ReturnsJson()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/Evaluations/CapabilitiesPreview/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("buildingBlockNombre", content);
        Assert.Contains("Test BB", content);
    }

    [Fact]
    public async Task PostMutations_WithoutAntiforgery_ReturnBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var stdContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["buildingBlockId"] = "1",
            ["tecnologiaId"] = "10",
            ["rolEstandar"] = "PRINCIPAL",
            ["fechaInicio"] = "2026-01-01"
        });
        var response = await client.PostAsync("/Administration/AdoptionProcess/1/Standard", stdContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMutations_WithoutEditPermission_ReturnForbiddenOrBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["empresaId"] = "1",
            ["aplica"] = "true"
        });
        var response = await client.PostAsync("/Administration/AdoptionProcess/1/Convene?noEdit=true", content);
        // Either forbidden by authorization filter or bad request due to antiforgery
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task QuickCreateTechnology_WithoutAntiforgery_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["nombreCorporativo"] = "Nueva Tech Test"
        });
        var response = await client.PostAsync("/Administration/AdoptionProcess/QuickCreateTechnology", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QuickCreateTechnology_WithoutEditPermission_ReturnsForbiddenOrBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["nombreCorporativo"] = "Nueva Tech Test"
        });
        var response = await client.PostAsync("/Administration/AdoptionProcess/QuickCreateTechnology?noEdit=true", content);
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchTechnologies_Authorized_Returns200()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/SearchTechnologies?q=Test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithoutAntiforgery_ReturnsBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["statusId"] = "2"
        });
        var response = await client.PostAsync("/Administration/AdoptionProcess/1/Status", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_WithoutEditPermission_ReturnsForbiddenOrBadRequest()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["statusId"] = "2"
        });
        var response = await client.PostAsync("/Administration/AdoptionProcess/1/Status?noEdit=true", content);
        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProcessDetails_RendersChangeStatusAndEditButtons()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("modal-change-status", content);
        Assert.Contains("Cambiar Estado", content);
        Assert.Contains("Editar Evaluación", content);
    }

    [Fact]
    public async Task EvaluationsIndex_RendersEditAndChangeStatusButtons()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Administration/AdoptionProcess/Evaluations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("modal-change-status-eval", content);
        Assert.Contains("btn-change-status-row", content);
        Assert.Contains("Editar", content);
    }

    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:LandscapeTsiDb", string.Empty);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdoptionProcessService>();
            services.AddSingleton<IAdoptionProcessService, StubAdoptionProcessService>();

            services.AddHttpContextAccessor();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "AdoptionWebTest";
                options.DefaultChallengeScheme = "AdoptionWebTest";
            }).AddScheme<AuthenticationSchemeOptions, AdoptionAuthHandler>("AdoptionWebTest", _ => { });
        });
    });

    private sealed class AdoptionAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Query.ContainsKey("anonymous")) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            if (!Request.Query.ContainsKey("noPermission")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogView));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noEdit")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogEdit));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    private sealed class StubAdoptionProcessService : IAdoptionProcessService
    {
        public Task<IReadOnlyList<AdoptionProcessSummaryDto>> ListProcessesAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AdoptionProcessSummaryDto> list = [
                new AdoptionProcessSummaryDto(1, "PROC-TEST-001", "Proceso MFA 2026", 1, "MFA BB", 1, "En Evaluación", "Líder TSI", DateTime.Today, DateTime.Today.AddMonths(6), 3, 2, 1)
            ];
            return Task.FromResult(list);
        }

        public Task<AdoptionProcessDetailDto?> GetProcessDetailAsync(int procesoId, CancellationToken cancellationToken = default)
        {
            var adenda = new ContractDto(2, 10, "AD-2026-001", true, 1, "CT-2026-001", DateTime.Today, DateTime.Today.AddYears(1), null, null, 15000m, "USD", null, []);
            var contract = new ContractDto(1, 10, "CT-2026-001", false, null, null, DateTime.Today, DateTime.Today.AddYears(1), null, null, 80000m, "USD", null, [adenda]);
            var driver = new DriverDto(1, 10, "Licencias E5", "Usuarios", 100, 35m, "USD", 3500m);
            var opModel = new OperationModelDto(1, 10, 1, "Operación Híbrida", 1, "Teletrabajo");

            var impl = new ImplementedTechnologyDto(10, 1, "Banco Subsidiaria", 1, 100, "Tech Impl", "Vendor X", true, "1.2.0", "ALINEADO", opModel, [contract], [driver]);
            var comp = new CompanyAdoptionRowDto(1, 1, "Banco Subsidiaria", 5, "Juan Perez", "jperez@banco.corp", true, null, DateTime.Today, [impl], "ALINEADO");

            var stdPrincipal = new StandardTechnologyDto(1, 1, 100, "Tech Corp Principal", "Vendor Corp", "Contacto V", "Partner P", "PRINCIPAL", "ACTIVO_VIGENTE", DateTime.Today, null, null, "Sustento corporativo", ["Caso A", "Caso B"]);

            return Task.FromResult<AdoptionProcessDetailDto?>(new AdoptionProcessDetailDto(
                1, "PROC-TEST-001", "Proceso MFA 2026", 1, "MFA BB", "Identidad", "Autenticación", 1, "En Evaluación", "Objetivo", "Alcance", "Líder", DateTime.Today, DateTime.Today.AddMonths(6),
                stdPrincipal, [], [], [comp]));
        }

        public Task<AdoptionProcessDetailDto?> GetProcessDetailByBuildingBlockAsync(int buildingBlockId, CancellationToken cancellationToken = default) =>
            GetProcessDetailAsync(1, cancellationToken);

        public Task<AdoptionResult> CreateProcessAsync(CreateAdoptionProcessCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> UpdateProcessStatusAsync(int procesoId, int nuevoEstadoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok"));

        public Task<AdoptionResult> ConveneCompanyAsync(ConveneCompanyCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> SetCorporateStandardAsync(SetCorporateStandardCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> RegisterImplementedTechnologyAsync(RegisterImplementedTechnologyCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> SaveContractAsync(SaveContractCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> DeleteContractAsync(int contratoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok"));

        public Task<AdoptionResult> SaveDriverAsync(SaveDriverCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> DeleteDriverAsync(int driverId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok"));

        public Task<AdoptionResult> SaveOperationModelAsync(SaveOperationModelCommand command, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok"));

        public Task<BuildingBlockCapabilitiesDto?> GetBuildingBlockCapabilitiesAsync(int buildingBlockId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BuildingBlockCapabilitiesDto?>(new BuildingBlockCapabilitiesDto(buildingBlockId, "Test BB", "Test Dominio", []));

        public Task<AdoptionResult> BatchConveneCompaniesAsync(int procesoId, IEnumerable<ConveneCompanyInput> companies, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok", 1));

        public Task<AdoptionResult> DeactivateProcessAsync(int procesoId, string motivo, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdoptionResult(true, "Ok"));
    }
}