namespace Landscape.Tsi.Application.Catalogs;

public enum CatalogFieldType
{
    Text,
    DateTime,
    ForeignKey
}

public sealed record CatalogColumnDefinition(
    string Code,
    string PhysicalName,
    string Label,
    CatalogFieldType Type = CatalogFieldType.Text,
    bool IsNullable = true,
    bool IsMultiline = false,
    string? ReferenceCatalogCode = null);