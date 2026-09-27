namespace Landscape.Tsi.Web.Models;

public sealed record AssociateTechnologyRequest(int TecnologiaId);

public sealed record QuickCreateTechnologyDto(
    string? NombreCorporativo,
    string? NombreLocal,
    int? IdFamilia,
    int? IdVersionDesplegada,
    string? Grupo,
    int? IdEstadoAdopcion,
    int? IdPosturaRoadmap,
    string? Licenciamiento,
    string? Entorno
);
