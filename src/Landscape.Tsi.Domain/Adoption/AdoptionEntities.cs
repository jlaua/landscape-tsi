namespace Landscape.Tsi.Domain.Adoption;

public sealed class TProcesoAdopcionTSI
{
    public int IdProcesoAdopcionTSI { get; set; }
    public string CodigoProceso { get; set; } = string.Empty;
    public string NombreProceso { get; set; } = string.Empty;
    public int IdBuildingBlock { get; set; }
    public int IdEstadoAdopcionTSI { get; set; }
    public string TipoProceso { get; set; } = "EVALUACION";
    public int? IdFamilia { get; set; }
    public int? IdTecnologiaEstandar { get; set; }
    public int? IdProcesoEvaluacionOrigen { get; set; }
    public DateTime? FechaInicioVigenciaContrato { get; set; }
    public DateTime? FechaFinVigenciaContrato { get; set; }
    public string? NumeroContratoCorporativo { get; set; }
    public string? Objetivo { get; set; }
    public string? Alcance { get; set; }
    public string? LiderCorporativoTSI { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaEstimadaCierre { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string UsuarioCreacion { get; set; } = string.Empty;
    public string? DriversReporteVencimiento { get; set; }
    public string? OrdenEmpresas { get; set; }
}

public sealed class TProcesoAdopcionEmpresa
{
    public int IdProcesoAdopcionEmpresa { get; set; }
    public int IdProcesoAdopcionTSI { get; set; }
    public int IdEmpresaSubsidiaria { get; set; }
    public int? IdContactoEmpresaSubsidiaria { get; set; }
    public bool Aplica { get; set; } = true;
    public string? JustificacionNoAplica { get; set; }
    public string? ComentarioCapacidades { get; set; }
    public DateTime FechaIncorporacion { get; set; }
    public DateTime? FechaInicioAdopcion { get; set; }
    public string? EspecialistaTecnico { get; set; }
    public int? IdContactoEspecialista { get; set; }
    public string? UrlArquitectura { get; set; }
    public string? RutaArchivoArquitectura { get; set; }
    public string? NombreArchivoArquitectura { get; set; }
    public string EstadoImplementacion { get; set; } = "PENDIENTE";
    public string? TecnologiaAsIs { get; set; }
    public DateTime FechaModificacion { get; set; }
    public string UsuarioModificacion { get; set; } = string.Empty;
}

public sealed class TEstandarTecnologiaHistorico
{
    public int IdEstandarTecnologia { get; set; }
    public int IdBuildingBlock { get; set; }
    public int IdTecnologiaTSI { get; set; }
    public int? IdProcesoAdopcionTSI { get; set; }
    public string RolEstandar { get; set; } = "PRINCIPAL";
    public string EstadoVigencia { get; set; } = "ACTIVO_VIGENTE";
    public DateTime FechaInicioVigencia { get; set; }
    public DateTime? FechaFinVigencia { get; set; }
    public string? MotivoCambio { get; set; }
    public string? SustentoArquitectura { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string UsuarioRegistro { get; set; } = string.Empty;
}

public sealed class TContratoTecnologia
{
    public int IdContratoTecnologia { get; set; }
    public int IdTecnologiaTSIimplementadaSubsidiaria { get; set; }
    public string NumeroContrato { get; set; } = string.Empty;
    public bool EsAdenda { get; set; }
    public bool EsPayg { get; set; }
    public int? IdContratoPadre { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaAdjudicacion { get; set; }
    public string? RutaDocumentoContrato { get; set; }
    public decimal? MontoContratado { get; set; }
    public decimal? MontoAnual { get; set; }
    public decimal? MontoTrianual { get; set; }
    public string Moneda { get; set; } = "USD";
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string UsuarioRegistro { get; set; } = string.Empty;
}

public sealed class TTecnologiaTSIimplementadaSubsidiaria
{
    public int IdTecnologiaTSIimplementadaSubsidiaria { get; set; }
    public int IdEmpresaSubsidiaria { get; set; }
    public int IdTecnologiaTSI { get; set; }
    public int? IdBuildingBlock { get; set; }
    public int? IdProcesoAdopcionEmpresa { get; set; }
    public bool EsTecnologiaPrimaria { get; set; } = true;
    public bool EsInstanciaCorporativa { get; set; } = false;
    public string? VersionDesplegada { get; set; }
    public int? IdVersionDesplegada { get; set; }
}

public sealed class TDriver
{
    public int IdDriver { get; set; }
    public int IdTecnologiaTSIimplementadaSubsidiaria { get; set; }
    public string? DescripcionDriver { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string Moneda { get; set; } = "USD";
}

public sealed class TDriverProceso
{
    public int IdDriverProceso { get; set; }
    public int IdProcesoAdopcionTSI { get; set; }
    public string DescripcionDriver { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = "Unidades";
    public int? OrdenVisual { get; set; } = 1;
    public DateTime? FechaCreacion { get; set; }
}

public sealed class TDriverEmpresa
{
    public int IdDriverEmpresa { get; set; }
    public int IdProcesoAdopcionEmpresa { get; set; }
    public int IdDriverProceso { get; set; }
    public decimal? Cantidad { get; set; } = 0.00m;
    public decimal? PrecioUnitario { get; set; } = 0.00m;
    public decimal? CostoTotal { get; set; } = 0.00m;
    public string Moneda { get; set; } = "USD";
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }

    public TDriverProceso? DriverProceso { get; set; }
}

public sealed class TProcesoEmpresaCapacidad
{
    public int IdProcesoEmpresaCapacidad { get; set; }
    public int IdProcesoAdopcionEmpresa { get; set; }
    public int IdCapacidad { get; set; }
    public string EstadoCobertura { get; set; } = "NA";
    public string? Comentario { get; set; }
    public DateTime FechaModificacion { get; set; }
    public string UsuarioModificacion { get; set; } = "SYSTEM";
}