using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class CatalogDetailViewModel
{
    public required MasterCatalogDefinition Definition { get; init; }
    public required CatalogRow Record { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> Options { get; init; }
    public BuildingBlockRelatedResult? Related { get; init; }
    public CatalogPageResult? RelatedRecords { get; init; }
    public BuildingTechnologyRelationResult? TechnologyMapping { get; init; }
    public TechnologyRelationResult? TechnologyRelations { get; init; }
    public string? FunctionalitySortBy { get; init; }
    public string? FunctionalitySortDirection { get; init; }
    public string? CapabilitySortBy { get; init; }
    public string? CapabilitySortDirection { get; init; }
    public int FunctionalityPageSize { get; init; } = 10;
    public int CapabilityPageSize { get; init; } = 10;
    public string? FunctionalitySearch { get; init; }
    public string? CapabilitySearch { get; init; }
    public string? TechnologySearch { get; init; }
    public Landscape.Tsi.Application.Adoption.AdoptionProcessDetailDto? AdoptionDetail { get; init; }

    public CatalogPageResult? ImplementedContracts { get; init; }
    public CatalogPageResult? ImplementedOperationModels { get; init; }
    public CatalogPageResult? ImplementedDrivers { get; init; }
    public CatalogPageResult? ImplementedServices { get; init; }

    public CatalogPageResult? ProcessCompanies { get; init; }
    public CatalogPageResult? ProcessStandards { get; init; }
    public CatalogPageResult? ProcessServices { get; init; }

    public IReadOnlyList<VendorTechnologyDto>? VendorTechnologies { get; init; }
    public CatalogPageResult? RelatedContacts { get; init; }
    public string? ContactSearch { get; init; }
    public int ContactPage { get; init; } = 1;
}