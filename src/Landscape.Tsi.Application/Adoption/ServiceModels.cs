namespace Landscape.Tsi.Application.Adoption;

public sealed record ServiceTypeDto(
    int Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    int Orden);

public sealed record SupportActivityDto(
    int Id,
    string NivelSoporte,
    string Descripcion,
    int OrdenVisual);

public sealed record ServiceDetailDto(
    int Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    int TipoServicioId,
    string TipoServicioCodigo,
    string TipoServicioNombre,
    int? TecnologiaTsiId,
    string? TecnologiaTsiNombre,
    int? TecnologiaImplementadaId,
    int? EmpresaId,
    string? EmpresaNombre,
    int? ProcesoAdopcionId,
    int? VendorId,
    string? VendorNombre,
    string? NombreProveedorServicio,
    string Estado,
    decimal CostoTotalEstimado,
    string Moneda,
    DateTime FechaCreacion,
    IReadOnlyList<ProjectRateCardDto> TarifariosProyecto,
    IReadOnlyList<OperationRateCardDto> TarifariosOperacion);

public sealed record ProjectRateCardDto(
    int Id,
    int ServicioId,
    string Complejidad,
    int RangoHorasDesde,
    int? RangoHorasHasta,
    decimal TarifaHora,
    decimal HorasEstimadas,
    decimal Subtotal,
    string Moneda,
    string? Observaciones);

public sealed record OperationRateCardDto(
    int Id,
    int ServicioId,
    string NivelSoporte,
    string Modalidad,
    string? DetalleModalidad,
    int HorasBaseMensual,
    string Expertise,
    string Locacion,
    decimal? TarifaHora,
    decimal? TarifaMensual,
    int CantidadMeses,
    decimal? HorasEstimadas,
    decimal Subtotal,
    string Moneda);

public sealed record SaveServiceHeaderCommand(
    int? Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    int TipoServicioId,
    int? TecnologiaTsiId,
    int? TecnologiaImplementadaId,
    int? EmpresaId,
    int? ProcesoAdopcionId,
    int? VendorId,
    string? NombreProveedorServicio,
    string Estado,
    string Moneda,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SaveProjectRateCardCommand(
    int? Id,
    int ServicioId,
    string Complejidad,
    int RangoHorasDesde,
    int? RangoHorasHasta,
    decimal TarifaHora,
    decimal HorasEstimadas,
    string Moneda,
    string? Observaciones,
    Guid ActorUserId,
    string CorrelationId);

public sealed record SaveOperationRateCardCommand(
    int? Id,
    int ServicioId,
    string NivelSoporte,
    string Modalidad,
    string? DetalleModalidad,
    int HorasBaseMensual,
    string Expertise,
    string Locacion,
    decimal? TarifaHora,
    decimal? TarifaMensual,
    int CantidadMeses,
    decimal? HorasEstimadas,
    string Moneda,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ServiceResult(bool Success, string Message, int? EntityId = null)
{
    public static ServiceResult Ok(string message, int? entityId = null) => new(true, message, entityId);
    public static ServiceResult Failure(string message) => new(false, message);
}