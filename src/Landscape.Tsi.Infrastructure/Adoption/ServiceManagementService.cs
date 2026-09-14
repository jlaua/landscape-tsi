using System.Data;
using System.Data.Common;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Adoption;

public sealed class ServiceManagementService(
    CatalogDbContext dbContext,
    IAuditTrailService auditTrail) : IServiceManagementService
{
    public async Task<IReadOnlyList<ServiceTypeDto>> GetServiceTypesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceTypes
            .AsNoTracking()
            .Where(t => t.EsActivo)
            .OrderBy(t => t.Orden)
            .Select(t => new ServiceTypeDto(t.IdTipoServicio, t.Codigo, t.Nombre, t.Descripcion, t.Orden))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupportActivityDto>> GetSupportActivitiesAsync(string? nivel = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SupportLevelActivities
            .AsNoTracking()
            .Where(a => a.EsActivo);

        if (!string.IsNullOrWhiteSpace(nivel))
        {
            query = query.Where(a => a.NivelSoporte == nivel);
        }

        return await query
            .OrderBy(a => a.NivelSoporte)
            .ThenBy(a => a.OrdenVisual)
            .Select(a => new SupportActivityDto(a.IdActividadSoporte, a.NivelSoporte, a.DescripcionActividad, a.OrdenVisual))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDetailDto>> GetServicesByProcessAsync(int procesoId, CancellationToken cancellationToken = default)
    {
        var processCompanyIds = await dbContext.AdoptionProcessCompanies
            .Where(pc => pc.IdProcesoAdopcionTSI == procesoId)
            .Select(pc => pc.IdProcesoAdopcionEmpresa)
            .ToListAsync(cancellationToken);

        var implTechIds = await dbContext.ImplementedTechnologies
            .Where(t => t.IdProcesoAdopcionEmpresa.HasValue && processCompanyIds.Contains(t.IdProcesoAdopcionEmpresa.Value))
            .Select(t => t.IdTecnologiaTSIimplementadaSubsidiaria)
            .ToListAsync(cancellationToken);

        var services = await dbContext.TechnologyServices
            .AsNoTracking()
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .Where(s => s.IdProcesoAdopcionTSI == procesoId ||
                        (s.IdTecnologiaTSIimplementadaSubsidiaria.HasValue && implTechIds.Contains(s.IdTecnologiaTSIimplementadaSubsidiaria.Value)))
            .OrderBy(s => s.NombreServicio)
            .ToListAsync(cancellationToken);

        return await MapToDetailListAsync(services, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDetailDto>> GetServicesByTechnologyAsync(int tecnologiaId, CancellationToken cancellationToken = default)
    {
        var services = await dbContext.TechnologyServices
            .AsNoTracking()
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .Where(s => s.IdTecnologiaTSI == tecnologiaId)
            .OrderBy(s => s.NombreServicio)
            .ToListAsync(cancellationToken);

        return await MapToDetailListAsync(services, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDetailDto>> GetServicesByImplementationAsync(int implementadaId, CancellationToken cancellationToken = default)
    {
        var services = await dbContext.TechnologyServices
            .AsNoTracking()
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .Where(s => s.IdTecnologiaTSIimplementadaSubsidiaria == implementadaId)
            .OrderBy(s => s.NombreServicio)
            .ToListAsync(cancellationToken);

        return await MapToDetailListAsync(services, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceDetailDto>> GetServicesByCompanyAsync(int empresaId, CancellationToken cancellationToken = default)
    {
        var services = await dbContext.TechnologyServices
            .AsNoTracking()
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .Where(s => s.IdEmpresaSubsidiaria == empresaId)
            .OrderBy(s => s.NombreServicio)
            .ToListAsync(cancellationToken);

        return await MapToDetailListAsync(services, cancellationToken);
    }

    public async Task<ServiceDetailDto?> GetServiceByIdAsync(int servicioId, CancellationToken cancellationToken = default)
    {
        var service = await dbContext.TechnologyServices
            .AsNoTracking()
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .FirstOrDefaultAsync(s => s.IdServicio == servicioId, cancellationToken);

        if (service is null) return null;

        var list = await MapToDetailListAsync([service], cancellationToken);
        return list.FirstOrDefault();
    }

    public async Task<ServiceResult> SaveServiceHeaderAsync(SaveServiceHeaderCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Codigo))
            return ServiceResult.Failure("El código de servicio es obligatorio.");

        if (string.IsNullOrWhiteSpace(command.Nombre))
            return ServiceResult.Failure("El nombre de servicio es obligatorio.");

        if (command.TipoServicioId <= 0)
            return ServiceResult.Failure("Debe seleccionar un tipo de servicio válido.");

        if (!command.TecnologiaTsiId.HasValue && !command.TecnologiaImplementadaId.HasValue)
            return ServiceResult.Failure("El servicio debe asociarse a una tecnología corporativa o a una tecnología implementada.");

        var resolvedEmpresaId = command.EmpresaId;
        var resolvedTechId = command.TecnologiaTsiId;
        var resolvedProcesoId = command.ProcesoAdopcionId;

        if (command.TecnologiaImplementadaId.HasValue)
        {
            var impl = await dbContext.ImplementedTechnologies
                .FirstOrDefaultAsync(t => t.IdTecnologiaTSIimplementadaSubsidiaria == command.TecnologiaImplementadaId.Value, cancellationToken);
            if (impl != null)
            {
                resolvedEmpresaId ??= impl.IdEmpresaSubsidiaria;
                resolvedTechId ??= impl.IdTecnologiaTSI;
                if (!resolvedProcesoId.HasValue && impl.IdProcesoAdopcionEmpresa.HasValue)
                {
                    var pe = await dbContext.AdoptionProcessCompanies
                        .FirstOrDefaultAsync(p => p.IdProcesoAdopcionEmpresa == impl.IdProcesoAdopcionEmpresa.Value, cancellationToken);
                    if (pe != null)
                    {
                        resolvedProcesoId = pe.IdProcesoAdopcionTSI;
                    }
                }
            }
        }

        TServicioTecnologia? entity;

        if (!command.Id.HasValue || command.Id.Value == 0)
        {
            var codeExists = await dbContext.TechnologyServices.AnyAsync(s => s.CodigoServicio == command.Codigo, cancellationToken);
            if (codeExists)
                return ServiceResult.Failure($"Ya existe un servicio con el código '{command.Codigo}'.");

            entity = new TServicioTecnologia
            {
                CodigoServicio = command.Codigo.Trim(),
                NombreServicio = command.Nombre.Trim(),
                Descripcion = command.Descripcion?.Trim(),
                IdTipoServicio = command.TipoServicioId,
                IdTecnologiaTSI = resolvedTechId,
                IdTecnologiaTSIimplementadaSubsidiaria = command.TecnologiaImplementadaId,
                IdEmpresaSubsidiaria = resolvedEmpresaId,
                IdProcesoAdopcionTSI = resolvedProcesoId,
                IdVendor = command.VendorId,
                NombreProveedorServicio = command.NombreProveedorServicio?.Trim(),
                EstadoServicio = string.IsNullOrWhiteSpace(command.Estado) ? "EVALUACION" : command.Estado.Trim(),
                Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant(),
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = command.ActorUserId.ToString(),
                FechaModificacion = DateTime.UtcNow,
                UsuarioModificacion = command.ActorUserId.ToString()
            };

            dbContext.TechnologyServices.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditTrail.RecordCreateAsync(
                "servicio-tecnologia",
                "TServicioTecnologia",
                entity.IdServicio,
                entity.NombreServicio,
                command.ActorUserId,
                command.CorrelationId,
                $"Creación de servicio '{entity.NombreServicio}' ({entity.CodigoServicio})",
                1,
                cancellationToken);

            return ServiceResult.Ok("Servicio registrado exitosamente.", entity.IdServicio);
        }

        entity = await dbContext.TechnologyServices.FirstOrDefaultAsync(s => s.IdServicio == command.Id.Value, cancellationToken);
        if (entity is null)
            return ServiceResult.Failure($"Servicio con Id {command.Id.Value} no encontrado.");

        var codeExistsOnOther = await dbContext.TechnologyServices.AnyAsync(s => s.CodigoServicio == command.Codigo && s.IdServicio != command.Id.Value, cancellationToken);
        if (codeExistsOnOther)
            return ServiceResult.Failure($"Ya existe otro servicio con el código '{command.Codigo}'.");

        entity.CodigoServicio = command.Codigo.Trim();
        entity.NombreServicio = command.Nombre.Trim();
        entity.Descripcion = command.Descripcion?.Trim();
        entity.IdTipoServicio = command.TipoServicioId;
        entity.IdTecnologiaTSI = resolvedTechId;
        entity.IdTecnologiaTSIimplementadaSubsidiaria = command.TecnologiaImplementadaId;
        entity.IdEmpresaSubsidiaria = resolvedEmpresaId;
        entity.IdProcesoAdopcionTSI = resolvedProcesoId;
        entity.IdVendor = command.VendorId;
        entity.NombreProveedorServicio = command.NombreProveedorServicio?.Trim();
        entity.EstadoServicio = string.IsNullOrWhiteSpace(command.Estado) ? entity.EstadoServicio : command.Estado.Trim();
        entity.Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? entity.Moneda : command.Moneda.Trim().ToUpperInvariant();
        entity.FechaModificacion = DateTime.UtcNow;
        entity.UsuarioModificacion = command.ActorUserId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync(
            "servicio-tecnologia",
            "TServicioTecnologia",
            entity.IdServicio,
            entity.NombreServicio,
            command.ActorUserId,
            command.CorrelationId,
            $"Actualización de servicio '{entity.NombreServicio}'",
            cancellationToken);

        return ServiceResult.Ok("Servicio actualizado exitosamente.", entity.IdServicio);
    }

    public async Task<ServiceResult> DeleteServiceAsync(int servicioId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.TechnologyServices.FirstOrDefaultAsync(s => s.IdServicio == servicioId, cancellationToken);
        if (entity is null)
            return ServiceResult.Failure($"Servicio con Id {servicioId} no encontrado.");

        var serviceName = entity.NombreServicio;
        dbContext.TechnologyServices.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync(
            "servicio-tecnologia",
            "TServicioTecnologia",
            servicioId,
            serviceName,
            actorUserId,
            correlationId,
            $"Baja/Eliminación del servicio '{serviceName}' y sus líneas de tarifario",
            cancellationToken);

        return ServiceResult.Ok("Servicio eliminado con éxito.", servicioId);
    }

    public async Task<ServiceResult> SaveProjectRateCardAsync(SaveProjectRateCardCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ServicioId <= 0)
            return ServiceResult.Failure("Servicio inválido.");

        if (string.IsNullOrWhiteSpace(command.Complejidad))
            return ServiceResult.Failure("La complejidad es obligatoria.");

        if (command.TarifaHora < 0 || command.HorasEstimadas < 0)
            return ServiceResult.Failure("Tarifa y horas no pueden ser negativas.");

        var subtotal = Math.Round(command.HorasEstimadas * command.TarifaHora, 2);

        TTarifarioProyectoHoras? item;
        if (!command.Id.HasValue || command.Id.Value == 0)
        {
            item = new TTarifarioProyectoHoras
            {
                IdServicio = command.ServicioId,
                Complejidad = command.Complejidad.Trim(),
                RangoHorasDesde = command.RangoHorasDesde,
                RangoHorasHasta = command.RangoHorasHasta,
                TarifaHora = command.TarifaHora,
                HorasEstimadas = command.HorasEstimadas,
                Subtotal = subtotal,
                Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant(),
                Observaciones = command.Observaciones?.Trim()
            };

            dbContext.ProjectRateCards.Add(item);
        }
        else
        {
            item = await dbContext.ProjectRateCards.FirstOrDefaultAsync(r => r.IdTarifarioProyecto == command.Id.Value, cancellationToken);
            if (item is null)
                return ServiceResult.Failure($"Línea de tarifario con Id {command.Id.Value} no encontrada.");

            item.Complejidad = command.Complejidad.Trim();
            item.RangoHorasDesde = command.RangoHorasDesde;
            item.RangoHorasHasta = command.RangoHorasHasta;
            item.TarifaHora = command.TarifaHora;
            item.HorasEstimadas = command.HorasEstimadas;
            item.Subtotal = subtotal;
            item.Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? item.Moneda : command.Moneda.Trim().ToUpperInvariant();
            item.Observaciones = command.Observaciones?.Trim();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateTotalAsync(command.ServicioId, cancellationToken);

        await auditTrail.RecordUpdateAsync(
            "servicio-tecnologia",
            "TServicioTecnologia",
            command.ServicioId,
            $"Tarifario Proyecto: {command.Complejidad}",
            command.ActorUserId,
            command.CorrelationId,
            $"Línea de tarifario proyecto registrada/actualizada: Subtotal {subtotal}",
            cancellationToken);

        return ServiceResult.Ok("Línea de tarifario guardada con éxito.", item.IdTarifarioProyecto);
    }

    public async Task<ServiceResult> DeleteProjectRateCardAsync(int rateCardId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ProjectRateCards.FirstOrDefaultAsync(r => r.IdTarifarioProyecto == rateCardId, cancellationToken);
        if (item is null)
            return ServiceResult.Failure($"Línea de tarifario con Id {rateCardId} no encontrada.");

        var servicioId = item.IdServicio;
        dbContext.ProjectRateCards.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateTotalAsync(servicioId, cancellationToken);

        await auditTrail.RecordUpdateAsync(
            "servicio-tecnologia",
            "TServicioTecnologia",
            servicioId,
            $"Tarifario Proyecto {rateCardId}",
            actorUserId,
            correlationId,
            $"Línea de tarifario proyecto eliminada",
            cancellationToken);

        return ServiceResult.Ok("Línea de tarifario eliminada con éxito.", rateCardId);
    }

    public async Task<ServiceResult> SaveOperationRateCardAsync(SaveOperationRateCardCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ServicioId <= 0)
            return ServiceResult.Failure("Servicio inválido.");

        if (string.IsNullOrWhiteSpace(command.NivelSoporte))
            return ServiceResult.Failure("El nivel de soporte (N1, N2, N3) es obligatorio.");

        if (string.IsNullOrWhiteSpace(command.Modalidad))
            return ServiceResult.Failure("La modalidad de operación es obligatoria.");

        decimal subtotal;
        if (command.Modalidad.StartsWith("Pay-Per-Use", StringComparison.OrdinalIgnoreCase))
        {
            var horas = command.HorasEstimadas ?? 0;
            var tarifa = command.TarifaHora ?? 0;
            subtotal = Math.Round(horas * tarifa, 2);
        }
        else
        {
            var meses = command.CantidadMeses <= 0 ? 1 : command.CantidadMeses;
            var tarifa = command.TarifaMensual ?? 0;
            subtotal = Math.Round(meses * tarifa, 2);
        }

        TTarifarioOperacion? item;
        if (!command.Id.HasValue || command.Id.Value == 0)
        {
            item = new TTarifarioOperacion
            {
                IdServicio = command.ServicioId,
                NivelSoporte = command.NivelSoporte.Trim().ToUpperInvariant(),
                Modalidad = command.Modalidad.Trim(),
                DetalleModalidad = command.DetalleModalidad?.Trim(),
                HorasBaseMensual = command.HorasBaseMensual,
                Expertise = command.Expertise.Trim(),
                Locacion = command.Locacion.Trim(),
                TarifaHora = command.TarifaHora,
                TarifaMensual = command.TarifaMensual,
                CantidadMeses = command.CantidadMeses <= 0 ? 1 : command.CantidadMeses,
                HorasEstimadas = command.HorasEstimadas,
                Subtotal = subtotal,
                Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant()
            };

            dbContext.OperationRateCards.Add(item);
        }
        else
        {
            item = await dbContext.OperationRateCards.FirstOrDefaultAsync(r => r.IdTarifarioOperacion == command.Id.Value, cancellationToken);
            if (item is null)
                return ServiceResult.Failure($"Línea de operación con Id {command.Id.Value} no encontrada.");

            item.NivelSoporte = command.NivelSoporte.Trim().ToUpperInvariant();
            item.Modalidad = command.Modalidad.Trim();
            item.DetalleModalidad = command.DetalleModalidad?.Trim();
            item.HorasBaseMensual = command.HorasBaseMensual;
            item.Expertise = command.Expertise.Trim();
            item.Locacion = command.Locacion.Trim();
            item.TarifaHora = command.TarifaHora;
            item.TarifaMensual = command.TarifaMensual;
            item.CantidadMeses = command.CantidadMeses <= 0 ? 1 : command.CantidadMeses;
            item.HorasEstimadas = command.HorasEstimadas;
            item.Subtotal = subtotal;
            item.Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? item.Moneda : command.Moneda.Trim().ToUpperInvariant();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateTotalAsync(command.ServicioId, cancellationToken);

        await auditTrail.RecordUpdateAsync(
            "servicio-tecnologia",
            "TServicioTecnologia",
            command.ServicioId,
            $"Tarifario Operación: {command.NivelSoporte} - {command.Modalidad}",
            command.ActorUserId,
            command.CorrelationId,
            $"Línea de tarifario de operación registrada/actualizada: Subtotal {subtotal}",
            cancellationToken);

        return ServiceResult.Ok("Línea de operación guardada con éxito.", item.IdTarifarioOperacion);
    }

    public async Task<ServiceResult> DeleteOperationRateCardAsync(int rateCardId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.OperationRateCards.FirstOrDefaultAsync(r => r.IdTarifarioOperacion == rateCardId, cancellationToken);
        if (item is null)
            return ServiceResult.Failure($"Línea de operación con Id {rateCardId} no encontrada.");

        var servicioId = item.IdServicio;
        dbContext.OperationRateCards.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RecalculateTotalAsync(servicioId, cancellationToken);

        await auditTrail.RecordUpdateAsync(
            "servicio-tecnologia",
            "TServicioTecnologia",
            servicioId,
            $"Tarifario Operación {rateCardId}",
            actorUserId,
            correlationId,
            $"Línea de tarifario de operación eliminada",
            cancellationToken);

        return ServiceResult.Ok("Línea de operación eliminada con éxito.", rateCardId);
    }

    private async Task RecalculateTotalAsync(int servicioId, CancellationToken cancellationToken)
    {
        var servicio = await dbContext.TechnologyServices
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .FirstOrDefaultAsync(s => s.IdServicio == servicioId, cancellationToken);

        if (servicio is null) return;

        var totalProyecto = servicio.TarifariosProyecto.Sum(p => p.Subtotal);
        var totalOperacion = servicio.TarifariosOperacion.Sum(o => o.Subtotal);
        servicio.CostoTotalEstimado = Math.Round(totalProyecto + totalOperacion, 2);
        servicio.FechaModificacion = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ServiceDetailDto>> MapToDetailListAsync(List<TServicioTecnologia> services, CancellationToken cancellationToken)
    {
        if (services.Count == 0) return [];

        var tipoMap = await dbContext.ServiceTypes.AsNoTracking().ToDictionaryAsync(t => t.IdTipoServicio, cancellationToken);
        var techMap = await dbContext.Technologies.AsNoTracking().ToDictionaryAsync(t => t.Id, cancellationToken);
        var empMap = await dbContext.Companies.AsNoTracking().ToDictionaryAsync(e => e.Id, cancellationToken);

        var list = new List<ServiceDetailDto>();
        foreach (var s in services)
        {
            var tipo = tipoMap.GetValueOrDefault(s.IdTipoServicio);
            var tech = s.IdTecnologiaTSI.HasValue ? techMap.GetValueOrDefault(s.IdTecnologiaTSI.Value) : null;
            var emp = s.IdEmpresaSubsidiaria.HasValue ? empMap.GetValueOrDefault(s.IdEmpresaSubsidiaria.Value) : null;

            var projectCards = s.TarifariosProyecto
                .OrderBy(p => p.RangoHorasDesde)
                .Select(p => new ProjectRateCardDto(
                    p.IdTarifarioProyecto,
                    p.IdServicio,
                    p.Complejidad,
                    p.RangoHorasDesde,
                    p.RangoHorasHasta,
                    p.TarifaHora,
                    p.HorasEstimadas,
                    p.Subtotal,
                    p.Moneda,
                    p.Observaciones))
                .ToList();

            var operationCards = s.TarifariosOperacion
                .OrderBy(o => o.NivelSoporte)
                .ThenBy(o => o.Modalidad)
                .Select(o => new OperationRateCardDto(
                    o.IdTarifarioOperacion,
                    o.IdServicio,
                    o.NivelSoporte,
                    o.Modalidad,
                    o.DetalleModalidad,
                    o.HorasBaseMensual,
                    o.Expertise,
                    o.Locacion,
                    o.TarifaHora,
                    o.TarifaMensual,
                    o.CantidadMeses,
                    o.HorasEstimadas,
                    o.Subtotal,
                    o.Moneda))
                .ToList();

            list.Add(new ServiceDetailDto(
                s.IdServicio,
                s.CodigoServicio,
                s.NombreServicio,
                s.Descripcion,
                s.IdTipoServicio,
                tipo?.Codigo ?? "DESCONOCIDO",
                tipo?.Nombre ?? "Desconocido",
                s.IdTecnologiaTSI,
                tech?.NombreCorporativo,
                s.IdTecnologiaTSIimplementadaSubsidiaria,
                s.IdEmpresaSubsidiaria,
                emp?.Nombre,
                s.IdProcesoAdopcionTSI,
                s.IdVendor,
                null,
                s.NombreProveedorServicio,
                s.EstadoServicio,
                s.CostoTotalEstimado,
                s.Moneda,
                s.FechaCreacion,
                projectCards,
                operationCards));
        }

        return list;
    }
}