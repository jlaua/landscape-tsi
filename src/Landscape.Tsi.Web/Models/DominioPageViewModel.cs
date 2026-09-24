using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class DominioPageViewModel
{
    public required PagedResult<DominioRecord> Result { get; init; }
    public string? Search { get; init; }
    public string? SortColumn { get; init; }
    public string? SortDirection { get; init; }
    public DominioFormModel Create { get; init; } = new();
}