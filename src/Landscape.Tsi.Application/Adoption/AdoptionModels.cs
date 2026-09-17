namespace Landscape.Tsi.Application.Adoption;

public sealed record AdoptionProcessSummaryDto(
    int Id,
    string Codigo,
    string Nombre,
    int BuildingBlockId,
    string BuildingBlockNombre,
    int EstadoAdopcionId,
    string EstadoAdopcionNombre,
    string? LiderCorporativo,
    DateTime FechaInicio,
    DateTime? FechaEstimadaCierre,
    int TotalEmpresasConvocadas,
    int TotalEmpresasImplementadas,
    int TotalEmpresasNoAplica);

public sealed record AdoptionProcessDetailDto(
    int Id,
    string Codigo,
    string Nombre,
    int BuildingBlockId,
    string BuildingBlockNombre,
    string? DominioNombre,
    string? FamiliaNombre,
    int EstadoAdopcionId,
    string EstadoAdopcionNombre,
    string? Objetivo,
    string? Alcance,
    string? LiderCorporativo,
    DateTime FechaInicio,
    DateTime? FechaEstimadaCierre,
    StandardTechnologyDto? EstandarPrincipalVigente,
    IReadOnlyList<StandardTechnologyDto> EstandaresAlternativos,
    IReadOnlyList<StandardTechnologyDto> EstandaresHistoricos,
    IReadOnlyList<CompanyAdoptionRowDto> EmpresasParticipantes);

public sealed record CompanyCapabilityItemDto(
    int Id,
    int CapacidadId,
    string CapacidadNombre,
    string EstadoCodigo,
    string? Comentario,
    int OrdenVisualizacion = 0);

public sealed record CompanyAdoptionRowDto(
    int ProcesoEmpresaId,
    int EmpresaId,
    string EmpresaNombre,
    int? ContactoFocalId,
    string? ContactoFocalNombre,
    string? ContactoFocalEmail,
    bool Aplica,
    string? JustificacionNoAplica,
    DateTime FechaIncorporacion,
    IReadOnlyList<ImplementedTechnologyDto> TecnologiasImplementadas,
    string EstadoAlineamiento,
    IReadOnlyList<CompanyCapabilityItemDto>? Capacidades = null,
    string? ComentarioCapacidades = null);

public sealed record StandardTechnologyDto(
    int Id,
    int BuildingBlockId,
    int TecnologiaId,
    string TecnologiaNombre,
    string? VendorNombre,
    string? ContactoVendorNombre,
    string? ContactoPartnerNombre,
    string RolEstandar,
    string EstadoVigencia,
    DateTime FechaInicioVigencia,
    DateTime? FechaFinVigencia,
    string? MotivoCambio,
    string? SustentoArquitectura,
    IReadOnlyList<string> CasosDeUso);

public sealed record ImplementedTechnologyDto(
    int Id,
    int EmpresaId,
    string EmpresaNombre,
    int BuildingBlockId,
    int TecnologiaId,
    string TecnologiaNombre,
    string? VendorNombre,
    bool EsPrimaria,
    string? VersionDesplegada,
    string EstadoAlineamiento,
    OperationModelDto? ModeloOperacion,
    IReadOnlyList<ContractDto> Contratos,
    IReadOnlyList<DriverDto> Drivers,
    bool EsInstanciaCorporativa = false);

public sealed record ContractDto(
    int Id,
    int TecnologiaImplementadaId,
    string NumeroContrato,
    bool EsAdenda,
    int? ContratoPadreId,
    string? ContratoPadreNumero,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    DateTime? FechaAdjudicacion,
    string? RutaDocumento,
    decimal? Monto,
    string Moneda,
    string? Observaciones,
    IReadOnlyList<ContractDto> Adendas,
    bool EsPayg = false,
    decimal? MontoAnual = null,
    decimal? MontoTrianual = null);

public sealed record DriverDto(
    int Id,
    int TecnologiaImplementadaId,
    string Descripcion,
    string? UnidadMedida,
    decimal? Cantidad,
    decimal? PrecioUnitario,
    string Moneda,
    decimal Subtotal)
{
    public decimal CostoTotal => Subtotal;
}

