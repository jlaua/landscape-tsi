namespace Landscape.Tsi.Web.Models;

public sealed record DomainColorItemViewModel(
    int DomainId,
    string DomainName,
    string? Description,
    int BuildingBlocksCount,
    string PastelHex,
    string BorderHex,
    string TextHex,
    bool IsCustomized);

public sealed class DomainColorsConfigViewModel
{
    public IReadOnlyList<DomainColorItemViewModel> Domains { get; init; } = [];
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class SaveDomainColorInputModel
{
    public int DomainId { get; set; }
    public string PastelHex { get; set; } = string.Empty;
    public string? BorderHex { get; set; }
    public string? TextHex { get; set; }
}