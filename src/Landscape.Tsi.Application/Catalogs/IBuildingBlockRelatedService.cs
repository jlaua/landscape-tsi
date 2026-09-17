namespace Landscape.Tsi.Application.Catalogs;

public interface IBuildingBlockRelatedService
{
    Task<BuildingBlockRelatedResult> GetAsync(
        int buildingBlockId,
        string? capabilitySearch,
        string? functionalitySearch,
        string? technologySearch,
        int capabilityPage,
        int functionalityPage,
        int technologyPage,
        int pageSize,
        string? functionalitySortBy = null,
        string? functionalitySortDirection = null,
        string? capabilitySortBy = null,
        string? capabilitySortDirection = null,
        int? functionalityPageSize = null,
        int? capabilityPageSize = null,
        CancellationToken cancellationToken = default);
}

public sealed record BuildingBlockRelatedResult(
    CatalogPageResult Capabilities,
    CatalogPageResult Functionalities,
    CatalogPageResult Technologies);