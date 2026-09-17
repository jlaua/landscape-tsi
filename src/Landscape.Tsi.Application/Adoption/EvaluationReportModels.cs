namespace Landscape.Tsi.Application.Adoption;

/// <summary>
/// Reporte a: Alcance del proceso de evaluación por empresa subsidiaria.
/// </summary>
public sealed record EvaluationScopeReportRowDto(
    int EmpresaId,
    string EmpresaNombre,
    string? Pais,
    DateTime? FechaVencimientoContrato,
    string? TecnologiaAsIs,
    string? TipoContrato,
    string? Vendor,
    string? Partner,
    string? TipoOperacion);

/// <summary>
/// Hito temporal mensual para la matriz de vencimientos cronológicos.
/// </summary>
public sealed record ContractTimelineMilestoneDto(
    string PeriodoLabel,
    int Anio,
    int Mes,
    bool EsPaygSinVencimiento);

/// <summary>
/// Fila de vencimiento cronológico con indicadores en la línea de tiempo.
/// </summary>
public sealed record ContractExpirationRowDto(
    int Secuencia,
    int EmpresaId,
    string EmpresaNombre,
    string? ProductoActual,
    decimal ThroughputGbMes,
    int CantidadAppFqdn,
    decimal RequestWafMillonesMes,
    DateTime? FechaVencimiento,
    string? VencimientoLabel,
    IReadOnlyDictionary<string, bool> HitoExpiracionPorPeriodo,
    string? TipoContrato = null,
    string? TipoOperacion = null,
    int CantidadDrivers = 0,
    decimal CostoTotalDrivers = 0m,
    IReadOnlyDictionary<string, decimal>? DriversPorNombre = null);

/// <summary>
/// Reporte b: Fechas de vencimiento de contratos por empresa y proyección acumulada.
/// </summary>
public sealed record ContractExpirationReportDto(
    IReadOnlyList<ContractTimelineMilestoneDto> PeriodosHito,
    IReadOnlyList<ContractExpirationRowDto> Filas,
    IReadOnlyDictionary<string, decimal> ThroughputGbAcumuladoPorPeriodo,
    IReadOnlyDictionary<string, int> CantidadAppsAcumuladoPorPeriodo,
    IReadOnlyDictionary<string, decimal> RequestWafMillonesAcumuladoPorPeriodo,
    decimal TotalThroughputGbMes,
    int TotalAppsFqdn,
    decimal TotalRequestWafMillonesMes,
    IReadOnlyDictionary<string, int>? SubsidiariasAcumuladasPorPeriodo = null,
    int TotalSubsidiariasConVencimiento = 0,
    IReadOnlyList<string>? TodosLosDriversDisponibles = null,
    IReadOnlyList<string>? DriversSeleccionados = null,
    IReadOnlyDictionary<string, string>? UnidadesPorDriver = null);

/// <summary>
/// Reporte 3: Detalle de volumetría y drivers por empresa (WAAP).
/// </summary>
public sealed record CompanyVolumeReportRowDto(
    int EmpresaId,
    string EmpresaNombre,
    string? TecnologiaAsIs,
    decimal ThroughputMensualGbps,
    decimal AnchoBandaMensualGbps,
    int Dominio,
    int SubDominios,
    int CantidadAppsFqdn,
    int CantidadAppsFqdnApiSecurity,
    decimal ApiProtectionRequestMillonesMes,
    decimal MillonesRequestAntibot,
    decimal MillonesRequestWaf,
    decimal MillonesRequestAntibotWaf,
    decimal DataTransferTbMensual,
    string? RequestSize = null,
    string? ResponseSize = null,
    string? RequestSizeResponseSize = null)
{
    public int DominioSubdominios => Dominio + SubDominios;
}

/// <summary>
/// Driver operativo / de consumo dinámico por subsidiaria (DSPM, Cloud, Endpoint, etc.) proveniente de TDriver.
/// </summary>
public sealed record CompanyDriverItemDto(
    int DriverId,
    int EmpresaId,
    string EmpresaNombre,
    string? TecnologiaAsIs,
    string DescripcionDriver,
    string UnidadMedida,
    decimal Cantidad,
    decimal PrecioUnitario,
    string Moneda,
    decimal CostoTotal);

