namespace Landscape.Tsi.Web.Models;

public sealed record BuildingBlockCardViewModel(
    int Id,
    string Nombre,
    string? Definicion,
    string? PilarZt,
    string? FaseAdopcionNombre,
    int FuncionalidadesCount,
    int EmpresasCount);

public sealed record DomainRectCardViewModel(
    int Id,
    string Nombre,
    string? Descripcion,
    string PastelHex,
    string BorderHex,
    string TextHex,
    double PorcentajeBb,
    IReadOnlyList<BuildingBlockCardViewModel> BuildingBlocks);

public sealed class CatalogTreemapHomeViewModel
{
    public IReadOnlyList<DomainRectCardViewModel> Dominios { get; init; } = [];
    public int TotalBuildingBlocks { get; init; }
    public int TotalDominios { get; init; }
    public int TotalFuncionalidades { get; init; }
    public int TotalImplementaciones { get; init; }
    public string ActiveTab { get; set; } = "dominios";
}

public sealed record BuildingBlockDetailPopupDto(
    int Id,
    string Nombre,
    string DominioNombre,
    string? Definicion,
    string? PilarZt,
    string? FaseAdopcion,
    IReadOnlyList<FunctionalityItemDto> Funcionalidades,
    IReadOnlyList<CompanyImplementationItemDto> EmpresasImplementadas);

public sealed record FunctionalityItemDto(
    int Id,
    string Nombre,
    string? Descripcion,
    string? Estado,
    string? CapacidadSeguridadNombre = null);

public sealed record CompanyImplementationItemDto(
    int EmpresaId,
    string EmpresaNombre,
    string? Pais,
    string? TecnologiaAsIs,
    string? VersionDesplegada,
    bool EsInstanciaCorporativa);