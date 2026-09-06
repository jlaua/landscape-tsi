namespace Landscape.Tsi.Application.Catalogs;

public sealed record CatalogRelationDefinition(
    string ParentCatalogCode,
    string ChildCatalogCode,
    string Type,
    string EvidenceObject);
