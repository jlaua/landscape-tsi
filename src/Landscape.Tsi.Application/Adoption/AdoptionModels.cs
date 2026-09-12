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
    string EstadoAlineamiento);

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
    IReadOnlyList<DriverDto> Drivers);

public sealed record ContractDto(
    int Id,
    int TecnologiaImplementadaId,
    string NumeroContrato,
    bool EsAdenda,
    int? ContratoPadreId,
    string? ContratoPadreNumero,
    DateTime FechaInicio,
    DateTime FechaFin,
    DateTime? FechaAdjudicacion,
    string? RutaDocumento,
    decimal? Monto,
    string Moneda,
    string? Observaciones,
    IReadOnlyList<ContractDto> Adendas);

public sealed record DriverDto(
    int Id,
    int TecnologiaImplementadaId,
    string Descripcion,
    string? UnidadMedida,
    decimal? Cantidad,
    decimal? PrecioUnitario,
    string Moneda,
    decimal CostoTotal);

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
    string CorrelationId);

public sealed record SaveContractCommand(
    int TecnologiaImplementadaId,
    string NumeroContrato,
    bool EsAdenda,
    int? ContratoPadreId,
    DateTime FechaInicio,
    DateTime FechaFin,
    DateTime? FechaAdjudicacion,
    string? RutaDocumento,
    decimal? Monto,
    string Moneda,
    string? Observaciones,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SaveDriverCommand(
    int TecnologiaImplementadaId,
    string Descripcion,
    string? UnidadMedida,
    decimal? Cantidad,
    decimal? PrecioUnitario,
    string Moneda,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SaveOperationModelCommand(
    int TecnologiaImplementadaId,
    int TipoOperacionId,
    int ModalidadLaboralId,
    Guid ActorUserId,
    string CorrelationId);

public sealed record AdoptionResult(bool Succeeded, string Message, int? EntityId = null);