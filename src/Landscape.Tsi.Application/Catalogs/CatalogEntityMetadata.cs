namespace Landscape.Tsi.Application.Catalogs;

public sealed record CatalogEntityMetadata(
    string Code,
    string PhysicalTableName,
    string LogicalName,
    CatalogEntityType EntityType,
    string Group,
    string? Route,
    bool IsAdministrable,
    string Description,
    bool IsDeletable = false)
{
    public string Badge => EntityType == CatalogEntityType.Master ? "M" : "T";
    public string EntityTypeLabel => EntityType == CatalogEntityType.Master ? "Tabla maestra" : "Tabla transaccional";
    public string? ControllerName => IsAdministrable ? "MasterTables" : null;
    public string? ListActionName => IsAdministrable ? (Code == "dominio" ? "Domain" : "Catalog") : null;
    public IReadOnlyDictionary<string, string>? RouteValues => IsAdministrable && Code != "dominio"
        ? new Dictionary<string, string>(StringComparer.Ordinal) { ["catalogRoute"] = Code }
        : null;
}

public sealed record CatalogRelationshipMetadata(
    string FromEntity,
    string ToEntity,
    string CardinalityFrom,
    string CardinalityTo,
    string ForeignKeyName,
    string RelationshipType,
    string? BridgeEntity = null);
