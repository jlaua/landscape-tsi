using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class CatalogPageViewModel
{
    public required MasterCatalogDefinition Definition { get; init; }
    public required CatalogPageResult Result { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> Options { get; init; }
    public string? Search { get; init; }
}
