using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class CatalogEditorViewModel
{
    public required MasterCatalogDefinition Definition { get; init; }
    public CatalogRow? Record { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> Options { get; init; }
    public bool IsEdit => Record is not null;
}