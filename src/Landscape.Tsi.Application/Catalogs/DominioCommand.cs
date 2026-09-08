namespace Landscape.Tsi.Application.Catalogs;

public sealed record DominioCommand(
    string? Dominio,
    string? DescripcionDominio,
    string? Referencias,
    string? HomologacionDimensionSegunCiber,
    string? HomologacionDimensionSegunLineamiento,
    string? SubDominioCvt,
    string? Ejemplos);