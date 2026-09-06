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

    private sealed class StubCatalogService : ICatalogManagementService
    {
        public Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new CatalogPageResult([], 1, 1, definition.Code == "tecnologia-tsi" ? 7 : 3));
        public Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
