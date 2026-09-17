using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class DominioFormModel
{
    public string? Dominio { get; set; }
    public string? DescripcionDominio { get; set; }
    public string? Referencias { get; set; }
    public string? HomologacionDimensionSegunCiber { get; set; }
    public string? HomologacionDimensionSegunLineamiento { get; set; }
    public string? SubDominioCvt { get; set; }
    public string? Ejemplos { get; set; }

    public DominioCommand ToCommand() => new(
        Dominio,
        DescripcionDominio,
        Referencias,
        HomologacionDimensionSegunCiber,
        HomologacionDimensionSegunLineamiento,
        SubDominioCvt,
        Ejemplos);

    public static DominioFormModel FromRecord(DominioRecord record) => new()
    {
        Dominio = record.Dominio,
        DescripcionDominio = record.DescripcionDominio,
        Referencias = record.Referencias,
        HomologacionDimensionSegunCiber = record.HomologacionDimensionSegunCiber,
        HomologacionDimensionSegunLineamiento = record.HomologacionDimensionSegunLineamiento,
        SubDominioCvt = record.SubDominioCvt,
        Ejemplos = record.Ejemplos
    };
}