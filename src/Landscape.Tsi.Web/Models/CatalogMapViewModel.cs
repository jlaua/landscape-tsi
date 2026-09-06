using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class CatalogMapViewModel
{
    public required IReadOnlyList<MasterCatalogDefinition> Catalogs { get; init; }
    public required IReadOnlyList<CatalogRelationDefinition> Relations { get; init; }
    public IEnumerable<IGrouping<string, MasterCatalogDefinition>> Groups => Catalogs.GroupBy(catalog => catalog.Group);
}
