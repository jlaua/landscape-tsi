using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class DominioDetailViewModel
{
    public required DominioRecord Record { get; init; }
    public required DominioFormModel Edit { get; init; }
    public required CatalogPageResult RelatedBuildingBlocks { get; init; }
    public required DeletionImpactResult DependencyImpact { get; init; }
    public string? RelatedSearch { get; init; }
}
