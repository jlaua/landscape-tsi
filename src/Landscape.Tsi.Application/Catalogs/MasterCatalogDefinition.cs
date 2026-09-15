namespace Landscape.Tsi.Application.Catalogs;

public enum CatalogEditorMode
{
    Modal,
    Page
}

public enum CatalogEntityType
{
    Master,
    Transactional
}

public sealed record MasterCatalogDefinition(
    string Code,
    string Name,
    string PhysicalTable,
    string PrimaryKeyColumn,
    string Route,
    string Group,
    string DisplayColumnCode,
    CatalogEditorMode EditorMode,
    IReadOnlyList<CatalogColumnDefinition> Columns,
    IReadOnlyList<string> ListColumnCodes,
    CatalogEntityType? ExplicitEntityType = null,
    string? Description = null)
{
    public string Key => Code;
    public bool Enabled => true;
    public CatalogColumnDefinition DisplayColumn => Columns.Single(column => column.Code == DisplayColumnCode);
    public CatalogEntityType EntityType => ExplicitEntityType
        ?? (PhysicalTable.StartsWith("TM", StringComparison.Ordinal) ? CatalogEntityType.Master : CatalogEntityType.Transactional);
    public string Badge => EntityType == CatalogEntityType.Master ? "M" : "T";
    public string EntityTypeLabel => EntityType == CatalogEntityType.Master ? "Tabla maestra" : "Tabla transaccional";
    public bool IsAdministrable => Enabled;
    public bool IsReadOnly => Code is "estado-adopcion-tsi" or "fase-adopcion" or "estado-capacidad" or "estado-funcionalidad";
    public bool IsDeletable => IsAdministrable && !IsReadOnly;
    public bool UsesResponsiveDrawer => Code is "capacidad-seguridad" or "funcionalidad" or "casos-uso" or "ciso";
    public bool IncludeInReporting => Code is "dominio" or "building-block" or "capacidad-seguridad" or "funcionalidad" or "tecnologia-tsi" or "familia";
    public string NavigationRoute => Code == "dominio"
        ? "/Administration/MasterTables/Domain"
        : $"/Administration/MasterTables/{Route}";
}