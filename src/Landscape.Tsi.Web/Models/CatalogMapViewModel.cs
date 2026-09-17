using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class CatalogMapViewModel
{
    public required IReadOnlyList<CatalogEntityMetadata> Entities { get; init; }
    public required IReadOnlyList<CatalogRelationshipMetadata> Relationships { get; init; }
    public string EntityMetadataJson => Serialize(Entities.Select(entity => new EntityPayload(
        entity.PhysicalTableName,
        entity.PhysicalTableName,
        entity.LogicalName,
        entity.EntityType.ToString(),
        entity.Badge,
        entity.Group,
        entity.Route,
        entity.IsAdministrable,
        entity.Description)));

    public string RelationshipMetadataJson
    {
        get
        {
            var entitiesByCode = Entities.ToDictionary(entity => entity.Code, StringComparer.Ordinal);
            return Serialize(Relationships.Select((relationship, index) =>
            {
                var source = entitiesByCode[relationship.FromEntity].PhysicalTableName;
                var target = entitiesByCode[relationship.ToEntity].PhysicalTableName;
                var bridge = relationship.BridgeEntity is null ? null : entitiesByCode[relationship.BridgeEntity].PhysicalTableName;
                return new RelationshipPayload(
                    $"relation-{index}",
                    source,
                    target,
                    relationship.CardinalityFrom,
                    relationship.CardinalityTo,
                    relationship.ForeignKeyName,
                    relationship.RelationshipType,
                    bridge);
            }));
        }
    }

    private static string Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, JsonOptions);

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    private sealed record EntityPayload(
        string Id,
        string PhysicalTableName,
        string LogicalName,
        string EntityType,
        string Badge,
        string Group,
        string? Route,
        bool IsAdministrable,
        string Description);

    private sealed record RelationshipPayload(
        string Id,
        string Source,
        string Target,
        string SourceCardinality,
        string TargetCardinality,
        string ForeignKeyName,
        string RelationshipType,
        string? BridgeEntity);
}