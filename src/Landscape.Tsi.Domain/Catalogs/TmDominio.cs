namespace Landscape.Tsi.Domain.Catalogs;

public sealed class TmDominio : MasterCatalogEntity
{
    public string? Dominio { get; set; }
    public string? DescripcionDominio { get; set; }
    public string? Referencias { get; set; }
    public string? HomologacionDimensionSegunCiber { get; set; }
    public string? HomologacionDimensionSegunLineamiento { get; set; }
    public string? SubDominioCvt { get; set; }
    public string? Ejemplos { get; set; }
    public int? IdFamilia { get; set; }
}