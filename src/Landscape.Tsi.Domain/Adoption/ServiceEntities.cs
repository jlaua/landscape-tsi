namespace Landscape.Tsi.Domain.Adoption;

public sealed class TTipoServicio
{
    public int IdTipoServicio { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Orden { get; set; } = 1;
    public bool EsActivo { get; set; } = true;
}

public sealed class TServicioTecnologia
{
    public int IdServicio { get; set; }
    public string CodigoServicio { get; set; } = string.Empty;
    public string NombreServicio { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int IdTipoServicio { get; set; }
    public int? IdTecnologiaTSI { get; set; }
    public int? IdTecnologiaTSIimplementadaSubsidiaria { get; set; }
    public int? IdEmpresaSubsidiaria { get; set; }
    public int? IdProcesoAdopcionTSI { get; set; }
    public int? IdVendor { get; set; }
    public string? NombreProveedorServicio { get; set; }
    public string EstadoServicio { get; set; } = "EVALUACION";
    public decimal CostoTotalEstimado { get; set; }
    public string Moneda { get; set; } = "USD";
    public DateTime FechaCreacion { get; set; }
    public string UsuarioCreacion { get; set; } = string.Empty;
    public DateTime FechaModificacion { get; set; }
    public string UsuarioModificacion { get; set; } = string.Empty;

    public List<TTarifarioProyectoHoras> TarifariosProyecto { get; set; } = [];
    public List<TTarifarioOperacion> TarifariosOperacion { get; set; } = [];
}

public sealed class TTarifarioProyectoHoras
{
    public int IdTarifarioProyecto { get; set; }
    public int IdServicio { get; set; }
    public string Complejidad { get; set; } = string.Empty;
    public int RangoHorasDesde { get; set; }
    public int? RangoHorasHasta { get; set; }
    public decimal TarifaHora { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal Subtotal { get; set; }
    public string Moneda { get; set; } = "USD";
    public string? Observaciones { get; set; }
}

public sealed class TActividadNivelSoporte
{
    public int IdActividadSoporte { get; set; }
    public string NivelSoporte { get; set; } = string.Empty;
    public string DescripcionActividad { get; set; } = string.Empty;
    public int OrdenVisual { get; set; } = 1;
    public bool EsActivo { get; set; } = true;
}

public sealed class TTarifarioOperacion
{
    public int IdTarifarioOperacion { get; set; }
    public int IdServicio { get; set; }
    public string NivelSoporte { get; set; } = string.Empty;
    public string Modalidad { get; set; } = string.Empty;
    public string? DetalleModalidad { get; set; }
    public int HorasBaseMensual { get; set; }
    public string Expertise { get; set; } = string.Empty;
    public string Locacion { get; set; } = string.Empty;
    public decimal? TarifaHora { get; set; }
    public decimal? TarifaMensual { get; set; }
    public int CantidadMeses { get; set; } = 1;
    public decimal? HorasEstimadas { get; set; }
    public decimal Subtotal { get; set; }
    public string Moneda { get; set; } = "USD";
}