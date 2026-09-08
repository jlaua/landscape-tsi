using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Infrastructure.Reporting;

namespace Landscape.Tsi.Tests.Reporting;

public sealed class CatalogReportingServiceTests
{
    [Fact]
    public async Task GetCatalogTotals_FiltersByGroupAndUsesTotalCount()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        var result = await service.GetCatalogTotalsAsync("Tecnología");

        var technology = Assert.Single(result, point => point.Code == "tecnologia-tsi");
        Assert.Equal(7, technology.Total);
        Assert.All(result, point => Assert.Equal("Tecnología", point.Group));
    }

    [Fact]
    public async Task GetCatalogDetail_UsesFunctionalColumnsAndPagination()
    {
        var stub = new StubCatalogService();
        var service = new CatalogReportingService(stub);

        var result = await service.GetCatalogDetailAsync("dominio", "ident", 2, 5, sortColumn: "dominio", sortDirection: "desc");

        Assert.Equal("Dominio", result.Name);
        Assert.Contains(result.Columns, column => column.Label == "Dominio");
        Assert.DoesNotContain(result.Columns, column => column.Code == "iddominio");
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(("ident", 2, 5, "dominio", "desc"), stub.LastListRequest);
    }

    [Fact]
    public async Task GetCatalogRelations_UsesRegisteredOneToManyRelation()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        var result = await service.GetCatalogRelationsAsync("dominio");

        var relation = Assert.Single(result);
        Assert.Equal("building-block", relation.ChildCode);
        Assert.Equal(2, relation.Buckets.Single().Total);
    }

    [Fact]
    public async Task GetCatalogContextKpis_ReturnsOnlyDirectBuildingBlockKpi()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        var result = await service.GetCatalogContextKpisAsync("dominio", 1);

        var kpi = Assert.Single(result);
        Assert.Equal("building-block", kpi.Code);
        Assert.Equal(2, kpi.Value);
    }

    [Fact]
    public async Task GetCatalogDetail_RejectsUnknownCatalogCode()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCatalogDetailAsync("tabla-arbitraria", null, 1, 10));
    }

    [Fact]
    public async Task GetCatalogContextKpis_RejectsUnknownDomain()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCatalogContextKpisAsync("dominio", 999));
    }

    [Fact]
    public async Task GetCatalogContextKpis_ReturnsZeroForDomainWithoutBuildingBlocks()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        var result = await service.GetCatalogContextKpisAsync("dominio", 2);

        Assert.Equal(0, Assert.Single(result).Value);
    }

    [Fact]
    public async Task GetRelatedCatalogDetail_UsesRegisteredParentChildRelation()
    {
        var service = new CatalogReportingService(new StubCatalogService());

        var result = await service.GetRelatedCatalogDetailAsync("dominio", "building-block", 1, "identity", 2, 10);

        Assert.Equal("building-block", result.Code);
        Assert.Equal(2, result.TotalCount);
    }

    private sealed class StubCatalogService : ICatalogManagementService
    {
        public (string? Search, int Page, int PageSize, string? SortColumn, string? SortDirection)? LastListRequest { get; private set; }

        public Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null)
        {
            LastListRequest = (search, page, pageSize, sortColumn, sortDirection);
            var row = new CatalogRow(1, new Dictionary<string, object?> { ["dominio"] = "Seguridad" }, new Dictionary<string, string?> { ["dominio"] = "Seguridad" });
            return Task.FromResult(new CatalogPageResult(definition.Code == "dominio" ? [row] : [], 1, pageSize, definition.Code == "tecnologia-tsi" ? 7 : definition.Code == "dominio" ? 1 : 3));
        }
        public Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new CatalogPageResult([], page, pageSize, parentId == 1 ? 2 : 0));
        public Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CatalogRelationBucket>>([new CatalogRelationBucket(1, "Seguridad", 2)]);
        public Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default)
            => Task.FromResult(parentId == 1 ? 2 : 0);
        public Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default)
            => Task.FromResult<CatalogRow?>(definition.Code == "dominio" && (id == 1 || id == 2)
                ? new CatalogRow(id, new Dictionary<string, object?> { ["dominio"] = id == 1 ? "Seguridad" : "Vacio" }, new Dictionary<string, string?> { ["dominio"] = id == 1 ? "Seguridad" : "Vacio" })
                : null);
        public Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}