/// <summary>
/// Fila de la matriz de volumetría pivotada dinámicamente por columnas de driver (operativo puro).
/// </summary>
public sealed record CompanyDriverMatrixRowDto(
    int EmpresaId,
    string EmpresaNombre,
    string? TecnologiaAsIs,
    IReadOnlyDictionary<string, decimal> ValoresPorColumnaDriver);

/// <summary>
/// Matriz de volumetría dinámica por columnas ("[Descripción] / [Unidad]") para Building Blocks no-WAAP o genéricos.
/// </summary>
public sealed record CompanyDriverMatrixDto(
    IReadOnlyList<string> ColumnasDrivers,
    IReadOnlyList<CompanyDriverMatrixRowDto> Filas,
    IReadOnlyDictionary<string, decimal> TotalesPorColumna);

/// <summary>
/// Fila del reporte 5: Proyección de Costos AS-IS (financiero).
/// </summary>
public sealed record AsIsCostItemDto(
    int DriverId,
    int EmpresaId,
    string EmpresaNombre,
    string? TecnologiaAsIs,
    string DescripcionDriver,
    string UnidadMedida,
    decimal Cantidad,
    decimal PrecioUnitario,
    string Moneda,
    decimal CostoTotal);

/// <summary>
/// Reporte 5: Proyección de Costos AS-IS valorizado por empresa y consolidado.
/// </summary>
public sealed record AsIsCostsReportDto(
    IReadOnlyList<AsIsCostItemDto> Filas,
    decimal TotalInversionGeneral,
    int TotalItemsConCosto);

/// <summary>
/// Contenedor de Reporte 3 con subtotales consolidados y soporte para drivers dinámicos.
/// </summary>
public sealed record CompanyVolumeReportDto(
    IReadOnlyList<CompanyVolumeReportRowDto> Filas,
    decimal TotalThroughputMensualGbps,
    decimal TotalAnchoBandaMensualGbps,
    int TotalDominioSubdominios,
    int TotalAppsFqdn,
    int TotalAppsFqdnApiSecurity,
    decimal TotalApiProtectionRequestMillonesMes,
    decimal TotalMillonesRequestAntibot,
    decimal TotalMillonesRequestWaf,
    decimal TotalMillonesRequestAntibotWaf,
    decimal TotalDataTransferTbMensual,
    IReadOnlyList<CompanyDriverItemDto>? DriversGenerales = null,
    decimal TotalInversionDrivers = 0m,
    int TotalCantidadDrivers = 0,
    CompanyDriverMatrixDto? MatrizDrivers = null,
    int TotalDominio = 0,
    int TotalSubDominios = 0);

/// <summary>
/// Estado de capacidad por subsidiaria: A (Activo), F (Futuro), NA (No aplica), o texto.
/// </summary>
public sealed record CompanyCapabilityMatrixCellDto(
    int CapacidadId,
    string CapacidadNombre,
    string EstadoCodigo,
    string? Comentario);

/// <summary>
/// Fila de matriz de capacidades por subsidiaria.
/// </summary>
public sealed record CompanyCapabilityMatrixRowDto(
    int EmpresaId,
    string EmpresaNombre,
    string? TecnologiaAsIs,
    IReadOnlyDictionary<string, CompanyCapabilityMatrixCellDto> Capacidades,
    string? ComentarioSubsidiaria,
    int ProcesoEmpresaId = 0);

/// <summary>
/// Reporte 4: Matriz de Capacidades por Empresa.
/// </summary>
public sealed record CompanyCapabilitiesMatrixDto(
    IReadOnlyList<string> ColumnasCapacidades,
    IReadOnlyList<CompanyCapabilityMatrixRowDto> Filas);

/// <summary>
/// Contenedor general con los 5 reportes de la evaluación seleccionada.
/// </summary>
public sealed record EvaluationReportsDto(
    int ProcesoId,
    string CodigoProceso,
    string NombreProceso,
    int BuildingBlockId,
    string BuildingBlockNombre,
    string DominioNombre,
    string? LiderCorporativo,
    DateTime FechaInicio,
    DateTime? FechaEstimadaCierre,
    string EstadoAdopcionNombre,
    IReadOnlyList<EvaluationScopeReportRowDto> AlcanceReport,
    ContractExpirationReportDto VencimientoContratosReport,
    CompanyVolumeReportDto VolumetriaReport,
    CompanyCapabilitiesMatrixDto CapacidadesMatrixReport,
    bool EsWaap = false,
    AsIsCostsReportDto? CostosAsIsReport = null);