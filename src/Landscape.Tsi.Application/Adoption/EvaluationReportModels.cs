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
    IReadOnlyDictionary<string, bool> HitoExpiracionPorPeriodo);

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
    decimal TotalRequestWafMillonesMes);

/// <summary>
/// Reporte 3: Detalle de volumetría y drivers por empresa.
/// </summary>
public sealed record CompanyVolumeReportRowDto(
    int EmpresaId,
    string EmpresaNombre,
    string? TecnologiaAsIs,
    decimal ThroughputMensualGbps,
    decimal AnchoBandaMensualGbps,
    int DominioSubdominios,
    int CantidadAppsFqdn,
    int CantidadAppsFqdnApiSecurity,
    decimal ApiProtectionRequestMillonesMes,
    decimal MillonesRequestAntibot,
    decimal MillonesRequestWaf,
    decimal MillonesRequestAntibotWaf,
    decimal DataTransferTbMensual,
    string? RequestSizeResponseSize);

/// <summary>
/// Contenedor de Reporte 3 con subtotales consolidados.
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
    decimal TotalDataTransferTbMensual);

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
    string? ComentarioSubsidiaria);

/// <summary>
/// Reporte 4: Matriz de Capacidades por Empresa.
/// </summary>
public sealed record CompanyCapabilitiesMatrixDto(
    IReadOnlyList<string> ColumnasCapacidades,
    IReadOnlyList<CompanyCapabilityMatrixRowDto> Filas);

/// <summary>
/// Contenedor general con los 4 reportes de la evaluación seleccionada.
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
    CompanyCapabilitiesMatrixDto CapacidadesMatrixReport);
