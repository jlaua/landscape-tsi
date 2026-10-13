using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Domain.Adoption;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Adoption;

public sealed partial class AdoptionProcessService
{
    public async Task<IReadOnlyList<ImplementationProcessSummaryDto>> ListImplementationsAsync(CancellationToken cancellationToken = default)
    {
        var processes = await dbContext.AdoptionProcesses.AsNoTracking()
            .Where(p => p.TipoProceso == "IMPLEMENTACION")
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync(cancellationToken);

        if (processes.Count == 0) return [];

        var families = await dbContext.Families.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.Nombre ?? string.Empty, cancellationToken);
        var technologies = await dbContext.Technologies.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.NombreCorporativo ?? string.Empty, cancellationToken);
        var buildingBlocks = await dbContext.BuildingBlocks.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? string.Empty, cancellationToken);
        var states = await dbContext.TechnologyAdoptionStates.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Nombre ?? string.Empty, cancellationToken);
        var processCompanies = await dbContext.AdoptionProcessCompanies.AsNoTracking().ToListAsync(cancellationToken);

        var list = new List<ImplementationProcessSummaryDto>();
        foreach (var p in processes)
        {
            var empresasProceso = processCompanies.Where(cp => cp.IdProcesoAdopcionTSI == p.IdProcesoAdopcionTSI).ToList();
            var totalConvocadas = empresasProceso.Count;
            var totalNoAplica = empresasProceso.Count(cp => cp.EstadoImplementacion == "NO_APLICA");
            var totalAlineadas = empresasProceso.Count(cp => cp.EstadoImplementacion == "IMPLEMENTADO" || cp.EstadoImplementacion == "ALINEADO");
            var totalEnDespliegue = empresasProceso.Count(cp => cp.EstadoImplementacion == "EN_DESPLIEGUE");
            var totalPendientes = empresasProceso.Count(cp => cp.EstadoImplementacion == "PENDIENTE" || string.IsNullOrWhiteSpace(cp.EstadoImplementacion));

            var pct = totalConvocadas > 0 ? Math.Round((double)totalAlineadas / totalConvocadas * 100.0, 1) : 0.0;

            var estadoNombre = states.GetValueOrDefault(p.IdEstadoAdopcionTSI, "Planificada");
            var isBaja = p.NombreProceso.Contains("CANCELAD", StringComparison.OrdinalIgnoreCase)
                         || p.CodigoProceso.StartsWith("BAJA-", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(estadoNombre, "CANCELADO", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(estadoNombre, "CANCELADA", StringComparison.OrdinalIgnoreCase)
                         || p.NombreProceso.StartsWith("[DADO DE BAJA", StringComparison.OrdinalIgnoreCase)
                         || p.NombreProceso.StartsWith("[DADA DE BAJA", StringComparison.OrdinalIgnoreCase);

            var isTerminada = !isBaja && (
                string.Equals(estadoNombre, "IMPLEMENTADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estadoNombre, "ESTANDARIZADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estadoNombre, "TERMINADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estadoNombre, "TERMINADO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estadoNombre, "FINALIZADA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(estadoNombre, "CERRADA", StringComparison.OrdinalIgnoreCase)
            );

            var isActivo = !isBaja && !isTerminada;

            var famName = p.IdFamilia.HasValue ? families.GetValueOrDefault(p.IdFamilia.Value, "Sin Familia") : "Sin Familia";
            var techName = p.IdTecnologiaEstandar.HasValue ? technologies.GetValueOrDefault(p.IdTecnologiaEstandar.Value, "Por definir") : "Por definir";
            var bbName = p.IdBuildingBlock > 0 ? buildingBlocks.GetValueOrDefault(p.IdBuildingBlock, "General TSI") : "General TSI";

            list.Add(new ImplementationProcessSummaryDto(
                p.IdProcesoAdopcionTSI,
                p.CodigoProceso,
                p.NombreProceso,
                p.IdFamilia,
                famName,
                p.IdTecnologiaEstandar,
                techName,
                p.IdBuildingBlock,
                bbName,
                p.FechaInicioVigenciaContrato,
                p.FechaFinVigenciaContrato,
                p.NumeroContratoCorporativo,
                p.IdEstadoAdopcionTSI,
                estadoNombre,
                p.LiderCorporativoTSI,
                p.FechaInicio,
                p.FechaEstimadaCierre,
                totalConvocadas,
                totalAlineadas,
                totalEnDespliegue,
                totalPendientes,
                totalNoAplica,
                pct,
                isActivo,
                isTerminada,
                isBaja));
        }

        return list;
    }

    public async Task<ImplementationProcessDetailDto?> GetImplementationDetailAsync(int procesoId, CancellationToken cancellationToken = default)
    {
        var p = await dbContext.AdoptionProcesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdProcesoAdopcionTSI == procesoId && x.TipoProceso == "IMPLEMENTACION", cancellationToken);

        if (p is null) return null;

        var famName = "Sin Familia";
        if (p.IdFamilia.HasValue)
        {
            var fam = await dbContext.Families.AsNoTracking().FirstOrDefaultAsync(f => f.Id == p.IdFamilia.Value, cancellationToken);
            if (fam?.Nombre is not null) famName = fam.Nombre;
        }

        var allTechnologies = await dbContext.Technologies.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.NombreCorporativo ?? string.Empty, cancellationToken);

        var techName = "Por definir";
        string? vendorName = null;
        if (p.IdTecnologiaEstandar.HasValue && allTechnologies.TryGetValue(p.IdTecnologiaEstandar.Value, out var tCorporateName) && !string.IsNullOrWhiteSpace(tCorporateName))
        {
            techName = tCorporateName;
            var vendor = await dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.IdTecnologiaTSI == p.IdTecnologiaEstandar.Value, cancellationToken);
            if (vendor?.NombreVendor is not null) vendorName = vendor.NombreVendor;
        }

        var bbName = "General TSI";
        var domName = "Dominio TSI";
        if (p.IdBuildingBlock > 0)
        {
            var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == p.IdBuildingBlock, cancellationToken);
            if (bb is not null)
            {
                bbName = bb.Nombre ?? bbName;
                if (bb.IdDominio.HasValue)
                {
                    var dom = await dbContext.Domains.AsNoTracking().FirstOrDefaultAsync(d => d.Id == bb.IdDominio.Value, cancellationToken);
                    if (dom?.Dominio is not null) domName = dom.Dominio;
                }
            }
        }

        var estadoNombre = "Planificada";
        var estado = await dbContext.TechnologyAdoptionStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == p.IdEstadoAdopcionTSI, cancellationToken);
        if (estado?.Nombre is not null) estadoNombre = estado.Nombre;

        string? origenNombre = null;
        if (p.IdProcesoEvaluacionOrigen.HasValue)
        {
            var origen = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(o => o.IdProcesoAdopcionTSI == p.IdProcesoEvaluacionOrigen.Value, cancellationToken);
            if (origen is not null) origenNombre = $"{origen.CodigoProceso} - {origen.NombreProceso}";
        }

        var companies = await dbContext.Companies.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var processCompanies = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .Where(cp => cp.IdProcesoAdopcionTSI == procesoId)
            .ToListAsync(cancellationToken);

        var orderedCompanyIds = ParseOrderedCompanyIds(p.OrdenEmpresas);
        var companyOrderMap = new Dictionary<int, int>();
        for (var i = 0; i < orderedCompanyIds.Count; i++)
        {
            companyOrderMap[orderedCompanyIds[i]] = i;
        }

        var orderedProcessCompanies = processCompanies
            .OrderBy(cp => companyOrderMap.GetValueOrDefault(cp.IdEmpresaSubsidiaria, int.MaxValue))
            .ThenBy(cp => companies.TryGetValue(cp.IdEmpresaSubsidiaria, out var c) ? c.Nombre : string.Empty)
            .ToList();

        var existingImplTechByCompany = new Dictionary<int, string>();
        if (p.IdBuildingBlock > 0)
        {
            var implTechRows = await dbContext.ImplementedTechnologies.AsNoTracking()
                .Where(it => it.IdBuildingBlock == p.IdBuildingBlock)
                .OrderByDescending(it => it.EsTecnologiaPrimaria ? 1 : 0)
                .ThenByDescending(it => it.IdTecnologiaTSIimplementadaSubsidiaria)
                .ToListAsync(cancellationToken);

            foreach (var it in implTechRows)
            {
                if (!existingImplTechByCompany.ContainsKey(it.IdEmpresaSubsidiaria))
                {
                    var tName = allTechnologies.GetValueOrDefault(it.IdTecnologiaTSI, string.Empty);
                    if (!string.IsNullOrWhiteSpace(tName))
                    {
                        existingImplTechByCompany[it.IdEmpresaSubsidiaria] = tName;
                    }
                }
            }
        }

        var empDtos = new List<ImplementationCompanyRowDto>();
        foreach (var cp in orderedProcessCompanies)
        {
            companies.TryGetValue(cp.IdEmpresaSubsidiaria, out var comp);
            var asIs = !string.IsNullOrWhiteSpace(cp.TecnologiaAsIs)
                ? cp.TecnologiaAsIs
                : (cp.EstadoImplementacion == "IMPLEMENTADO" || cp.EstadoImplementacion == "ALINEADO"
                    ? (techName != "Por definir" ? techName : null)
                    : existingImplTechByCompany.GetValueOrDefault(cp.IdEmpresaSubsidiaria));

            empDtos.Add(new ImplementationCompanyRowDto(
                cp.IdProcesoAdopcionEmpresa,
                cp.IdEmpresaSubsidiaria,
                comp?.Nombre ?? $"Empresa #{cp.IdEmpresaSubsidiaria}",
                comp?.Pais,
                comp?.Rubro,
                cp.Aplica,
                cp.JustificacionNoAplica,
                cp.FechaInicioAdopcion,
                cp.EspecialistaTecnico,
                cp.IdContactoEspecialista,
                cp.UrlArquitectura,
                cp.RutaArchivoArquitectura,
                cp.NombreArchivoArquitectura,
                string.IsNullOrWhiteSpace(cp.EstadoImplementacion) ? (cp.Aplica ? "PENDIENTE" : "NO_APLICA") : cp.EstadoImplementacion,
                cp.FechaIncorporacion,
                asIs));
        }

        return new ImplementationProcessDetailDto(
            p.IdProcesoAdopcionTSI,
            p.CodigoProceso,
            p.NombreProceso,
            p.IdFamilia,
            famName,
            p.IdTecnologiaEstandar,
            techName,
            vendorName,
            p.IdBuildingBlock,
            bbName,
            domName,
            p.FechaInicioVigenciaContrato,
            p.FechaFinVigenciaContrato,
            p.NumeroContratoCorporativo,
            p.IdEstadoAdopcionTSI,
            estadoNombre,
            p.Objetivo,
            p.Alcance,
            p.LiderCorporativoTSI,
            p.FechaInicio,
            p.FechaEstimadaCierre,
            p.IdProcesoEvaluacionOrigen,
            origenNombre,
            empDtos);
    }

    public async Task<AdoptionResult> CreateImplementationProcessAsync(CreateImplementationProcessCommand command, CancellationToken cancellationToken = default)
    {
        var code = string.IsNullOrWhiteSpace(command.Codigo)
            ? $"IMPL-TSI-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}"
            : command.Codigo.Trim().ToUpperInvariant();

        var exists = await dbContext.AdoptionProcesses.AnyAsync(x => x.CodigoProceso == code, cancellationToken);
        if (exists)
        {
            return new AdoptionResult(false, $"Ya existe una solicitud o proceso con el código '{code}'.");
        }

        var process = new TProcesoAdopcionTSI
        {
            TipoProceso = "IMPLEMENTACION",
            CodigoProceso = code,
            NombreProceso = command.Nombre.Trim(),
            IdFamilia = command.FamiliaId,
            IdTecnologiaEstandar = command.TecnologiaEstandarId,
            IdBuildingBlock = command.BuildingBlockId ?? 0,
            FechaInicioVigenciaContrato = command.FechaInicioVigenciaContrato,
            FechaFinVigenciaContrato = command.FechaFinVigenciaContrato,
            NumeroContratoCorporativo = command.NumeroContratoCorporativo?.Trim(),
            IdEstadoAdopcionTSI = command.EstadoAdopcionId > 0 ? command.EstadoAdopcionId : 1,
            Objetivo = command.Objetivo?.Trim(),
            Alcance = command.Alcance?.Trim(),
            LiderCorporativoTSI = command.LiderCorporativo?.Trim(),
            FechaInicio = command.FechaInicio,
            FechaEstimadaCierre = command.FechaEstimadaCierre,
            IdProcesoEvaluacionOrigen = command.ProcesoEvaluacionOrigenId,
            FechaCreacion = DateTime.UtcNow,
            UsuarioCreacion = "SYSTEM"
        };

        dbContext.AdoptionProcesses.Add(process);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (command.Empresas != null)
        {
            foreach (var emp in command.Empresas)
            {
                var estado = emp.Aplica
                    ? (string.IsNullOrWhiteSpace(emp.EstadoImplementacion) ? "PENDIENTE" : emp.EstadoImplementacion)
                    : "NO_APLICA";

                var pae = new TProcesoAdopcionEmpresa
                {
                    IdProcesoAdopcionTSI = process.IdProcesoAdopcionTSI,
                    IdEmpresaSubsidiaria = emp.EmpresaId,
                    IdContactoEmpresaSubsidiaria = emp.ContactoId,
                    Aplica = emp.Aplica,
                    JustificacionNoAplica = emp.Aplica ? null : emp.JustificacionNoAplica?.Trim(),
                    FechaInicioAdopcion = emp.FechaInicioAdopcion,
                    EspecialistaTecnico = emp.EspecialistaTecnico?.Trim(),
                    UrlArquitectura = emp.UrlArquitectura?.Trim(),
                    EstadoImplementacion = estado,
                    TecnologiaAsIs = emp.TecnologiaAsIs?.Trim(),
                    FechaIncorporacion = DateTime.UtcNow.Date,
                    FechaModificacion = DateTime.UtcNow,
                    UsuarioModificacion = "SYSTEM"
                };

                dbContext.AdoptionProcessCompanies.Add(pae);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await auditTrail.RecordCreateAsync("proceso-implementacion", "TProcesoAdopcionTSI", process.IdProcesoAdopcionTSI, process.NombreProceso,
            command.ActorUserId, command.CorrelationId, $"Creación de Solicitud de Implementación '{process.CodigoProceso}'.", 1, cancellationToken);

        return new AdoptionResult(true, "Solicitud de implementación creada exitosamente.", process.IdProcesoAdopcionTSI);
    }

    public async Task<AdoptionResult> UpdateImplementationProcessAsync(UpdateImplementationProcessCommand command, CancellationToken cancellationToken = default)
    {
        var proc = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == command.ProcesoId && p.TipoProceso == "IMPLEMENTACION", cancellationToken);
        if (proc is null)
        {
            return new AdoptionResult(false, $"No se encontró la solicitud de implementación #{command.ProcesoId}.");
        }

        proc.NombreProceso = command.Nombre.Trim();
        proc.IdFamilia = command.FamiliaId;
        proc.IdTecnologiaEstandar = command.TecnologiaEstandarId;
        if (command.BuildingBlockId.HasValue && command.BuildingBlockId.Value > 0)
        {
            proc.IdBuildingBlock = command.BuildingBlockId.Value;
        }
        proc.FechaInicioVigenciaContrato = command.FechaInicioVigenciaContrato;
        proc.FechaFinVigenciaContrato = command.FechaFinVigenciaContrato;
        proc.NumeroContratoCorporativo = command.NumeroContratoCorporativo?.Trim();
        proc.IdEstadoAdopcionTSI = command.EstadoAdopcionId;
        proc.Objetivo = command.Objetivo?.Trim();
        proc.Alcance = command.Alcance?.Trim();
        proc.LiderCorporativoTSI = command.LiderCorporativo?.Trim();
        proc.FechaInicio = command.FechaInicio;
        proc.FechaEstimadaCierre = command.FechaEstimadaCierre;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-implementacion", "TProcesoAdopcionTSI", proc.IdProcesoAdopcionTSI, proc.NombreProceso,
            command.ActorUserId, command.CorrelationId, $"Actualización de cabecera de Solicitud de Implementación '{proc.CodigoProceso}'.", cancellationToken);

        return new AdoptionResult(true, "Solicitud de implementación actualizada exitosamente.", proc.IdProcesoAdopcionTSI);
    }

    public async Task<AdoptionResult> UpdateImplementationCompanyAsync(UpdateImplementationCompanyCommand command, CancellationToken cancellationToken = default)
    {
        var pae = await dbContext.AdoptionProcessCompanies.FirstOrDefaultAsync(x => x.IdProcesoAdopcionEmpresa == command.ProcesoEmpresaId && x.IdProcesoAdopcionTSI == command.ProcesoId, cancellationToken);
        if (pae is null)
        {
            return new AdoptionResult(false, $"No se encontró el registro de la subsidiaria en el proceso #{command.ProcesoId}.");
        }

        pae.Aplica = command.Aplica;
        pae.JustificacionNoAplica = command.Aplica ? null : command.JustificacionNoAplica?.Trim();
        pae.FechaInicioAdopcion = command.FechaInicioAdopcion;
        pae.EspecialistaTecnico = command.EspecialistaTecnico?.Trim();
        pae.IdContactoEspecialista = command.IdContactoEspecialista;
        pae.UrlArquitectura = command.UrlArquitectura?.Trim();
        if (!string.IsNullOrWhiteSpace(command.RutaArchivoArquitectura))
        {
            pae.RutaArchivoArquitectura = command.RutaArchivoArquitectura;
            pae.NombreArchivoArquitectura = command.NombreArchivoArquitectura;
        }
        pae.EstadoImplementacion = command.Aplica
            ? (string.IsNullOrWhiteSpace(command.EstadoImplementacion) ? "PENDIENTE" : command.EstadoImplementacion)
            : "NO_APLICA";
        pae.TecnologiaAsIs = command.TecnologiaAsIs?.Trim();
        pae.FechaModificacion = DateTime.UtcNow;
        pae.UsuarioModificacion = "MANUAL_EDIT";

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-empresa-implementacion", "TProcesoAdopcionEmpresa", pae.IdProcesoAdopcionEmpresa, $"Empresa #{pae.IdEmpresaSubsidiaria}",
            command.ActorUserId, command.CorrelationId, $"Actualización de avance de implementación de empresa #{pae.IdEmpresaSubsidiaria}: Estado={pae.EstadoImplementacion}, Fecha={pae.FechaInicioAdopcion:yyyy-MM-dd}.", cancellationToken);

        return new AdoptionResult(true, "Datos de implementación de la subsidiaria actualizados exitosamente.", pae.IdProcesoAdopcionEmpresa);
    }

    public async Task<AdoptionResult> DecommissionImplementationProcessAsync(int procesoId, string motivoBaja, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var proc = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId && p.TipoProceso == "IMPLEMENTACION", cancellationToken);
        if (proc is null)
        {
            return new AdoptionResult(false, $"No se encontró la solicitud de implementación #{procesoId}.");
        }

        var cancelState = await dbContext.TechnologyAdoptionStates.FirstOrDefaultAsync(s => s.Nombre != null && s.Nombre.ToUpper().Contains("CANCEL"), cancellationToken);
        if (cancelState != null)
        {
            proc.IdEstadoAdopcionTSI = cancelState.Id;
        }

        var motivo = string.IsNullOrWhiteSpace(motivoBaja) ? "Sin motivo especificado" : motivoBaja.Trim();
        proc.Objetivo = $"[DADA DE BAJA: {motivo}] {proc.Objetivo}".Trim();
        if (!proc.NombreProceso.StartsWith("[DADA DE BAJA", StringComparison.OrdinalIgnoreCase))
        {
            proc.NombreProceso = $"[DADA DE BAJA: {motivo}] {proc.NombreProceso}".Trim();
        }
        proc.FechaEstimadaCierre = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-implementacion", "TProcesoAdopcionTSI", proc.IdProcesoAdopcionTSI, proc.NombreProceso,
            actorUserId, correlationId, $"Solicitud de implementación '{proc.CodigoProceso}' dada de baja. Motivo: {motivo}", cancellationToken);

        return new AdoptionResult(true, $"La solicitud de implementación '{proc.CodigoProceso}' ha sido dada de baja exitosamente.");
    }

    public async Task<AdoptionResult> DeleteImplementationProcessAsync(int procesoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        return await DeleteProcessCascadeAsync(procesoId, actorUserId, correlationId, cancellationToken);
    }

    public async Task<AdoptionResult> ManageImplementationCompaniesAsync(int procesoId, IEnumerable<int> selectedCompanyIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var proc = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId && p.TipoProceso == "IMPLEMENTACION", cancellationToken);
        if (proc is null)
        {
            return new AdoptionResult(false, $"No se encontró la solicitud de implementación #{procesoId}.");
        }

        var targetIds = selectedCompanyIds.Distinct().ToHashSet();
        var existing = await dbContext.AdoptionProcessCompanies
            .Where(cp => cp.IdProcesoAdopcionTSI == procesoId)
            .ToListAsync(cancellationToken);

        var existingIds = existing.Select(e => e.IdEmpresaSubsidiaria).ToHashSet();

        // 1. Eliminar empresas desmarcadas
        var toRemove = existing.Where(e => !targetIds.Contains(e.IdEmpresaSubsidiaria)).ToList();
        if (toRemove.Count > 0)
        {
            dbContext.AdoptionProcessCompanies.RemoveRange(toRemove);
        }

        // 2. Agregar empresas marcadas nuevas
        var toAdd = targetIds.Where(id => !existingIds.Contains(id)).ToList();
        foreach (var companyId in toAdd)
        {
            dbContext.AdoptionProcessCompanies.Add(new TProcesoAdopcionEmpresa
            {
                IdProcesoAdopcionTSI = procesoId,
                IdEmpresaSubsidiaria = companyId,
                Aplica = true,
                EstadoImplementacion = "PENDIENTE",
                FechaIncorporacion = DateTime.UtcNow.Date,
                FechaModificacion = DateTime.UtcNow,
                UsuarioModificacion = "SYSTEM"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-implementacion-empresas", "TProcesoAdopcionTSI", procesoId, proc.NombreProceso,
            actorUserId, correlationId, $"Convocatoria de empresas actualizada: {toAdd.Count} agregadas, {toRemove.Count} removidas.", cancellationToken);

        return new AdoptionResult(true, "Convocatoria de empresas actualizada exitosamente.");
    }

    public async Task<ImplementationReportsDto?> GetImplementationReportsAsync(int procesoId, CancellationToken cancellationToken = default)
    {
        var proc = await dbContext.AdoptionProcesses.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId && p.TipoProceso == "IMPLEMENTACION", cancellationToken);

        if (proc is null) return null;

        var famName = "Sin Familia";
        if (proc.IdFamilia.HasValue)
        {
            var f = await dbContext.Families.AsNoTracking().FirstOrDefaultAsync(x => x.Id == proc.IdFamilia.Value, cancellationToken);
            if (f?.Nombre is not null) famName = f.Nombre;
        }

        var techName = "Por definir";
        string? vendorName = null;
        if (proc.IdTecnologiaEstandar.HasValue)
        {
            var t = await dbContext.Technologies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == proc.IdTecnologiaEstandar.Value, cancellationToken);
            if (t?.NombreCorporativo is not null) techName = t.NombreCorporativo;

            var v = await dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.IdTecnologiaTSI == proc.IdTecnologiaEstandar.Value, cancellationToken);
            if (v?.NombreVendor is not null) vendorName = v.NombreVendor;
        }

        var companies = await dbContext.Companies.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var processCompanies = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .Where(cp => cp.IdProcesoAdopcionTSI == procesoId)
            .ToListAsync(cancellationToken);

        var totalConvocadas = processCompanies.Count;
        var totalNoAplica = processCompanies.Count(cp => cp.EstadoImplementacion == "NO_APLICA");
        var totalAlineadas = processCompanies.Count(cp => cp.EstadoImplementacion == "IMPLEMENTADO" || cp.EstadoImplementacion == "ALINEADO");
        var totalEnDespliegue = processCompanies.Count(cp => cp.EstadoImplementacion == "EN_DESPLIEGUE");
        var totalPendientes = processCompanies.Count(cp => cp.EstadoImplementacion == "PENDIENTE" || string.IsNullOrWhiteSpace(cp.EstadoImplementacion));
        var aplicables = totalConvocadas;
        var pctAlineamiento = totalConvocadas > 0 ? Math.Round((double)totalAlineadas / totalConvocadas * 100.0, 1) : 0.0;

        var orderedCompanyIds = ParseOrderedCompanyIds(proc.OrdenEmpresas);
        var companyOrderMap = new Dictionary<int, int>();
        for (var i = 0; i < orderedCompanyIds.Count; i++)
        {
            companyOrderMap[orderedCompanyIds[i]] = i;
        }

        var orderedCompanies = processCompanies
            .OrderBy(cp => companyOrderMap.GetValueOrDefault(cp.IdEmpresaSubsidiaria, int.MaxValue))
            .ThenBy(cp => companies.TryGetValue(cp.IdEmpresaSubsidiaria, out var c) ? c.Nombre : string.Empty)
            .ToList();

        var filasAlineamiento = new List<ImplementationCompanyAlignmentRowDto>();
        foreach (var cp in orderedCompanies)
        {
            companies.TryGetValue(cp.IdEmpresaSubsidiaria, out var comp);
            var asIs = !string.IsNullOrWhiteSpace(cp.TecnologiaAsIs)
                ? cp.TecnologiaAsIs
                : (cp.EstadoImplementacion == "IMPLEMENTADO" || cp.EstadoImplementacion == "ALINEADO"
                    ? (techName != "Por definir" ? techName : null)
                    : null);

            filasAlineamiento.Add(new ImplementationCompanyAlignmentRowDto(
                cp.IdProcesoAdopcionEmpresa,
                cp.IdEmpresaSubsidiaria,
                comp?.Nombre ?? $"Empresa #{cp.IdEmpresaSubsidiaria}",
                comp?.Pais,
                comp?.Rubro,
                cp.Aplica,
                cp.JustificacionNoAplica,
                string.IsNullOrWhiteSpace(cp.EstadoImplementacion) ? (cp.Aplica ? "PENDIENTE" : "NO_APLICA") : cp.EstadoImplementacion,
                cp.FechaInicioAdopcion,
                cp.EspecialistaTecnico,
                cp.UrlArquitectura,
                cp.NombreArchivoArquitectura,
                !string.IsNullOrWhiteSpace(cp.RutaArchivoArquitectura),
                asIs));
        }

        // Generar periodos de timeline
        var dates = processCompanies
            .Where(cp => cp.Aplica && cp.FechaInicioAdopcion.HasValue)
            .Select(cp => cp.FechaInicioAdopcion!.Value)
            .ToList();

        if (proc.FechaInicioVigenciaContrato.HasValue) dates.Add(proc.FechaInicioVigenciaContrato.Value);
        if (proc.FechaFinVigenciaContrato.HasValue) dates.Add(proc.FechaFinVigenciaContrato.Value);
        dates.Add(proc.FechaInicio);
        if (proc.FechaEstimadaCierre.HasValue) dates.Add(proc.FechaEstimadaCierre.Value);

        var minDate = dates.Min();
        var maxDate = dates.Max();

        // Asegurar al menos 6 meses de ventana visual
        if ((maxDate.Year - minDate.Year) * 12 + maxDate.Month - minDate.Month < 5)
        {
            maxDate = minDate.AddMonths(5);
        }

        var periodos = new List<ImplementationTimelinePeriodDto>();
        var curr = new DateTime(minDate.Year, minDate.Month, 1);
        var endMonth = new DateTime(maxDate.Year, maxDate.Month, 1);
        var orden = 1;

        var culture = new System.Globalization.CultureInfo("es-ES");
        while (curr <= endMonth && periodos.Count < 36)
        {
            var pKey = curr.ToString("yyyy-MM");
            var monthName = culture.DateTimeFormat.GetAbbreviatedMonthName(curr.Month);
            var label = $"{char.ToUpper(monthName[0])}{monthName[1..]} {curr.Year}";
            periodos.Add(new ImplementationTimelinePeriodDto(pKey, label, curr.Year, curr.Month, orden++));
            curr = curr.AddMonths(1);
        }

        var filasTimeline = new List<ImplementationCompanyTimelineRowDto>();
        var seq = 1;
        foreach (var cp in orderedCompanies)
        {
            companies.TryGetValue(cp.IdEmpresaSubsidiaria, out var comp);
            var pMatch = new Dictionary<string, bool>();
            string? assignedHito = null;

            if (cp.Aplica && cp.FechaInicioAdopcion.HasValue)
            {
                var compKey = cp.FechaInicioAdopcion.Value.ToString("yyyy-MM");
                foreach (var pDto in periodos)
                {
                    var isMatch = pDto.PeriodoKey == compKey;
                    pMatch[pDto.PeriodoKey] = isMatch;
                    if (isMatch) assignedHito = pDto.PeriodoLabel;
                }
            }
            else
            {
                foreach (var pDto in periodos)
                {
                    pMatch[pDto.PeriodoKey] = false;
                }
            }

            filasTimeline.Add(new ImplementationCompanyTimelineRowDto(
                seq++,
                cp.IdEmpresaSubsidiaria,
                comp?.Nombre ?? $"Empresa #{cp.IdEmpresaSubsidiaria}",
                comp?.Pais,
                comp?.Rubro,
                string.IsNullOrWhiteSpace(cp.EstadoImplementacion) ? (cp.Aplica ? "PENDIENTE" : "NO_APLICA") : cp.EstadoImplementacion,
                cp.FechaInicioAdopcion,
                cp.EspecialistaTecnico,
                assignedHito,
                pMatch));
        }

        // Calcular acumulación progresiva
        var acumulados = new Dictionary<string, int>();
        var pctAcumulados = new Dictionary<string, double>();
        var runCount = 0;
        foreach (var pDto in periodos)
        {
            var inThisMonth = filasTimeline.Count(r => r.HitoAbordajePorPeriodo.TryGetValue(pDto.PeriodoKey, out var act) && act);
            runCount += inThisMonth;
            acumulados[pDto.PeriodoKey] = runCount;
            pctAcumulados[pDto.PeriodoKey] = aplicables > 0 ? Math.Round((double)runCount / aplicables * 100.0, 1) : 0.0;
        }

        return new ImplementationReportsDto(
            proc.IdProcesoAdopcionTSI,
            proc.CodigoProceso,
            proc.NombreProceso,
            famName,
            techName,
            vendorName,
            proc.FechaInicioVigenciaContrato,
            proc.FechaFinVigenciaContrato,
            proc.NumeroContratoCorporativo,
            proc.LiderCorporativoTSI,
            totalConvocadas,
            aplicables,
            totalAlineadas,
            totalEnDespliegue,
            totalPendientes,
            totalNoAplica,
            pctAlineamiento,
            filasAlineamiento,
            periodos,
            filasTimeline,
            acumulados,
            pctAcumulados);
    }

    public async Task<IReadOnlyList<StandardTechnologyDto>> GetCorporateStandardsByFamilyAsync(int? familiaId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.StandardTechnologyHistories.AsNoTracking()
            .Where(s => s.EstadoVigencia == "ACTIVO_VIGENTE");

        var standards = await query.ToListAsync(cancellationToken);
        if (standards.Count == 0) return [];

        var techIds = standards.Select(s => s.IdTecnologiaTSI).Distinct().ToList();
        var techQuery = dbContext.Technologies.AsNoTracking().Where(t => techIds.Contains(t.Id));
        if (familiaId.HasValue && familiaId.Value > 0)
        {
            techQuery = techQuery.Where(t => t.IdFamilia == familiaId.Value);
        }

        var technologies = await techQuery.ToDictionaryAsync(t => t.Id, cancellationToken);
        var bbIds = standards.Select(s => s.IdBuildingBlock).Distinct().ToList();
        var buildingBlocks = await dbContext.BuildingBlocks.AsNoTracking().Where(b => bbIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken);

        var result = new List<StandardTechnologyDto>();
        foreach (var s in standards)
        {
            if (!technologies.TryGetValue(s.IdTecnologiaTSI, out var tech)) continue;
            buildingBlocks.TryGetValue(s.IdBuildingBlock, out var bb);

            result.Add(new StandardTechnologyDto(
                s.IdEstandarTecnologia,
                s.IdBuildingBlock,
                s.IdTecnologiaTSI,
                tech.NombreCorporativo ?? $"Tecnología #{tech.Id}",
                null,
                null,
                null,
                s.RolEstandar,
                s.EstadoVigencia,
                s.FechaInicioVigencia,
                s.FechaFinVigencia,
                s.MotivoCambio,
                s.SustentoArquitectura,
                Array.Empty<string>()));
        }

        return result;
    }
}