public sealed record OperationModelDto(
    int Id,
    int TecnologiaImplementadaId,
    int? TipoOperacionId,
    string? TipoOperacionNombre,
    int? ModalidadLaboralId,
    string? ModalidadLaboralNombre);

public sealed record CreateAdoptionProcessCommand(
    string Codigo,
    string Nombre,
    int BuildingBlockId,
    int EstadoAdopcionId,
    string? Objetivo,
    string? Alcance,
    string? LiderCorporativo,
    DateTime FechaInicio,
    DateTime? FechaEstimadaCierre,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ConveneCompanyCommand(
    int ProcesoId,
    int EmpresaId,
    int? ContactoFocalId,
    bool Aplica,
    string? JustificacionNoAplica,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SetCorporateStandardCommand(
    int BuildingBlockId,
    int TecnologiaId,
    int? ProcesoAdopcionId,
    string RolEstandar,
    DateTime FechaInicio,
    string? MotivoCambio,
    string? SustentoArquitectura,
    Guid ActorUserId,
    string CorrelationId);

public sealed record RegisterImplementedTechnologyCommand(
    int EmpresaId,
    int TecnologiaId,
    int BuildingBlockId,
    int? ProcesoEmpresaId,
    bool EsPrimaria,
    string? VersionDesplegada,
    Guid ActorUserId,
    string CorrelationId,
    bool EsInstanciaCorporativa = false);

public sealed record SaveContractCommand(
    int TecnologiaImplementadaId,
    string NumeroContrato,
    bool EsAdenda,
    int? ContratoPadreId,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    DateTime? FechaAdjudicacion,
    string? RutaDocumento,
    decimal? Monto,
    string Moneda,
    string? Observaciones,
    Guid ActorUserId,
    string CorrelationId,
    bool EsPayg = false,
    int? ContratoId = null,
    decimal? MontoAnual = null,
    decimal? MontoTrianual = null);

public sealed record SaveDriverCommand(
    int TecnologiaImplementadaId,
    string Descripcion,
    string? UnidadMedida,
    decimal? Cantidad,
    decimal? PrecioUnitario,
    string Moneda,
    Guid ActorUserId,
    string CorrelationId,
    int? DriverId = null);

public sealed record SaveOperationModelCommand(
    int TecnologiaImplementadaId,
    int? TipoOperacionId,
    int? ModalidadLaboralId,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ConveneCompanyInput(
    int EmpresaId,
    int? ContactoFocalId,
    bool Aplica,
    string? JustificacionNoAplica);

public sealed record CorporateDriverItemDto(
    string Descripcion,
    string? UnidadMedida,
    decimal? Cantidad,
    decimal? PrecioUnitario,
    string? Moneda);

public sealed record FinalizeEvaluationWithStandardCommand(
    int ProcesoId,
    int BuildingBlockId,
    int TecnologiaId,
    string RolEstandar,
    DateTime FechaInicioVigencia,
    string? MotivoAdjudicacion,
    string? SustentoArquitectura,
    string? NumeroContratoCorporativo,
    decimal? MontoContratoCorporativo,
    string? MonedaContratoCorporativo,
    DateTime? FechaInicioContratoCorporativo,
    DateTime? FechaFinContratoCorporativo,
    DateTime? FechaAdjudicacionContratoCorporativo,
    bool EsPaygContratoCorporativo,
    IReadOnlyList<CorporateDriverItemDto>? DriversCorporativos,
    IReadOnlyList<int>? SubsidiariasAlineadasIds,
    Guid ActorUserId,
    string CorrelationId);

public sealed record BuildingBlockCapabilitiesDto(
    int BuildingBlockId,
    string BuildingBlockNombre,
    string DominioNombre,
    IReadOnlyList<CapabilitySummaryDto> Capacidades,
    string? FamiliaNombre = null);

public sealed record CapabilitySummaryDto(
    int Id,
    string Nombre,
    string? Estado,
    IReadOnlyList<string> Funcionalidades);

public sealed record AdoptionResult(bool Succeeded, string Message, int? EntityId = null);