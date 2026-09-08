using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Landscape.Tsi.Tests.Web;

[Collection("Web application")]
public sealed class MasterTablesHttpTests
{
    public static TheoryData<string> MutableCatalogRoutes => new()
    {
        "building-block",
        "capacidad-seguridad",
        "funcionalidad",
        "tecnologia-tsi",
        "familia",
        "casos-uso",
        "empresa-subsidiaria",
        "ciso",
        "postura-roadmap",
        "modalidad-laboral",
        "tipo-operacion"
    };

    public static TheoryData<string> AuthoritativeCatalogRoutes => new()
    {
        "estado-adopcion-tsi",
        "fase-adopcion",
        "estado-capacidad",
        "estado-funcionalidad"
    };

    [Fact]
    public async Task TestAuthenticationHandler_LeavesAnonymousRequestsUnauthenticated()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/Administration/MasterTables?anonymous=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/Audit?anonymous=true")).StatusCode);
    }

    [Fact]
    public async Task AuthorizedCatalogList_Returns200_AndReadOnlyCreateIsNotAvailable()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var list = await client.GetAsync("/Administration/MasterTables/familia");
        var readOnlyCreate = await client.GetAsync("/Administration/MasterTables/estado-adopcion-tsi/create");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, readOnlyCreate.StatusCode);
    }

    [Fact]
    public async Task MissingPermissionOrScope_IsForbiddenOnProtectedCatalogRoutes()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/familia?noPermission=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/create?noCreate=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/edit/1?noEdit=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Administration/MasterTables/tecnologia-tsi/1/delete-impact?noDelete=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Administration/MasterTables/familia/create?noScope=true")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(MutableCatalogRoutes))]
    public async Task EveryMutableCatalog_ExposesListDetailCreateAndEditRoutes(string route)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}?search=ITEST&page=1&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}/details/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}/edit/1")).StatusCode);

        var create = await client.GetAsync($"/Administration/MasterTables/{route}/create");
        var definition = MasterCatalogRegistry.GetByRoute(route)!;
        Assert.Equal(definition.EditorMode == CatalogEditorMode.Modal ? HttpStatusCode.Redirect : HttpStatusCode.OK, create.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AuthoritativeCatalogRoutes))]
    public async Task EveryAuthoritativeCatalog_AllowsReadAndRejectsMutationRoutes(string route)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Administration/MasterTables/{route}/details/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Administration/MasterTables/{route}/create")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Administration/MasterTables/{route}/edit/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Administration/MasterTables/{route}/1/delete-impact")).StatusCode);
    }

    [Fact]
    public async Task Domain_ExposesListDetailAndDeleteImpactWithoutUsingSql()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration/MasterTables/Domain?search=ITEST&page=1&pageSize=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration/MasterTables/Domain/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Administration/MasterTables/Domain/1/delete-impact")).StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        // These MVC authorization tests use service doubles and must not connect to any SQL server.
        builder.UseSetting("ConnectionStrings:LandscapeTsiDb", string.Empty);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICatalogManagementService>();
            services.AddSingleton<ICatalogManagementService, StubCatalogService>();
            services.RemoveAll<IDominioService>();
            services.AddSingleton<IDominioService, StubDominioService>();
            services.RemoveAll<IDeletionImpactService>();
            services.AddSingleton<IDeletionImpactService, StubDeletionImpactService>();
            services.RemoveAll<IBuildingBlockRelatedService>();
            services.AddSingleton<IBuildingBlockRelatedService, StubBuildingBlockRelatedService>();
            services.RemoveAll<IBuildingBlockTechnologyMappingService>();
            services.AddSingleton<IBuildingBlockTechnologyMappingService, StubTechnologyMappingService>();
            services.RemoveAll<IEffectiveAccessService>();
            services.AddSingleton<IEffectiveAccessService, StubAccessService>();
            services.AddHttpContextAccessor();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "MasterTablesTest";
                options.DefaultChallengeScheme = "MasterTablesTest";
            }).AddScheme<AuthenticationSchemeOptions, MasterTablesAuthHandler>("MasterTablesTest", _ => { });
        });
    });

    private sealed class MasterTablesAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Query.ContainsKey("anonymous")) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            if (!Request.Query.ContainsKey("noPermission")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogView));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noCreate")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogCreate));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noEdit")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogEdit));
            if (!Request.Query.ContainsKey("noPermission") && !Request.Query.ContainsKey("noDelete")) claims.Add(new Claim(CustomClaimTypes.Permission, Permissions.CatalogDelete));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }

    private sealed class StubAccessService(IHttpContextAccessor httpContextAccessor) : IEffectiveAccessService
    {
        public Task<bool> HasActiveCorporateScopeAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(!httpContextAccessor.HttpContext!.Request.Query.ContainsKey("noScope"));
        public Task<bool> IsAuthorizedAsync(Guid userId, string permission, int empresaSubsidiariaId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class StubCatalogService : ICatalogManagementService
    {
        public static string CurrentName { get; private set; } = "Familia de prueba";
        private static CatalogRow Row => new(1, new Dictionary<string, object?> { ["nombre"] = CurrentName, ["nombreCorporativo"] = CurrentName }, new Dictionary<string, string?> { ["nombre"] = CurrentName, ["nombreCorporativo"] = CurrentName });
        public Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null) => Task.FromResult(new CatalogPageResult([Row], 1, pageSize, 1));
        public Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new CatalogPageResult([], 1, pageSize, 0));
        public Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogRelationBucket>>([]);
        public Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default) => Task.FromResult<CatalogRow?>(id == 1 ? Row : null);
        public Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>>(new Dictionary<string, IReadOnlyList<CatalogOption>>());
        public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
        {
            if (values.TryGetValue("nombreCorporativo", out var name) && !string.IsNullOrWhiteSpace(name)) CurrentName = name;
            return Task.FromResult(true);
        }
    }

    private sealed class StubDominioService : IDominioService
    {
        private static readonly DominioRecord Record = new(1, "ITEST_DOMINIO", "Descripción", null, null, null, null, null);
        public Task<PagedResult<DominioRecord>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<DominioRecord>([Record], page, pageSize, 1));
        public Task<DominioRecord?> GetAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<DominioRecord?>(id == 1 ? Record : null);
        public Task<int> CreateAsync(DominioCommand command, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(int id, DominioCommand command, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(id == 1);
    }

    private sealed class StubDeletionImpactService : IDeletionImpactService
    {
        private static readonly DeletionDependencyNode Root = new("Registro", "TRegistro", 1, 0, "Raíz", []);
        private static readonly DeletionImpactResult Impact = new("dominio", "TMDominio", 1, "ITEST", [], [], 0, 1, true, null, Root, false);
        public Task<DeletionImpactResult?> PreviewAsync(string entityCode, int rootId, CancellationToken cancellationToken = default) => Task.FromResult<DeletionImpactResult?>(rootId == 1 ? Impact with { RootEntity = entityCode } : null);
        public Task<DeletionExecutionResult> DeleteAsync(string entityCode, int rootId, string? confirmation, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => Task.FromResult(new DeletionExecutionResult(true, 1));
    }

    private sealed class StubBuildingBlockRelatedService : IBuildingBlockRelatedService
    {
        private static readonly CatalogPageResult Empty = new([], 1, 10, 0);
        public Task<BuildingBlockRelatedResult> GetAsync(int buildingBlockId, string? capabilitySearch, string? functionalitySearch, string? technologySearch, int capabilityPage, int functionalityPage, int technologyPage, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new BuildingBlockRelatedResult(Empty, Empty, Empty));
    }

    private sealed class StubTechnologyMappingService : IBuildingBlockTechnologyMappingService
    {
        public Task<BuildingTechnologyRelationResult?> GetBuildingBlockRelationsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default) => Task.FromResult<BuildingTechnologyRelationResult?>(null);
        public Task<TechnologyRelationResult?> GetTechnologyRelationsAsync(int technologyId, CancellationToken cancellationToken = default) => Task.FromResult<TechnologyRelationResult?>(null);
        public Task<TechnologyMappingPage> ListAsync(TechnologyMappingQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UnassignedTechnologyPage> ListUnassignedAsync(UnassignedTechnologyQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TechnologyOption>> GetTechnologyOptionsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FamilyBuildingBlockReport> GetFamilyBuildingBlockReportAsync(int? familyId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveTechnologyRelationsAsync(int technologyId, IReadOnlyCollection<int> buildingBlockIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveBuildingBlockRelationsAsync(int buildingBlockId, IReadOnlyCollection<int> technologyIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AssociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisassociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}