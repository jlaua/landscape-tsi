namespace Landscape.Tsi.Application.Catalogs;

public enum CatalogEditorMode
{
    Modal,
    Page
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
    IReadOnlyList<string> ListColumnCodes)
{
    public string Key => Code;
    public bool Enabled => true;
    public CatalogColumnDefinition DisplayColumn => Columns.Single(column => column.Code == DisplayColumnCode);
}
