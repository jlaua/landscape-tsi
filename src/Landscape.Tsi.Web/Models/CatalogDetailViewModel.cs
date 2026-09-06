using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class CatalogDetailViewModel
{
    public required MasterCatalogDefinition Definition { get; init; }
    public required CatalogRow Record { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>> Options { get; init; }
}
