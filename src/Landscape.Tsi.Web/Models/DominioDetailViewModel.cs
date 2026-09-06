using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class DominioDetailViewModel
{
    public required DominioRecord Record { get; init; }
    public required DominioFormModel Edit { get; init; }
}
