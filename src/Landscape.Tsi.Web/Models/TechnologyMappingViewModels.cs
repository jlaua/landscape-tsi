using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class TechnologyMappingViewModel
{
    public TechnologyMappingQuery Query { get; init; } = new();
    public TechnologyMappingPage Page { get; init; } = new([], new(0, 0, 0, 0, 0, 0, 0, 0), [], [], [], 1, 25, 0);
    public int TotalPages => Page.TotalCount == 0 ? 1 : (int)Math.Ceiling(Page.TotalCount / (double)Page.PageSize);
    public FamilyBuildingBlockReport FamilyReport { get; init; } = new([], [], new(0, 0, 0), null, null, 1, 10, 0);
    public int FamilyReportTotalPages => FamilyReport.TotalCount == 0 ? 1 : (int)Math.Ceiling(FamilyReport.TotalCount / (double)FamilyReport.PageSize);
}

public sealed class TechnologyRelationsViewModel
{
    public TechnologyRelationResult Relation { get; init; } = null!;
    public string? ReturnUrl { get; init; }
}

public sealed class BuildingTechnologyRelationsViewModel
{
    public BuildingTechnologyRelationResult Relation { get; init; } = null!;
    public string? Search { get; init; }
    public int? FamilyId { get; init; }
    public IReadOnlyList<FamilyOption> Families { get; init; } = [];
    public string? ReturnUrl { get; init; }
}

public sealed class UnassignedTechnologiesViewModel
{
    public UnassignedTechnologyQuery Query { get; init; } = new();
    public UnassignedTechnologyPage Page { get; init; } = new([], [], [], 1, 25, 0);
    public int TotalPages => Page.TotalCount == 0 ? 1 : (int)Math.Ceiling(Page.TotalCount / (double)Page.PageSize);
}