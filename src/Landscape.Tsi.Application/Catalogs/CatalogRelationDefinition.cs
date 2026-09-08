namespace Landscape.Tsi.Application.Catalogs;

public sealed record CatalogRelationDefinition(
    string ParentCatalogCode,
    string ChildCatalogCode,
    string Type,
    string EvidenceObject,
    string? BridgeEntity = null)
{
    public string CardinalityFrom => Type == "Muchos a muchos" ? "N" : "1";
    public string CardinalityTo => Type == "Muchos a muchos" ? "M" : "N";
    public string RelationshipType => Type == "Muchos a muchos" ? "ManyToMany" : "ForeignKey";
}
