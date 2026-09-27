using System.Data;
using System.Data.Common;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Adoption;

public sealed partial class AdoptionProcessService(
    CatalogDbContext dbContext,
    IAuditTrailService auditTrail) : IAdoptionProcessService
{
    public async Task<IReadOnlyList<AdoptionProcessSummaryDto>> ListProcessesAsync(CancellationToken cancellationToken = default)
    {
        var processes = await dbContext.AdoptionProcesses.AsNoTracking()
            .Where(p => p.TipoProceso != "IMPLEMENTACION")
            .ToListAsync(cancellationToken);
        var buildingBlocks = await dbContext.BuildingBlocks.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Nombre ?? string.Empty, cancellationToken);
        var states = await dbContext.TechnologyAdoptionStates.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Nombre ?? string.Empty, cancellationToken);
        var companyProcesses = await dbContext.AdoptionProcessCompanies.AsNoTracking().ToListAsync(cancellationToken);
        var implementedTechs = await dbContext.ImplementedTechnologies.AsNoTracking().ToListAsync(cancellationToken);

        var list = new List<AdoptionProcessSummaryDto>();
        foreach (var p in processes)
        {
            var empresasProceso = companyProcesses.Where(cp => cp.IdProcesoAdopcionTSI == p.IdProcesoAdopcionTSI).ToList();
            var totalConvocadas = empresasProceso.Count;
            var totalNoAplica = empresasProceso.Count(cp => !cp.Aplica);

            var empresaIdsConTecnologia = implementedTechs
                .Where(it => it.IdBuildingBlock == p.IdBuildingBlock || empresasProceso.Any(ep => ep.IdProcesoAdopcionEmpresa == it.IdProcesoAdopcionEmpresa))
                .Select(it => it.IdEmpresaSubsidiaria)
                .Distinct()
                .Count();

            list.Add(new AdoptionProcessSummaryDto(
                p.IdProcesoAdopcionTSI,
                p.CodigoProceso,
                p.NombreProceso,
                p.IdBuildingBlock,
                buildingBlocks.GetValueOrDefault(p.IdBuildingBlock, "Building Block"),
                p.IdEstadoAdopcionTSI,
                states.GetValueOrDefault(p.IdEstadoAdopcionTSI, "Desconocido"),
                p.LiderCorporativoTSI,
                p.FechaInicio,
                p.FechaEstimadaCierre,
                totalConvocadas,
                empresaIdsConTecnologia,
                totalNoAplica));
        }

        return list;
    }

    public async Task<AdoptionProcessDetailDto?> GetProcessDetailAsync(int procesoId, CancellationToken cancellationToken = default)
    {
        var p = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(x => x.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (p is null) return null;

        return await BuildProcessDetailAsync(p, cancellationToken);
    }

    public async Task<AdoptionProcessDetailDto?> GetProcessDetailByBuildingBlockAsync(int buildingBlockId, CancellationToken cancellationToken = default)
    {
        var p = await dbContext.AdoptionProcesses.AsNoTracking()
            .OrderByDescending(x => x.FechaInicio)
            .FirstOrDefaultAsync(x => x.IdBuildingBlock == buildingBlockId, cancellationToken);

        if (p is null) return null;

        return await BuildProcessDetailAsync(p, cancellationToken);
    }

    private async Task<AdoptionProcessDetailDto> BuildProcessDetailAsync(TProcesoAdopcionTSI p, CancellationToken cancellationToken)
    {
        var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == p.IdBuildingBlock, cancellationToken);
        var dominio = bb?.IdDominio is not null ? await dbContext.Domains.AsNoTracking().FirstOrDefaultAsync(d => d.Id == bb.IdDominio, cancellationToken) : null;
        var familiaId = bb?.IdFamilia ?? dominio?.IdFamilia;
        var familia = familiaId is not null ? await dbContext.Families.AsNoTracking().FirstOrDefaultAsync(f => f.Id == familiaId, cancellationToken) : null;
        var estado = await dbContext.TechnologyAdoptionStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == p.IdEstadoAdopcionTSI, cancellationToken);

        // Estándares históricos y vigentes
        var allStandards = await dbContext.StandardTechnologyHistories.AsNoTracking()
            .Where(x => x.IdBuildingBlock == p.IdBuildingBlock)
            .OrderByDescending(x => x.FechaInicioVigencia)
            .ToListAsync(cancellationToken);

        var techIds = allStandards.Select(s => s.IdTecnologiaTSI).Distinct().ToList();
        var technologies = await dbContext.Technologies.AsNoTracking().Where(t => techIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var standardDtos = new List<StandardTechnologyDto>();
        foreach (var s in allStandards)
        {
            var tech = technologies.GetValueOrDefault(s.IdTecnologiaTSI);
            var useCases = await dbContext.UseCases.AsNoTracking()
                .Where(uc => uc.IdTecnologia == s.IdTecnologiaTSI || uc.IdEstandarTecnologia == s.IdEstandarTecnologia)
                .Select(uc => uc.Nombre ?? string.Empty)
                .ToListAsync(cancellationToken);

            // Obtener vendor y contactos desde ADO
            var (vendorName, vendorContact, partnerContact) = await GetVendorDetailsAsync(s.IdTecnologiaTSI, cancellationToken);

            standardDtos.Add(new StandardTechnologyDto(
                s.IdEstandarTecnologia,
                s.IdBuildingBlock,
                s.IdTecnologiaTSI,
                tech?.NombreCorporativo ?? "Tecnología",
                vendorName,
                vendorContact,
                partnerContact,
                s.RolEstandar,
                s.EstadoVigencia,
                s.FechaInicioVigencia,
                s.FechaFinVigencia,
                s.MotivoCambio,
                s.SustentoArquitectura,
                useCases));
        }

        var principalVigente = standardDtos.FirstOrDefault(s => s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE");
        var alternativos = standardDtos.Where(s => s.RolEstandar == "ALTERNATIVA" && s.EstadoVigencia == "ACTIVO_VIGENTE").ToList();
        var historicos = standardDtos.Where(s => s.EstadoVigencia == "HISTORICO_REEMPLAZADO").ToList();

        // Empresas convocadas participantes
        var companyProcesses = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .Where(cp => cp.IdProcesoAdopcionTSI == p.IdProcesoAdopcionTSI && cp.Aplica)
            .ToListAsync(cancellationToken);

        var allCompanies = await dbContext.Companies.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var allCompanyContacts = await GetCompanyContactsAsync(cancellationToken);

        var implemented = await dbContext.ImplementedTechnologies.AsNoTracking()
            .Where(it => it.IdBuildingBlock == p.IdBuildingBlock || companyProcesses.Select(cp => cp.IdProcesoAdopcionEmpresa).Contains(it.IdProcesoAdopcionEmpresa ?? 0))
            .ToListAsync(cancellationToken);

        var implTechIds = implemented.Select(it => it.IdTecnologiaTSI).Distinct().ToList();
        var implTechnologies = await dbContext.Technologies.AsNoTracking().Where(t => implTechIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var contracts = await dbContext.TechnologyContracts.AsNoTracking().ToListAsync(cancellationToken);
        var drivers = await dbContext.Drivers.AsNoTracking().ToListAsync(cancellationToken);
        var operationModels = await GetOperationModelsAsync(cancellationToken);

        var companyProcessIds = companyProcesses.Select(cp => cp.IdProcesoAdopcionEmpresa).ToList();
        var companyCaps = await dbContext.ProcessCompanyCapabilities.AsNoTracking()
            .Where(c => companyProcessIds.Contains(c.IdProcesoAdopcionEmpresa))
            .ToListAsync(cancellationToken);

        var bbCapabilities = await dbContext.Capabilities.AsNoTracking()
            .Where(c => c.IdBuildingBlock == p.IdBuildingBlock)
            .OrderBy(c => c.OrdenVisualizacion ?? 999999)
            .ThenBy(c => c.Nombre)
            .ToListAsync(cancellationToken);

        // Cargar drivers del proceso (Plantilla) y valores por empresa (TDriverEmpresa)
        var processDrivers = await dbContext.ProcessDrivers.AsNoTracking()
            .Where(dp => dp.IdProcesoAdopcionTSI == p.IdProcesoAdopcionTSI)
            .OrderBy(dp => dp.OrdenVisual ?? 999999)
            .ThenBy(dp => dp.IdDriverProceso)
            .ToListAsync(cancellationToken);

        var companyDriverValues = await dbContext.CompanyDrivers.AsNoTracking()
            .Where(cd => companyProcessIds.Contains(cd.IdProcesoAdopcionEmpresa))
            .ToListAsync(cancellationToken);

        var companyRows = new List<CompanyAdoptionRowDto>();
        foreach (var cp in companyProcesses)
        {
            var comp = allCompanies.GetValueOrDefault(cp.IdEmpresaSubsidiaria);
            (string Nombre, string? Email)? focal = null;
            if (cp.IdContactoEmpresaSubsidiaria.HasValue && allCompanyContacts.TryGetValue(cp.IdContactoEmpresaSubsidiaria.Value, out var contact))
            {
                focal = contact;
            }

            // Aislamiento estricto: priorizar tecnologías registradas para este proceso de adopción (cp.IdProcesoAdopcionEmpresa)
            var compImpls = implemented.Where(it => it.IdProcesoAdopcionEmpresa == cp.IdProcesoAdopcionEmpresa).ToList();
            if (compImpls.Count == 0)
            {
                // Fallback para procesos previos a la vinculación: tomar únicamente la última tecnología de la empresa para este BB
                compImpls = implemented
                    .Where(it => it.IdEmpresaSubsidiaria == cp.IdEmpresaSubsidiaria && (it.IdProcesoAdopcionEmpresa == null || it.IdProcesoAdopcionEmpresa == 0))
                    .OrderByDescending(it => it.EsTecnologiaPrimaria ? 1 : 0)
                    .ThenByDescending(it => it.IdTecnologiaTSIimplementadaSubsidiaria)
                    .Take(1)
                    .ToList();
            }

            var compImplDtos = new List<ImplementedTechnologyDto>();

            foreach (var impl in compImpls)
            {
                var t = implTechnologies.GetValueOrDefault(impl.IdTecnologiaTSI);
                var (vName, _, _) = await GetVendorDetailsAsync(impl.IdTecnologiaTSI, cancellationToken);

                // Contratos y adendas
                var compContracts = contracts.Where(c => c.IdTecnologiaTSIimplementadaSubsidiaria == impl.IdTecnologiaTSIimplementadaSubsidiaria).ToList();
                var contractDict = compContracts.ToDictionary(c => c.IdContratoTecnologia, c => c.NumeroContrato);
                var rootContracts = compContracts.Where(c => !c.EsAdenda || c.IdContratoPadre is null).ToList();
                var contractDtos = rootContracts.Select(rc =>
                {
                    var adendas = compContracts.Where(ac => ac.EsAdenda && ac.IdContratoPadre == rc.IdContratoTecnologia)
                        .Select(ac => new ContractDto(ac.IdContratoTecnologia, ac.IdTecnologiaTSIimplementadaSubsidiaria, ac.NumeroContrato, ac.EsAdenda, ac.IdContratoPadre, rc.NumeroContrato, ac.FechaInicio, ac.FechaFin, ac.FechaAdjudicacion, ac.RutaDocumentoContrato, ac.MontoContratado, ac.Moneda, ac.Observaciones, [], ac.EsPayg, ac.MontoAnual, ac.MontoTrianual))
                        .ToList();

                    return new ContractDto(rc.IdContratoTecnologia, rc.IdTecnologiaTSIimplementadaSubsidiaria, rc.NumeroContrato, rc.EsAdenda, rc.IdContratoPadre, null, rc.FechaInicio, rc.FechaFin, rc.FechaAdjudicacion, rc.RutaDocumentoContrato, rc.MontoContratado, rc.Moneda, rc.Observaciones, adendas, rc.EsPayg, rc.MontoAnual, rc.MontoTrianual);
                }).ToList();

                // Drivers
                var compDrivers = drivers.Where(d => d.IdTecnologiaTSIimplementadaSubsidiaria == impl.IdTecnologiaTSIimplementadaSubsidiaria)
                    .Select(d => new DriverDto(d.IdDriver, d.IdTecnologiaTSIimplementadaSubsidiaria, d.DescripcionDriver ?? "Driver", d.UnidadMedida, d.Cantidad, d.PrecioUnitario, d.Moneda, (d.Cantidad ?? 0) * (d.PrecioUnitario ?? 0)))
                    .ToList();

                var opModel = operationModels.GetValueOrDefault(impl.IdTecnologiaTSIimplementadaSubsidiaria);

                // Alineamiento individual
                var align = ComputeSingleAlignment(cp.Aplica, impl.IdTecnologiaTSI, principalVigente, alternativos);

                compImplDtos.Add(new ImplementedTechnologyDto(
                    impl.IdTecnologiaTSIimplementadaSubsidiaria,
                    impl.IdEmpresaSubsidiaria,
                    comp?.Nombre ?? "Empresa",
                    impl.IdBuildingBlock ?? p.IdBuildingBlock,
                    impl.IdTecnologiaTSI,
                    t?.NombreCorporativo ?? "Tecnología",
                    vName,
                    impl.EsTecnologiaPrimaria,
                    impl.VersionDesplegada,
                    align,
                    opModel,
                    contractDtos,
                    compDrivers,
                    impl.EsInstanciaCorporativa));
            }

            var globalAlign = ComputeGlobalAlignment(cp.Aplica, compImplDtos, principalVigente, alternativos);

            var compCapsList = bbCapabilities.Select(bc =>
            {
                var matching = companyCaps.FirstOrDefault(cc => cc.IdProcesoAdopcionEmpresa == cp.IdProcesoAdopcionEmpresa && cc.IdCapacidad == bc.Id);
                return new CompanyCapabilityItemDto(
                    matching?.IdProcesoEmpresaCapacidad ?? 0,
                    bc.Id,
                    bc.Nombre ?? "Capacidad",
                    matching?.EstadoCobertura ?? "NA",
                    matching?.Comentario,
                    bc.OrdenVisualizacion ?? 0);
            }).ToList();

            var compDriversList = processDrivers.Select(dp =>
            {
                var matching = companyDriverValues.FirstOrDefault(cd => cd.IdProcesoAdopcionEmpresa == cp.IdProcesoAdopcionEmpresa && cd.IdDriverProceso == dp.IdDriverProceso);
                var cant = matching?.Cantidad ?? 0.00m;
                var pu = matching?.PrecioUnitario ?? 0.00m;
                var total = matching?.CostoTotal ?? (cant * pu);
                var moneda = string.IsNullOrWhiteSpace(matching?.Moneda) ? "USD" : matching.Moneda;

                return new CompanyDriverValueDto(
                    matching?.IdDriverEmpresa ?? 0,
                    cp.IdProcesoAdopcionEmpresa,
                    dp.IdDriverProceso,
                    dp.DescripcionDriver,
                    dp.UnidadMedida,
                    cant,
                    pu,
                    total,
                    moneda);
            }).ToList();

            companyRows.Add(new CompanyAdoptionRowDto(
                cp.IdProcesoAdopcionEmpresa,
                cp.IdEmpresaSubsidiaria,
                comp?.Nombre ?? "Empresa",
                cp.IdContactoEmpresaSubsidiaria,
                focal?.Nombre,
                focal?.Email,
                cp.Aplica,
                cp.JustificacionNoAplica,
                cp.FechaIncorporacion,
                compImplDtos,
                globalAlign,
                compCapsList,
                cp.ComentarioCapacidades,
                compDriversList));
        }

        var orderedCompanyIds = ParseOrderedCompanyIds(p.OrdenEmpresas);
        if (orderedCompanyIds.Count > 0)
        {
            var orderMap = orderedCompanyIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
            companyRows = companyRows
                .OrderBy(r => orderMap.TryGetValue(r.EmpresaId, out var idx) ? idx : 999999)
                .ThenBy(r => r.EmpresaNombre)
                .ToList();
        }

        var processDriverTemplates = processDrivers.Select(dp => new ProcessDriverTemplateDto(
            dp.IdDriverProceso,
            dp.IdProcesoAdopcionTSI,
            dp.DescripcionDriver,
            dp.UnidadMedida,
            dp.OrdenVisual ?? 1)).ToList();

        return new AdoptionProcessDetailDto(
            p.IdProcesoAdopcionTSI,
            p.CodigoProceso,
            p.NombreProceso,
            p.IdBuildingBlock,
            bb?.Nombre ?? "Building Block",
            dominio?.Dominio,
            familia?.Nombre,
            p.IdEstadoAdopcionTSI,
            estado?.Nombre ?? "Estado",
            p.Objetivo,
            p.Alcance,
            p.LiderCorporativoTSI,
            p.FechaInicio,
            p.FechaEstimadaCierre,
            principalVigente,
            alternativos,
            historicos,
            companyRows,
            p.OrdenEmpresas,
            processDriverTemplates);
    }

    private static string ComputeSingleAlignment(bool applies, int techId, StandardTechnologyDto? principal, IReadOnlyList<StandardTechnologyDto> alternativos)
    {
        if (!applies) return "NO_APLICA";
        if (principal is not null && techId == principal.TecnologiaId) return "ALINEADO";
        if (alternativos.Any(a => a.TecnologiaId == techId)) return "HOMOLOGADO";
        return "NO_ALINEADO";
    }

    private static string ComputeGlobalAlignment(bool applies, IReadOnlyList<ImplementedTechnologyDto> techs, StandardTechnologyDto? principal, IReadOnlyList<StandardTechnologyDto> alternativos)
    {
        if (!applies) return "NO_APLICA";
        if (techs.Count == 0) return "PENDIENTE";
        if (principal is not null && techs.Any(t => t.TecnologiaId == principal.TecnologiaId && t.EsPrimaria)) return "ALINEADO";
        if (principal is not null && techs.Any(t => t.TecnologiaId == principal.TecnologiaId)) return "ALINEADO";
        if (alternativos.Any(a => techs.Any(t => t.TecnologiaId == a.TecnologiaId))) return "HOMOLOGADO";
        return "NO_ALINEADO";
    }

    public async Task<AdoptionResult> CreateProcessAsync(CreateAdoptionProcessCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Codigo))
            return new AdoptionResult(false, "El código del proceso es obligatorio.");

        if (string.IsNullOrWhiteSpace(command.Nombre))
            return new AdoptionResult(false, "El nombre del proceso es obligatorio.");

        var exists = await dbContext.AdoptionProcesses.AnyAsync(x => x.CodigoProceso == command.Codigo, cancellationToken);
        if (exists)
            return new AdoptionResult(false, $"Ya existe un proceso de adopción con el código '{command.Codigo}'.");

        var bbExists = await dbContext.BuildingBlocks.AnyAsync(b => b.Id == command.BuildingBlockId, cancellationToken);
        if (!bbExists)
            return new AdoptionResult(false, "El Building Block especificado no existe.");

        var proceso = new TProcesoAdopcionTSI
        {
            CodigoProceso = command.Codigo.Trim(),
            NombreProceso = command.Nombre.Trim(),
            IdBuildingBlock = command.BuildingBlockId,
            IdEstadoAdopcionTSI = command.EstadoAdopcionId,
            Objetivo = command.Objetivo,
            Alcance = command.Alcance,
            LiderCorporativoTSI = command.LiderCorporativo,
            FechaInicio = command.FechaInicio,
            FechaEstimadaCierre = command.FechaEstimadaCierre,
            FechaCreacion = DateTime.UtcNow,
            UsuarioCreacion = "SYSTEM"
        };

        dbContext.AdoptionProcesses.Add(proceso);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordCreateAsync("proceso-adopcion-tsi", "TProcesoAdopcionTSI", proceso.IdProcesoAdopcionTSI,
            proceso.NombreProceso, command.ActorUserId, command.CorrelationId, null, 1, cancellationToken);

        return new AdoptionResult(true, "Proceso de adopción creado exitosamente.", proceso.IdProcesoAdopcionTSI);
    }

    public async Task<AdoptionResult> UpdateProcessStatusAsync(int procesoId, int nuevoEstadoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null)
            return new AdoptionResult(false, "El proceso de adopción no fue encontrado.");

        var state = await dbContext.TechnologyAdoptionStates.FirstOrDefaultAsync(s => s.Id == nuevoEstadoId, cancellationToken);
        if (state is null)
            return new AdoptionResult(false, "El estado de adopción especificado no existe.");

        proceso.IdEstadoAdopcionTSI = nuevoEstadoId;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-adopcion-tsi", "TProcesoAdopcionTSI", proceso.IdProcesoAdopcionTSI,
            proceso.NombreProceso, actorUserId, correlationId, $"Estado de adopción actualizado a '{state.Nombre}'", cancellationToken);

        return new AdoptionResult(true, $"Estado de adopción actualizado exitosamente a '{state.Nombre}'.");
    }

    public async Task<AdoptionResult> ConveneCompanyAsync(ConveneCompanyCommand command, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == command.ProcesoId, cancellationToken);
        if (proceso is null)
            return new AdoptionResult(false, "El proceso de adopción no existe.");

        var empresa = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == command.EmpresaId, cancellationToken);
        if (empresa is null)
            return new AdoptionResult(false, "La empresa subsidiaria no existe.");

        if (!command.Aplica && string.IsNullOrWhiteSpace(command.JustificacionNoAplica))
            return new AdoptionResult(false, "Si el Building Block no aplica, debe ingresar una justificación.");

        var existing = await dbContext.AdoptionProcessCompanies
            .FirstOrDefaultAsync(cp => cp.IdProcesoAdopcionTSI == command.ProcesoId && cp.IdEmpresaSubsidiaria == command.EmpresaId, cancellationToken);

        if (existing is null)
        {
            existing = new TProcesoAdopcionEmpresa
            {
                IdProcesoAdopcionTSI = command.ProcesoId,
                IdEmpresaSubsidiaria = command.EmpresaId,
                IdContactoEmpresaSubsidiaria = command.ContactoFocalId,
                Aplica = command.Aplica,
                JustificacionNoAplica = command.JustificacionNoAplica,
                FechaIncorporacion = DateTime.UtcNow.Date,
                FechaModificacion = DateTime.UtcNow,
                UsuarioModificacion = "SYSTEM"
            };
            dbContext.AdoptionProcessCompanies.Add(existing);
        }
        else
        {
            existing.IdContactoEmpresaSubsidiaria = command.ContactoFocalId;
            existing.Aplica = command.Aplica;
            existing.JustificacionNoAplica = command.JustificacionNoAplica;
            existing.FechaModificacion = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureCompanyCapabilitiesInitializedAsync(proceso.IdBuildingBlock, existing.IdProcesoAdopcionEmpresa, cancellationToken);

        await auditTrail.RecordRelationAsync("CONVENE_COMPANY", "proceso-adopcion-empresa", "TProcesoAdopcionEmpresa",
            command.ProcesoId, empresa.Nombre, command.ActorUserId, command.CorrelationId, null, cancellationToken);

        return new AdoptionResult(true, "Empresa subsidiaria convocada al proceso exitosamente.", existing.IdProcesoAdopcionEmpresa);
    }

    public async Task<AdoptionResult> SetCorporateStandardAsync(SetCorporateStandardCommand command, CancellationToken cancellationToken = default)
    {
        var bbExists = await dbContext.BuildingBlocks.AnyAsync(b => b.Id == command.BuildingBlockId, cancellationToken);
        if (!bbExists)
            return new AdoptionResult(false, "El Building Block especificado no existe.");

        var techExists = await dbContext.Technologies.AnyAsync(t => t.Id == command.TecnologiaId, cancellationToken);
        if (!techExists)
            return new AdoptionResult(false, "La tecnología especificada no existe.");

        var rol = command.RolEstandar.Trim().ToUpperInvariant();
        if (rol != "PRINCIPAL" && rol != "ALTERNATIVA")
            rol = "PRINCIPAL";

        // Si se define un nuevo estándar principal, archivar el principal vigente anterior
        if (rol == "PRINCIPAL")
        {
            var activePrincipals = await dbContext.StandardTechnologyHistories
                .Where(x => x.IdBuildingBlock == command.BuildingBlockId && x.RolEstandar == "PRINCIPAL" && x.EstadoVigencia == "ACTIVO_VIGENTE")
                .ToListAsync(cancellationToken);

            foreach (var active in activePrincipals)
            {
                active.EstadoVigencia = "HISTORICO_REEMPLAZADO";
                active.FechaFinVigencia = command.FechaInicio.AddDays(-1);
                active.MotivoCambio = command.MotivoCambio ?? "Sustituido por nuevo estándar corporativo principal";
            }
        }

        var newStandard = new TEstandarTecnologiaHistorico
        {
            IdBuildingBlock = command.BuildingBlockId,
            IdTecnologiaTSI = command.TecnologiaId,
            IdProcesoAdopcionTSI = command.ProcesoAdopcionId,
            RolEstandar = rol,
            EstadoVigencia = "ACTIVO_VIGENTE",
            FechaInicioVigencia = command.FechaInicio,
            FechaFinVigencia = null,
            MotivoCambio = null,
            SustentoArquitectura = command.SustentoArquitectura,
            FechaRegistro = DateTime.UtcNow,
            UsuarioRegistro = "SYSTEM"
        };

        dbContext.StandardTechnologyHistories.Add(newStandard);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Sincronizar en tabla puente TBuildingBlockVsTTecnologiaTSI (Task 3.5)
        await SyncBridgeTableAsync(command.BuildingBlockId, command.TecnologiaId, cancellationToken);

        await auditTrail.RecordRelationAsync("SET_CORPORATE_STANDARD", "estandar-tecnologia-historico", "TEstandarTecnologiaHistorico",
            command.BuildingBlockId, $"Tech {command.TecnologiaId}", command.ActorUserId, command.CorrelationId, $"Rol: {rol}", cancellationToken);

        return new AdoptionResult(true, "Estándar corporativo registrado exitosamente.", newStandard.IdEstandarTecnologia);
    }

    private async Task SyncBridgeTableAsync(int buildingBlockId, int tecnologiaId, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock = @bbId AND idTecnologiaTSI = @techId;";
        var p1 = checkCmd.CreateParameter(); p1.ParameterName = "@bbId"; p1.Value = buildingBlockId; checkCmd.Parameters.Add(p1);
        var p2 = checkCmd.CreateParameter(); p2.ParameterName = "@techId"; p2.Value = tecnologiaId; checkCmd.Parameters.Add(p2);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(cancellationToken)) > 0;

        if (!exists)
        {
            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO dbo.TBuildingBlockVsTTecnologiaTSI (idBuildingBlock, idTecnologiaTSI) VALUES (@bbId, @techId);";
            var ip1 = insertCmd.CreateParameter(); ip1.ParameterName = "@bbId"; ip1.Value = buildingBlockId; insertCmd.Parameters.Add(ip1);
            var ip2 = insertCmd.CreateParameter(); ip2.ParameterName = "@techId"; ip2.Value = tecnologiaId; insertCmd.Parameters.Add(ip2);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<AdoptionResult> RegisterImplementedTechnologyAsync(RegisterImplementedTechnologyCommand command, CancellationToken cancellationToken = default)
    {
        var tech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdEmpresaSubsidiaria = command.EmpresaId,
            IdTecnologiaTSI = command.TecnologiaId,
            IdBuildingBlock = command.BuildingBlockId,
            IdProcesoAdopcionEmpresa = command.ProcesoEmpresaId,
            EsTecnologiaPrimaria = command.EsPrimaria,
            EsInstanciaCorporativa = command.EsInstanciaCorporativa,
            VersionDesplegada = command.VersionDesplegada
        };

        dbContext.ImplementedTechnologies.Add(tech);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordCreateAsync("tecnologia-implementada", "TTecnologiaTSIimplementadaSubsidiaria",
            tech.IdTecnologiaTSIimplementadaSubsidiaria, $"Empresa {command.EmpresaId} - Tech {command.TecnologiaId}",
            command.ActorUserId, command.CorrelationId, null, affectedRecordCount: 1, cancellationToken);

        return new AdoptionResult(true, "Tecnología implementada registrada exitosamente.", tech.IdTecnologiaTSIimplementadaSubsidiaria);
    }

    public async Task<AdoptionResult> DeleteImplementedTechnologyAsync(int procesoId, int tecnologiaImplementadaId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var tech = await dbContext.ImplementedTechnologies
            .FirstOrDefaultAsync(t => t.IdTecnologiaTSIimplementadaSubsidiaria == tecnologiaImplementadaId, cancellationToken);

        if (tech is null)
            return new AdoptionResult(false, "La tecnología implementada no existe.");

        var techEntity = await dbContext.Technologies.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tech.IdTecnologiaTSI, cancellationToken);
        var techName = techEntity?.NombreCorporativo ?? $"Tecnología {tech.IdTecnologiaTSI}";

        // 1. Contratos y adendas vinculados
        var contratos = await dbContext.TechnologyContracts
            .Where(c => c.IdTecnologiaTSIimplementadaSubsidiaria == tecnologiaImplementadaId)
            .ToListAsync(cancellationToken);
        if (contratos.Count > 0)
        {
            var childAdendas = contratos.Where(c => c.IdContratoPadre != null).ToList();
            var parents = contratos.Where(c => c.IdContratoPadre == null).ToList();
            if (childAdendas.Count > 0)
                dbContext.TechnologyContracts.RemoveRange(childAdendas);
            if (parents.Count > 0)
                dbContext.TechnologyContracts.RemoveRange(parents);
        }

        // 2. Drivers de costo vinculados
        var drivers = await dbContext.Drivers
            .Where(d => d.IdTecnologiaTSIimplementadaSubsidiaria == tecnologiaImplementadaId)
            .ToListAsync(cancellationToken);
        if (drivers.Count > 0)
        {
            dbContext.Drivers.RemoveRange(drivers);
        }

        // 3. Servicios de TI vinculados
        var services = await dbContext.TechnologyServices
            .Include(s => s.TarifariosProyecto)
            .Include(s => s.TarifariosOperacion)
            .Where(s => s.IdTecnologiaTSIimplementadaSubsidiaria == tecnologiaImplementadaId)
            .ToListAsync(cancellationToken);
        if (services.Count > 0)
        {
            dbContext.TechnologyServices.RemoveRange(services);
        }

        // 4. Modelo de Operación (si la base de datos es relacional)
        if (dbContext.Database.IsRelational())
        {
            try
            {
                var connection = dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken);
                }
                await using var delOpCmd = connection.CreateCommand();
                delOpCmd.CommandText = "DELETE FROM dbo.TModeloDeOperacion WHERE idTecnologiaTSIimplementadaSubsidiaria = @implId;";
                var p = delOpCmd.CreateParameter();
                p.ParameterName = "@implId";
                p.Value = tecnologiaImplementadaId;
                delOpCmd.Parameters.Add(p);
                await delOpCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch
            {
                // Ignorar si la tabla no existe en el esquema o proveedor actual
            }
        }

        // 5. Eliminar la tecnología implementada
        dbContext.ImplementedTechnologies.Remove(tech);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 6. Auditoría
        await auditTrail.RecordRelationAsync("DELETE_IMPLEMENTED_TECH", "tecnologia-tsi-implementada", "TTecnologiaTSIimplementadaSubsidiaria",
            tecnologiaImplementadaId, techName, actorUserId, correlationId, $"Tecnología '{techName}' eliminada de la subsidiaria {tech.IdEmpresaSubsidiaria}", cancellationToken);

        return new AdoptionResult(true, $"La tecnología '{techName}' fue eliminada exitosamente.");
    }

    public async Task<AdoptionResult> SaveContractAsync(SaveContractCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NumeroContrato))
            return new AdoptionResult(false, "El número de contrato es obligatorio.");

        if (command.EsAdenda)
        {
            if (!command.ContratoPadreId.HasValue)
                return new AdoptionResult(false, "Una adenda debe especificar un contrato principal padre.");

            var parentExists = await dbContext.TechnologyContracts.AnyAsync(
                c => c.IdContratoTecnologia == command.ContratoPadreId.Value &&
                     c.IdTecnologiaTSIimplementadaSubsidiaria == command.TecnologiaImplementadaId,
                cancellationToken);

            if (!parentExists)
                return new AdoptionResult(false, "El contrato principal especificado no existe o pertenece a otra tecnología implementada.");
        }

        if (!command.EsPayg)
        {
            if (!command.FechaInicio.HasValue || !command.FechaFin.HasValue)
                return new AdoptionResult(false, "Las fechas de inicio y fin son obligatorias cuando no es modalidad PAYG.");

            if (command.FechaFin < command.FechaInicio)
                return new AdoptionResult(false, "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        if (command.ContratoId.HasValue && command.ContratoId.Value > 0)
        {
            var contratoExistente = await dbContext.TechnologyContracts.FirstOrDefaultAsync(c => c.IdContratoTecnologia == command.ContratoId.Value, cancellationToken);
            if (contratoExistente is null)
                return new AdoptionResult(false, "El contrato a editar no existe.");

            contratoExistente.NumeroContrato = command.NumeroContrato.Trim();
            contratoExistente.EsAdenda = command.EsAdenda;
            contratoExistente.EsPayg = command.EsPayg;
            contratoExistente.IdContratoPadre = command.ContratoPadreId;
            contratoExistente.FechaInicio = command.EsPayg ? null : command.FechaInicio;
            contratoExistente.FechaFin = command.EsPayg ? null : command.FechaFin;
            contratoExistente.FechaAdjudicacion = command.EsPayg ? null : command.FechaAdjudicacion;
            contratoExistente.RutaDocumentoContrato = command.RutaDocumento;
            contratoExistente.MontoContratado = command.Monto;
            contratoExistente.MontoAnual = command.MontoAnual;
            contratoExistente.MontoTrianual = command.MontoTrianual;
            contratoExistente.Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant();
            contratoExistente.Observaciones = command.Observaciones;

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditTrail.RecordUpdateAsync("contrato-tecnologia", "TContratoTecnologia", contratoExistente.IdContratoTecnologia,
                contratoExistente.NumeroContrato, command.ActorUserId, command.CorrelationId, null, cancellationToken);

            return new AdoptionResult(true, "Contrato actualizado exitosamente.", contratoExistente.IdContratoTecnologia);
        }

        var contrato = new TContratoTecnologia
        {
            IdTecnologiaTSIimplementadaSubsidiaria = command.TecnologiaImplementadaId,
            NumeroContrato = command.NumeroContrato.Trim(),
            EsAdenda = command.EsAdenda,
            EsPayg = command.EsPayg,
            IdContratoPadre = command.ContratoPadreId,
            FechaInicio = command.EsPayg ? null : command.FechaInicio,
            FechaFin = command.EsPayg ? null : command.FechaFin,
            FechaAdjudicacion = command.EsPayg ? null : command.FechaAdjudicacion,
            RutaDocumentoContrato = command.RutaDocumento,
            MontoContratado = command.Monto,
            MontoAnual = command.MontoAnual,
            MontoTrianual = command.MontoTrianual,
            Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant(),
            Observaciones = command.Observaciones,
            FechaRegistro = DateTime.UtcNow,
            UsuarioRegistro = "SYSTEM"
        };

        dbContext.TechnologyContracts.Add(contrato);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordCreateAsync("contrato-tecnologia", "TContratoTecnologia", contrato.IdContratoTecnologia,
            contrato.NumeroContrato, command.ActorUserId, command.CorrelationId, null, affectedRecordCount: 1, cancellationToken);

        return new AdoptionResult(true, "Contrato guardado exitosamente.", contrato.IdContratoTecnologia);
    }

    public async Task<AdoptionResult> DeleteContractAsync(int contratoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var contrato = await dbContext.TechnologyContracts.FirstOrDefaultAsync(c => c.IdContratoTecnologia == contratoId, cancellationToken);
        if (contrato is null)
            return new AdoptionResult(false, "El contrato no existe.");

        // Eliminar adendas hijas si existen
        var childAdendas = await dbContext.TechnologyContracts.Where(c => c.IdContratoPadre == contratoId).ToListAsync(cancellationToken);
        if (childAdendas.Count > 0)
        {
            dbContext.TechnologyContracts.RemoveRange(childAdendas);
        }

        dbContext.TechnologyContracts.Remove(contrato);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordRelationAsync("DELETE_CONTRACT", "contrato-tecnologia", "TContratoTecnologia",
            contratoId, null, actorUserId, correlationId, null, cancellationToken);

        return new AdoptionResult(true, "Contrato eliminado exitosamente.");
    }

    public async Task<AdoptionResult> SaveDriverAsync(SaveDriverCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Descripcion))
            return new AdoptionResult(false, "La descripción del driver es obligatoria.");

        if (command.DriverId.HasValue && command.DriverId.Value > 0)
        {
            var driverExistente = await dbContext.Drivers.FirstOrDefaultAsync(d => d.IdDriver == command.DriverId.Value, cancellationToken);
            if (driverExistente is null)
                return new AdoptionResult(false, "El driver a editar no existe.");

            driverExistente.DescripcionDriver = command.Descripcion.Trim();
            driverExistente.UnidadMedida = command.UnidadMedida?.Trim();
            driverExistente.Cantidad = command.Cantidad;
            driverExistente.PrecioUnitario = command.PrecioUnitario;
            driverExistente.Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant();

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditTrail.RecordUpdateAsync("driver", "TDriver", driverExistente.IdDriver,
                driverExistente.DescripcionDriver, command.ActorUserId, command.CorrelationId, null, cancellationToken);

            return new AdoptionResult(true, "Driver actualizado exitosamente.", driverExistente.IdDriver);
        }

        var driver = new TDriver
        {
            IdTecnologiaTSIimplementadaSubsidiaria = command.TecnologiaImplementadaId,
            DescripcionDriver = command.Descripcion.Trim(),
            UnidadMedida = command.UnidadMedida?.Trim(),
            Cantidad = command.Cantidad,
            PrecioUnitario = command.PrecioUnitario,
            Moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim().ToUpperInvariant()
        };

        dbContext.Drivers.Add(driver);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordCreateAsync("driver", "TDriver", driver.IdDriver,
            driver.DescripcionDriver, command.ActorUserId, command.CorrelationId, null, affectedRecordCount: 1, cancellationToken);

        return new AdoptionResult(true, "Driver guardado exitosamente.", driver.IdDriver);
    }

    public async Task<AdoptionResult> DeleteDriverAsync(int driverId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var driver = await dbContext.Drivers.FirstOrDefaultAsync(d => d.IdDriver == driverId, cancellationToken);
        if (driver is null)
            return new AdoptionResult(false, "El driver no existe.");

        dbContext.Drivers.Remove(driver);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordRelationAsync("DELETE_DRIVER", "driver", "TDriver",
            driverId, null, actorUserId, correlationId, null, cancellationToken);

        return new AdoptionResult(true, "Driver eliminado exitosamente.");
    }

    public async Task<AdoptionResult> SaveOperationModelAsync(SaveOperationModelCommand command, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            return new AdoptionResult(true, "Modelo de operación guardado exitosamente.");
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT idModeloOperacion FROM dbo.TModeloDeOperacion WHERE idTecnologiaTSIimplementadaSubsidiaria = @implId;";
        var p = checkCmd.CreateParameter(); p.ParameterName = "@implId"; p.Value = command.TecnologiaImplementadaId; checkCmd.Parameters.Add(p);
        var existingId = await checkCmd.ExecuteScalarAsync(cancellationToken);

        if (existingId is not null && existingId != DBNull.Value)
        {
            await using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"UPDATE dbo.TModeloDeOperacion
                SET idTipoModeloDeOperacion = @tipoId, idModalidadLaboral = @modalidadId
                WHERE idModeloOperacion = @id;";
            var p1 = updateCmd.CreateParameter(); p1.ParameterName = "@tipoId"; p1.Value = (object?)command.TipoOperacionId ?? DBNull.Value; updateCmd.Parameters.Add(p1);
            var p2 = updateCmd.CreateParameter(); p2.ParameterName = "@modalidadId"; p2.Value = (object?)command.ModalidadLaboralId ?? DBNull.Value; updateCmd.Parameters.Add(p2);
            var p3 = updateCmd.CreateParameter(); p3.ParameterName = "@id"; p3.Value = existingId; updateCmd.Parameters.Add(p3);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"INSERT INTO dbo.TModeloDeOperacion (idTecnologiaTSIimplementadaSubsidiaria, idTipoModeloDeOperacion, idModalidadLaboral)
                VALUES (@implId, @tipoId, @modalidadId);";
            var ip1 = insertCmd.CreateParameter(); ip1.ParameterName = "@implId"; ip1.Value = command.TecnologiaImplementadaId; insertCmd.Parameters.Add(ip1);
            var ip2 = insertCmd.CreateParameter(); ip2.ParameterName = "@tipoId"; ip2.Value = (object?)command.TipoOperacionId ?? DBNull.Value; insertCmd.Parameters.Add(ip2);
            var ip3 = insertCmd.CreateParameter(); ip3.ParameterName = "@modalidadId"; ip3.Value = (object?)command.ModalidadLaboralId ?? DBNull.Value; insertCmd.Parameters.Add(ip3);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await auditTrail.RecordRelationAsync("SAVE_OPERATION_MODEL", "modelo-operacion", "TModeloDeOperacion",
            command.TecnologiaImplementadaId, $"TipoOperacion: {command.TipoOperacionId}", command.ActorUserId, command.CorrelationId, null, cancellationToken);

        return new AdoptionResult(true, "Modelo de operación guardado exitosamente.");
    }

    private async Task<(string? VendorName, string? VendorContact, string? PartnerContact)> GetVendorDetailsAsync(int tecnologiaId, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            var vendor = await dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.IdTecnologiaTSI == tecnologiaId, cancellationToken);
            if (vendor is null) return (null, null, null);
            var vc = await dbContext.VendorContacts.AsNoTracking().FirstOrDefaultAsync(c => c.IdVendor == vendor.Id, cancellationToken);
            var pc = await dbContext.PartnerContacts.AsNoTracking().FirstOrDefaultAsync(c => c.IdVendor == vendor.Id, cancellationToken);
            return (vendor.NombreVendor, vc?.NombreContactoVendor, pc?.NombreContactoPartner);
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT TOP 1 v.nombreVendor,
                   (SELECT TOP 1 cv.nombreContactoVendor FROM dbo.TContactoVendor cv WHERE cv.idVendor = v.idVendor) AS ContactoVendor,
                   (SELECT TOP 1 cp.nombreContactoPartner FROM dbo.TContactoPartner cp WHERE cp.idVendor = v.idVendor) AS ContactoPartner
            FROM dbo.TVendor v
            WHERE v.idTecnologiaTSI = @techId;";
        var p = cmd.CreateParameter(); p.ParameterName = "@techId"; p.Value = tecnologiaId; cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return (
                reader.IsDBNull(0) ? null : reader.GetString(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2)
            );
        }

        return (null, null, null);
    }

    private async Task<Dictionary<int, (string Nombre, string? Email)>> GetCompanyContactsAsync(CancellationToken cancellationToken)
    {
        var dict = new Dictionary<int, (string, string?)>();
        if (!dbContext.Database.IsRelational())
        {
            return dict;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT idContactoEmpresaSubsidiaria, nombreContactoEmpresaSubsidiaria, email FROM dbo.TContactoEmpresaSubsidiaria;";
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt32(0);
                var name = reader.IsDBNull(1) ? "Contacto" : reader.GetString(1);
                var email = reader.IsDBNull(2) ? null : reader.GetString(2);
                dict[id] = (name, email);
            }
        }
        catch
        {
            // fallback si la columna tiene otro nombre de display
        }

        return dict;
    }

    private async Task<Dictionary<int, OperationModelDto>> GetOperationModelsAsync(CancellationToken cancellationToken)
    {
        var dict = new Dictionary<int, OperationModelDto>();
        if (!dbContext.Database.IsRelational())
        {
            return dict;
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT m.idModeloOperacion, m.idTecnologiaTSIimplementadaSubsidiaria,
                   m.idTipoModeloDeOperacion, t.TipoModeloDeOperacion,
                   m.idModalidadLaboral, l.TipoModalidadLaboral
            FROM dbo.TModeloDeOperacion m
            LEFT JOIN dbo.TTipoOperacion t ON t.idTipoModeloOperacion = m.idTipoModeloDeOperacion
            LEFT JOIN dbo.TModalidadLaboral l ON l.idModalidadLaboral = m.idModalidadLaboral;";

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetInt32(0);
                var implId = reader.GetInt32(1);
                int? tipoId = reader.IsDBNull(2) ? null : reader.GetInt32(2);
                string? tipoNom = reader.IsDBNull(3) ? null : reader.GetString(3);
                int? modId = reader.IsDBNull(4) ? null : reader.GetInt32(4);
                string? modNom = reader.IsDBNull(5) ? null : reader.GetString(5);

                dict[implId] = new OperationModelDto(id, implId, tipoId, tipoNom, modId, modNom);
            }
        }
        catch
        {
            // En caso de tablas vacías o sin registros
        }

        return dict;
    }

    public async Task<BuildingBlockCapabilitiesDto?> GetBuildingBlockCapabilitiesAsync(int buildingBlockId, CancellationToken cancellationToken = default)
    {
        var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == buildingBlockId, cancellationToken);
        if (bb is null) return null;

        var dominio = await dbContext.Domains.AsNoTracking().FirstOrDefaultAsync(x => x.Id == bb.IdDominio, cancellationToken);
        var familiaId = bb.IdFamilia ?? dominio?.IdFamilia;
        var familia = familiaId is not null ? await dbContext.Families.AsNoTracking().FirstOrDefaultAsync(f => f.Id == familiaId, cancellationToken) : null;
        var capacidades = await dbContext.Capabilities.AsNoTracking()
            .Where(c => c.IdBuildingBlock == buildingBlockId)
            .ToListAsync(cancellationToken);

        var capacidadIds = capacidades.Select(c => c.Id).ToList();
        var funcionalidades = await dbContext.Functionalities.AsNoTracking()
            .Where(f => f.IdCapacidad.HasValue && capacidadIds.Contains(f.IdCapacidad.Value))
            .ToListAsync(cancellationToken);

        var estadosCapacidad = await dbContext.CapabilityStates.AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => s.Nombre, cancellationToken);

        var capDtos = capacidades.Select(c => new CapabilitySummaryDto(
            c.Id,
            c.Nombre ?? $"Capacidad #{c.Id}",
            c.IdEstado.HasValue ? estadosCapacidad.GetValueOrDefault(c.IdEstado.Value) : null,
            funcionalidades.Where(f => f.IdCapacidad == c.Id).Select(f => f.Nombre ?? $"Funcionalidad #{f.Id}").ToList()
        )).ToList();

        return new BuildingBlockCapabilitiesDto(
            bb.Id,
            bb.Nombre ?? $"Building Block #{bb.Id}",
            dominio?.Dominio ?? "Dominio",
            capDtos,
            familia?.Nombre);
    }

    public async Task<AdoptionResult> BatchConveneCompaniesAsync(int procesoId, IEnumerable<ConveneCompanyInput> companies, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null) return new AdoptionResult(false, "El proceso de adopción no existe.");

        var existing = await dbContext.AdoptionProcessCompanies
            .Where(c => c.IdProcesoAdopcionTSI == procesoId)
            .ToListAsync(cancellationToken);

        var selectedCompanyIds = companies.Select(c => c.EmpresaId).ToHashSet();

        // 1. Eliminar empresas que fueron desmarcadas y ya no deben participar en este proceso
        var toRemove = existing.Where(e => !selectedCompanyIds.Contains(e.IdEmpresaSubsidiaria)).ToList();
        int countRemoved = toRemove.Count;
        if (toRemove.Count > 0)
        {
            var removeProcessEmpresaIds = toRemove.Select(r => r.IdProcesoAdopcionEmpresa).ToHashSet();
            var linkedImplTechs = await dbContext.ImplementedTechnologies
                .Where(it => it.IdProcesoAdopcionEmpresa.HasValue && removeProcessEmpresaIds.Contains(it.IdProcesoAdopcionEmpresa.Value))
                .ToListAsync(cancellationToken);

            foreach (var impl in linkedImplTechs)
            {
                impl.IdProcesoAdopcionEmpresa = null;
            }

            dbContext.AdoptionProcessCompanies.RemoveRange(toRemove);
        }

        // 2. Agregar o actualizar las empresas seleccionadas
        int countUpdated = 0, countAdded = 0;
        foreach (var item in companies)
        {
            var match = existing.FirstOrDefault(e => e.IdEmpresaSubsidiaria == item.EmpresaId);
            if (match is not null)
            {
                match.Aplica = item.Aplica;
                match.JustificacionNoAplica = item.Aplica ? null : item.JustificacionNoAplica;
                if (item.ContactoFocalId.HasValue) match.IdContactoEmpresaSubsidiaria = item.ContactoFocalId;
                match.FechaModificacion = DateTime.UtcNow;
                match.UsuarioModificacion = actorUserId.ToString();
                countUpdated++;
            }
            else
            {
                dbContext.AdoptionProcessCompanies.Add(new TProcesoAdopcionEmpresa
                {
                    IdProcesoAdopcionTSI = procesoId,
                    IdEmpresaSubsidiaria = item.EmpresaId,
                    IdContactoEmpresaSubsidiaria = item.ContactoFocalId,
                    Aplica = item.Aplica,
                    JustificacionNoAplica = item.Aplica ? null : item.JustificacionNoAplica,
                    FechaIncorporacion = DateTime.UtcNow,
                    FechaModificacion = DateTime.UtcNow,
                    UsuarioModificacion = actorUserId.ToString()
                });
                countAdded++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var currentProcessCompanies = await dbContext.AdoptionProcessCompanies
            .Where(c => c.IdProcesoAdopcionTSI == procesoId)
            .ToListAsync(cancellationToken);

        var processDriverTemplates = await dbContext.ProcessDrivers
            .Where(dp => dp.IdProcesoAdopcionTSI == procesoId)
            .ToListAsync(cancellationToken);

        foreach (var apc in currentProcessCompanies)
        {
            await EnsureCompanyCapabilitiesInitializedAsync(proceso.IdBuildingBlock, apc.IdProcesoAdopcionEmpresa, cancellationToken);

            // Clonar la plantilla de drivers a la nueva empresa convocada si no los tiene
            foreach (var dp in processDriverTemplates)
            {
                var hasDrv = await dbContext.CompanyDrivers.AnyAsync(cd => 
                    cd.IdProcesoAdopcionEmpresa == apc.IdProcesoAdopcionEmpresa && 
                    cd.IdDriverProceso == dp.IdDriverProceso, cancellationToken);

                if (!hasDrv)
                {
                    dbContext.CompanyDrivers.Add(new TDriverEmpresa
                    {
                        IdProcesoAdopcionEmpresa = apc.IdProcesoAdopcionEmpresa,
                        IdDriverProceso = dp.IdDriverProceso,
                        Cantidad = 0.00m,
                        PrecioUnitario = 0.00m,
                        CostoTotal = 0.00m,
                        Moneda = "USD",
                        FechaModificacion = DateTime.UtcNow,
                        UsuarioModificacion = "CONVENE_AUTO_CLONE"
                    });
                }
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-adopcion-empresa", "TProcesoAdopcionEmpresa", procesoId,
            proceso.NombreProceso, actorUserId, correlationId, $"Sincronización de convocatoria ({countAdded} agregadas, {countUpdated} actualizadas, {countRemoved} retiradas)", cancellationToken);

        return new AdoptionResult(true, $"Se actualizó la convocatoria ({countAdded} agregadas, {countUpdated} actualizadas, {countRemoved} retiradas).");
    }

    public async Task<AdoptionResult> RemoveCompanyFromProcessAsync(int procesoId, int procesoEmpresaId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var match = await dbContext.AdoptionProcessCompanies
            .FirstOrDefaultAsync(c => c.IdProcesoAdopcionTSI == procesoId && c.IdProcesoAdopcionEmpresa == procesoEmpresaId, cancellationToken);

        if (match is null)
            return new AdoptionResult(false, "La empresa subsidiaria no está vinculada a este proceso de evaluación.");

        var company = await dbContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == match.IdEmpresaSubsidiaria, cancellationToken);
        var companyName = company?.Nombre ?? $"Empresa {match.IdEmpresaSubsidiaria}";

        var linkedImplTechs = await dbContext.ImplementedTechnologies
            .Where(it => it.IdProcesoAdopcionEmpresa == procesoEmpresaId)
            .ToListAsync(cancellationToken);

        foreach (var impl in linkedImplTechs)
        {
            impl.IdProcesoAdopcionEmpresa = null;
        }

        dbContext.AdoptionProcessCompanies.Remove(match);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordRelationAsync("DELETE_COMPANY_PROCESS", "proceso-adopcion-empresa", "TProcesoAdopcionEmpresa", procesoEmpresaId,
            companyName, actorUserId, correlationId, $"Subsidiaria '{companyName}' retirada del proceso {procesoId}", cancellationToken);

        return new AdoptionResult(true, $"La subsidiaria '{companyName}' fue retirada exitosamente del proceso.");
    }

    public async Task<EvaluationReportsDto?> GetEvaluationReportsAsync(int procesoId, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.AsNoTracking().FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null) return null;

        var bb = await dbContext.BuildingBlocks.AsNoTracking().FirstOrDefaultAsync(b => b.Id == proceso.IdBuildingBlock, cancellationToken);
        var dominio = bb?.IdDominio is not null ? await dbContext.Domains.AsNoTracking().FirstOrDefaultAsync(d => d.Id == bb.IdDominio, cancellationToken) : null;
        var estado = await dbContext.TechnologyAdoptionStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == proceso.IdEstadoAdopcionTSI, cancellationToken);

        var esWaap = (bb?.Nombre?.Contains("WAAP", StringComparison.OrdinalIgnoreCase) == true ||
                      bb?.Nombre?.Contains("Web Application", StringComparison.OrdinalIgnoreCase) == true ||
                      bb?.Nombre?.Contains("WAF", StringComparison.OrdinalIgnoreCase) == true)
                     || proceso.NombreProceso.Contains("WAAP", StringComparison.OrdinalIgnoreCase)
                     || proceso.NombreProceso.Contains("WAF", StringComparison.OrdinalIgnoreCase);

        // Empresas convocadas en el proceso que efectivamente participan
        var convocadas = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .Where(c => c.IdProcesoAdopcionTSI == procesoId && c.Aplica)
            .ToListAsync(cancellationToken);

        var allCompanies = await dbContext.Companies.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);
        var implementedTechs = await dbContext.ImplementedTechnologies.AsNoTracking()
            .Where(it => it.IdBuildingBlock == proceso.IdBuildingBlock || convocadas.Select(cp => cp.IdProcesoAdopcionEmpresa).Contains(it.IdProcesoAdopcionEmpresa ?? 0))
            .ToListAsync(cancellationToken);

        var allTechs = await dbContext.Technologies.AsNoTracking().ToDictionaryAsync(t => t.Id, cancellationToken);
        var contracts = await dbContext.TechnologyContracts.AsNoTracking().ToListAsync(cancellationToken);
        var drivers = await dbContext.Drivers.AsNoTracking().ToListAsync(cancellationToken);
        var operationModels = await GetOperationModelsAsync(cancellationToken);

        // Capacidades y funcionalidades del Building Block
        var capacidadesBb = await dbContext.Capabilities.AsNoTracking()
            .Where(c => c.IdBuildingBlock == proceso.IdBuildingBlock)
            .OrderBy(c => c.OrdenVisualizacion ?? 999999)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        // Drivers del Proceso (Plantilla) y Drivers por Empresa
        var processDrivers = await dbContext.ProcessDrivers.AsNoTracking()
            .Where(dp => dp.IdProcesoAdopcionTSI == procesoId)
            .OrderBy(dp => dp.OrdenVisual ?? 999999)
            .ThenBy(dp => dp.IdDriverProceso)
            .ToListAsync(cancellationToken);

        var procEmpIds = convocadas.Select(cp => cp.IdProcesoAdopcionEmpresa).ToList();
        var companyDrivers = await dbContext.CompanyDrivers.AsNoTracking()
            .Where(cd => procEmpIds.Contains(cd.IdProcesoAdopcionEmpresa))
            .ToListAsync(cancellationToken);

        // REPORTE A: ALCANCE DEL PROCESO
        var alcanceRows = new List<EvaluationScopeReportRowDto>();
        // Datos para Reporte B
        var expirationCandidates = new List<(
            int EmpresaId,
            string EmpresaNombre,
            string? ProductoActual,
            decimal Throughput,
            int Apps,
            decimal RequestWaf,
            DateTime? FechaFin,
            bool EsPayg,
            string? TipoContrato,
            string? TipoOperacion,
            int CantidadDrivers,
            decimal CostoTotalDrivers,
            IReadOnlyDictionary<string, decimal> CompDriversMap,
            decimal? MontoContrato,
            string? MonedaContrato)>();
        // Datos para Reporte 3
        var volumeRows = new List<CompanyVolumeReportRowDto>();
        var genericDrivers = new List<CompanyDriverItemDto>();
        var allAvailableDriverNames = new List<string>();
        var driverUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (processDrivers.Count > 0)
        {
            foreach (var dp in processDrivers)
            {
                var dName = dp.DescripcionDriver.Trim();
                if (!allAvailableDriverNames.Contains(dName, StringComparer.OrdinalIgnoreCase))
                {
                    allAvailableDriverNames.Add(dName);
                }
                if (!string.IsNullOrWhiteSpace(dp.UnidadMedida) && !driverUnits.ContainsKey(dName))
                {
                    driverUnits[dName] = dp.UnidadMedida.Trim();
                }
            }
        }


        var orderedCompanyIds = ParseOrderedCompanyIds(proceso.OrdenEmpresas);

        // Si no hay empresas convocadas registradas, recopilar a partir de las empresas generales o asociadas
        var empresaIdsTarget = convocadas.Count > 0
            ? convocadas.Select(c => c.IdEmpresaSubsidiaria).Distinct().ToList()
            : implementedTechs.Select(it => it.IdEmpresaSubsidiaria).Distinct().ToList();

        if (empresaIdsTarget.Count == 0)
        {
            empresaIdsTarget = allCompanies.Keys.Take(10).ToList();
        }

        if (orderedCompanyIds.Count > 0)
        {
            var orderMap = orderedCompanyIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
            empresaIdsTarget = empresaIdsTarget
                .OrderBy(id => orderMap.TryGetValue(id, out var idx) ? idx : 999999)
                .ThenBy(id => allCompanies.TryGetValue(id, out var c) ? c.Nombre : string.Empty)
                .ToList();
        }

        foreach (var empId in empresaIdsTarget)
        {
            var emp = allCompanies.GetValueOrDefault(empId);
            var empNombre = emp?.Nombre ?? $"Empresa #{empId}";
            var pais = emp?.Pais ?? "Global";

            var matchingConvocada = convocadas.FirstOrDefault(c => c.IdEmpresaSubsidiaria == empId);
            var compImpls = matchingConvocada is not null
                ? implementedTechs.Where(it => it.IdProcesoAdopcionEmpresa == matchingConvocada.IdProcesoAdopcionEmpresa).ToList()
                : new List<TTecnologiaTSIimplementadaSubsidiaria>();

            if (compImpls.Count == 0)
            {
                compImpls = implementedTechs
                    .Where(it => it.IdEmpresaSubsidiaria == empId && (it.IdProcesoAdopcionEmpresa == null || it.IdProcesoAdopcionEmpresa == 0))
                    .OrderByDescending(it => it.EsTecnologiaPrimaria ? 1 : 0)
                    .ThenByDescending(it => it.IdTecnologiaTSIimplementadaSubsidiaria)
                    .Take(1)
                    .ToList();
            }

            var primaryImpl = compImpls.FirstOrDefault(i => i.EsTecnologiaPrimaria) ?? compImpls.FirstOrDefault();

            string? techNombre = null;
            string? tipoContrato = null;
            string? vendor = null;
            string? partner = null;
            string? tipoOperacion = null;
            DateTime? fechaVencimiento = null;
            bool esPayg = false;
            decimal? montoContrato = null;
            string? monedaContrato = null;

            decimal thgVal = 0m;
            int appsVal = 0;
            decimal reqWafVal = 0m;
            decimal anchoBanda = 0m;
            int dominios = 0;
            int subDominios = 0;
            int appsApiSec = 0;
            decimal apiProtReq = 0m;
            decimal reqAntibot = 0m;
            decimal dataTransferTb = 0m;
            string? reqSizeVal = null;
            string? respSizeVal = null;
            string? reqRespSize = "-";

            List<TDriver> compDrivers = [];

            if (primaryImpl is not null)
            {
                var t = allTechs.GetValueOrDefault(primaryImpl.IdTecnologiaTSI);
                techNombre = t?.NombreCorporativo ?? t?.NombreLocal;
                tipoContrato = t?.Licenciamiento;

                var compContracts = contracts.Where(c => c.IdTecnologiaTSIimplementadaSubsidiaria == primaryImpl.IdTecnologiaTSIimplementadaSubsidiaria).ToList();
                var contract = compContracts.OrderBy(c => c.EsPayg ? 1 : 0)
                    .ThenBy(c => c.FechaFin.HasValue ? c.FechaFin.Value : DateTime.MaxValue)
                    .FirstOrDefault();
                if (contract is not null)
                {
                    montoContrato = contract.MontoContratado ?? contract.MontoTrianual ?? contract.MontoAnual;
                    monedaContrato = !string.IsNullOrWhiteSpace(contract.Moneda) ? contract.Moneda : "USD";

                    if (contract.EsPayg)
                    {
                        esPayg = true;
                        tipoContrato = "PAYG";
                        fechaVencimiento = null;
                    }
                    else
                    {
                        fechaVencimiento = contract.FechaFin;
                        if (string.IsNullOrWhiteSpace(tipoContrato))
                        {
                            tipoContrato = contract.EsAdenda ? "Adenda" : "Contrato";
                        }
                    }

                    // Extraer Vendor y Partner reales de Observaciones ("Vendor: X / Partner: Y")
                    if (!string.IsNullOrWhiteSpace(contract.Observaciones))
                    {
                        var obs = contract.Observaciones;
                        var vMatch = System.Text.RegularExpressions.Regex.Match(obs, @"Vendor:\s*([^/]+?)(?:\s*/|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (vMatch.Success)
                        {
                            var parsedV = vMatch.Groups[1].Value.Trim();
                            if (!string.IsNullOrWhiteSpace(parsedV) && !parsedV.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                                vendor = parsedV;
                        }

                        var pMatch = System.Text.RegularExpressions.Regex.Match(obs, @"Partner:\s*(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (pMatch.Success)
                        {
                            var parsedP = pMatch.Groups[1].Value.Trim();
                            if (!string.IsNullOrWhiteSpace(parsedP) && !parsedP.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                                partner = parsedP;
                        }
                    }
                }

                if (!montoContrato.HasValue && compContracts.Count > 0)
                {
                    var anyC = compContracts.FirstOrDefault(c => c.MontoContratado.HasValue || c.MontoTrianual.HasValue || c.MontoAnual.HasValue);
                    if (anyC is not null)
                    {
                        montoContrato = anyC.MontoContratado ?? anyC.MontoTrianual ?? anyC.MontoAnual;
                        monedaContrato = !string.IsNullOrWhiteSpace(anyC.Moneda) ? anyC.Moneda : "USD";
                    }
                }

                var (dbVendor, _, dbPartner) = await GetVendorDetailsAsync(primaryImpl.IdTecnologiaTSI, cancellationToken);
                if (string.IsNullOrWhiteSpace(vendor)) vendor = dbVendor;
                if (string.IsNullOrWhiteSpace(partner)) partner = dbPartner;

                if (operationModels.TryGetValue(primaryImpl.IdTecnologiaTSIimplementadaSubsidiaria, out var opMod))
                {
                    tipoOperacion = opMod.TipoOperacionNombre;
                }

                compDrivers = drivers.Where(d => d.IdTecnologiaTSIimplementadaSubsidiaria == primaryImpl.IdTecnologiaTSIimplementadaSubsidiaria).ToList();
            }

            if (string.IsNullOrWhiteSpace(techNombre))
            {
                techNombre = "Sin tecnología registrada";
            }

            if (string.IsNullOrWhiteSpace(tipoContrato))
            {
                tipoContrato = primaryImpl is null ? "—" : "Contrato";
            }
            if (tipoContrato.Equals("PAYG", StringComparison.OrdinalIgnoreCase))
            {
                esPayg = true;
            }

            if (string.IsNullOrWhiteSpace(tipoOperacion))
            {
                tipoOperacion = primaryImpl is null ? "—" : "Autogestionado";
            }

            if (string.IsNullOrWhiteSpace(vendor))
            {
                vendor = primaryImpl is null ? "—" : "-";
            }

            if (string.IsNullOrWhiteSpace(partner))
            {
                partner = primaryImpl is null ? "—" : "Directo";
            }

            // Si el proceso cuenta con drivers en el nuevo modelo (TDriverProceso y TDriverEmpresa), utilizarlos prioritariamente
            var procEmpDriverValues = matchingConvocada is not null
                ? companyDrivers.Where(cd => cd.IdProcesoAdopcionEmpresa == matchingConvocada.IdProcesoAdopcionEmpresa).ToList()
                : [];

            if (processDrivers.Count > 0)
            {
                foreach (var dp in processDrivers)
                {
                    var cd = procEmpDriverValues.FirstOrDefault(p => p.IdDriverProceso == dp.IdDriverProceso);
                    var cant = cd?.Cantidad ?? 0m;
                    var precio = cd?.PrecioUnitario ?? 0m;
                    var total = cd?.CostoTotal ?? (cant * precio);
                    var moneda = string.IsNullOrWhiteSpace(cd?.Moneda) ? "USD" : cd.Moneda;
                    var desc = (dp.DescripcionDriver ?? string.Empty).Trim().ToLowerInvariant();

                    genericDrivers.Add(new CompanyDriverItemDto(
                        cd?.IdDriverEmpresa ?? dp.IdDriverProceso,
                        empId,
                        empNombre,
                        techNombre,
                        dp.DescripcionDriver ?? "Driver de Consumo",
                        dp.UnidadMedida ?? "Unidades",
                        cant,
                        precio,
                        moneda,
                        total));

                    // Mapeo automático de métricas de volumetría a partir de los drivers formales del proceso
                    if (desc.StartsWith("throughput"))
                    {
                        thgVal = cant;
                    }
                    else if (desc.StartsWith("ancho de banda"))
                    {
                        anchoBanda = cant;
                    }
                    else if (desc.Equals("dominio") || desc.StartsWith("dominio (") || desc.StartsWith("dominio "))
                    {
                        dominios = (int)cant;
                    }
                    else if (desc.Contains("subdominio") || desc.Contains("sub dominio"))
                    {
                        subDominios = (int)cant;
                    }
                    else if ((desc.Contains("api security") || desc.Contains("api sec")) && (desc.Contains("aplicaciones") || desc.Contains("fqdn")))
                    {
                        appsApiSec = (int)cant;
                    }
                    else if (desc.Contains("aplicaciones") || desc.Contains("fqdn"))
                    {
                        appsVal = (int)cant;
                    }
                    else if (desc.Contains("api protection") || desc.Contains("api security request"))
                    {
                        apiProtReq = cant;
                    }
                    else if (desc.Contains("antibot") && !desc.Contains("waf"))
                    {
                        reqAntibot = cant;
                    }
                    else if (desc.Contains("waf") && !desc.Contains("antibot"))
                    {
                        reqWafVal = cant;
                    }
                    else if (desc.Contains("transfer"))
                    {
                        dataTransferTb = cant;
                    }
                    else if (desc.StartsWith("request size") || desc.StartsWith("req size"))
                    {
                        reqSizeVal = cant > 0 ? cant.ToString("N0") : (cd?.Cantidad.HasValue == true ? "0" : null);
                    }
                    else if (desc.StartsWith("response size") || desc.StartsWith("resp size"))
                    {
                        respSizeVal = cant > 0 ? cant.ToString("N0") : (cd?.Cantidad.HasValue == true ? "0" : null);
                    }
                }

                if (!string.IsNullOrWhiteSpace(reqSizeVal) || !string.IsNullOrWhiteSpace(respSizeVal))
                {
                    reqRespSize = $"{reqSizeVal ?? "—"} / {respSizeVal ?? "—"}";
                }
            }
            else if (compDrivers.Count > 0)
            {
                foreach (var d in compDrivers)
                {
                    var cant = d.Cantidad ?? 0m;
                    var precio = d.PrecioUnitario ?? 0m;
                    var moneda = string.IsNullOrWhiteSpace(d.Moneda) ? "USD" : d.Moneda;
                    var desc = (d.DescripcionDriver ?? string.Empty).Trim().ToLowerInvariant();

                    genericDrivers.Add(new CompanyDriverItemDto(
                        d.IdDriver,
                        empId,
                        empNombre,
                        techNombre,
                        string.IsNullOrWhiteSpace(d.DescripcionDriver) ? "Driver de Consumo" : d.DescripcionDriver,
                        string.IsNullOrWhiteSpace(d.UnidadMedida) ? "Unidades" : d.UnidadMedida,
                        cant,
                        precio,
                        moneda,
                        cant * precio));

                    if (desc.StartsWith("throughput"))
                    {
                        thgVal = cant;
                    }
                    else if (desc.StartsWith("ancho de banda"))
                    {
                        anchoBanda = cant;
                    }
                    else if (desc.Equals("dominio") || desc.StartsWith("dominio (") || desc.StartsWith("dominio "))
                    {
                        dominios = (int)cant;
                    }
                    else if (desc.Contains("subdominio") || desc.Contains("sub dominio"))
                    {
                        subDominios = (int)cant;
                    }
                    else if ((desc.Contains("api security") || desc.Contains("api sec")) && (desc.Contains("aplicaciones") || desc.Contains("fqdn")))
                    {
                        appsApiSec = (int)cant;
                    }
                    else if (desc.Contains("aplicaciones") || desc.Contains("fqdn"))
                    {
                        appsVal = (int)cant;
                    }
                    else if (desc.Contains("api protection") || desc.Contains("api security request"))
                    {
                        apiProtReq = cant;
                    }
                    else if (desc.Contains("antibot") && !desc.Contains("waf"))
                    {
                        reqAntibot = cant;
                    }
                    else if (desc.Contains("waf") && !desc.Contains("antibot"))
                    {
                        reqWafVal = cant;
                    }
                    else if (desc.Contains("transfer"))
                    {
                        dataTransferTb = cant;
                    }
                    else if (desc.StartsWith("request size") || desc.StartsWith("req size"))
                    {
                        reqSizeVal = cant > 0 ? cant.ToString("N0") : (d.Cantidad.HasValue ? "0" : null);
                    }
                    else if (desc.StartsWith("response size") || desc.StartsWith("resp size"))
                    {
                        respSizeVal = cant > 0 ? cant.ToString("N0") : (d.Cantidad.HasValue ? "0" : null);
                    }
                }

                if (!string.IsNullOrWhiteSpace(reqSizeVal) || !string.IsNullOrWhiteSpace(respSizeVal))
                {
                    reqRespSize = $"{reqSizeVal ?? "—"} / {respSizeVal ?? "—"}";
                }
            }
            else
            {
                genericDrivers.Add(new CompanyDriverItemDto(
                    0,
                    empId,
                    empNombre,
                    techNombre,
                    "Sin drivers de consumo registrados",
                    "—",
                    0m,
                    0m,
                    "USD",
                    0m));
            }

            var compDriversCount = processDrivers.Count > 0 
                ? processDrivers.Count 
                : compDrivers.Count;
            var compDriversCost = processDrivers.Count > 0
                ? procEmpDriverValues.Sum(cd => cd.CostoTotal ?? ((cd.Cantidad ?? 0m) * (cd.PrecioUnitario ?? 0m)))
                : compDrivers.Sum(d => (d.Cantidad ?? 0m) * (d.PrecioUnitario ?? 0m));

            var compDriverValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (processDrivers.Count > 0)
            {
                foreach (var dp in processDrivers)
                {
                    var key = dp.DescripcionDriver.Trim();
                    var empDrv = procEmpDriverValues.FirstOrDefault(cd => cd.IdDriverProceso == dp.IdDriverProceso);
                    compDriverValues[key] = empDrv?.Cantidad ?? 0m;
                    if (!string.IsNullOrWhiteSpace(dp.UnidadMedida) && !driverUnits.ContainsKey(key))
                    {
                        driverUnits[key] = dp.UnidadMedida.Trim();
                    }
                }
            }
            else if (compDrivers.Count > 0)
            {
                foreach (var d in compDrivers)
                {
                    if (!string.IsNullOrWhiteSpace(d.DescripcionDriver))
                    {
                        var key = d.DescripcionDriver.Trim();
                        var cant = d.Cantidad ?? 0m;
                        compDriverValues[key] = cant;
                        if (!string.IsNullOrWhiteSpace(d.UnidadMedida) && !driverUnits.ContainsKey(key))
                        {
                            driverUnits[key] = d.UnidadMedida.Trim();
                        }
                        if (!allAvailableDriverNames.Contains(key, StringComparer.OrdinalIgnoreCase))
                        {
                            allAvailableDriverNames.Add(key);
                        }
                    }
                }
            }

            if (esWaap && processDrivers.Count == 0 && compDrivers.Count == 0)
            {
                if (!compDriverValues.ContainsKey("Throughput (mensual)")) compDriverValues["Throughput (mensual)"] = 0m;
                if (!compDriverValues.ContainsKey("Cantidad de aplicaciones (FQDN)")) compDriverValues["Cantidad de aplicaciones (FQDN)"] = 0m;
                if (!compDriverValues.ContainsKey("Millones Request WAF (mensual)")) compDriverValues["Millones Request WAF (mensual)"] = 0m;

                if (!driverUnits.ContainsKey("Throughput (mensual)")) driverUnits["Throughput (mensual)"] = "Gbps";
                if (!driverUnits.ContainsKey("Cantidad de aplicaciones (FQDN)")) driverUnits["Cantidad de aplicaciones (FQDN)"] = "FQDN";
                if (!driverUnits.ContainsKey("Millones Request WAF (mensual)")) driverUnits["Millones Request WAF (mensual)"] = "Millones";
            }

            alcanceRows.Add(new EvaluationScopeReportRowDto(
                empId,
                empNombre,
                pais,
                fechaVencimiento,
                techNombre,
                tipoContrato,
                vendor,
                partner,
                tipoOperacion));

            expirationCandidates.Add((
                empId,
                empNombre,
                techNombre,
                thgVal,
                appsVal,
                reqWafVal,
                fechaVencimiento,
                esPayg,
                tipoContrato,
                tipoOperacion,
                compDriversCount,
                compDriversCost,
                compDriverValues,
                montoContrato,
                monedaContrato));

            volumeRows.Add(new CompanyVolumeReportRowDto(
                empId,
                empNombre,
                techNombre,
                thgVal,
                anchoBanda,
                dominios,
                subDominios,
                appsVal,
                appsApiSec,
                apiProtReq,
                reqAntibot,
                reqWafVal,
                reqAntibot + reqWafVal,
                dataTransferTb,
                reqSizeVal,
                respSizeVal,
                reqRespSize));
        }

        // REPORTE B: VENCIMIENTO CONTRACTUAL & PROYECCIÓN ACUMULADA
        // Períodos hitos dinámicos generados a partir de las fechas reales de vencimiento (Año y Mes)
        var hitos = new List<ContractTimelineMilestoneDto>();
        var distinctPeriods = expirationCandidates
            .Where(c => !c.EsPayg && c.FechaFin.HasValue)
            .Select(c => (Anio: c.FechaFin!.Value.Year, Mes: c.FechaFin!.Value.Month))
            .Distinct()
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.Mes)
            .ToList();

        string[] mesesAbrev = ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Set", "Oct", "Nov", "Dic"];

        foreach (var (anio, mes) in distinctPeriods)
        {
            var mesLabel = (mes >= 1 && mes <= 12) ? mesesAbrev[mes - 1] : $"M{mes:D2}";
            var anioLabel = anio.ToString().Length >= 2 ? anio.ToString().Substring(anio.ToString().Length - 2) : anio.ToString();
            var periodoLabel = $"{mesLabel}-{anioLabel}";
            hitos.Add(new ContractTimelineMilestoneDto(periodoLabel, anio, mes, false));
        }

        // Si existen contratos PAYG o subsidiarias sin fecha fija de término (o si no hay hitos de fecha), agregar columna PAYG al final
        var hasPaygOrSinFecha = expirationCandidates.Any(c => c.EsPayg || !c.FechaFin.HasValue);
        if (hasPaygOrSinFecha || hitos.Count == 0)
        {
            hitos.Add(new ContractTimelineMilestoneDto("PAYG", 2099, 12, true));
        }

        // Ordenar candidatos por fecha más cercana (los PAYG o sin vencimiento van al final), desempatando por el orden visual configurado
        var orderMapHito = orderedCompanyIds.Select((id, idx) => (id, idx)).ToDictionary(x => x.id, x => x.idx);
        var sortedCandidates = expirationCandidates
            .OrderBy(c => c.EsPayg)
            .ThenBy(c => c.FechaFin.HasValue ? c.FechaFin.Value : DateTime.MaxValue)
            .ThenBy(c => orderMapHito.TryGetValue(c.EmpresaId, out var idx) ? idx : 999999)
            .ThenBy(c => c.EmpresaNombre)
            .ToList();

        var expirationRows = new List<ContractExpirationRowDto>();
        var thgAcumulado = new Dictionary<string, decimal>();
        var appsAcumulado = new Dictionary<string, int>();
        var reqWafAcumulado = new Dictionary<string, decimal>();
        var subsAcumulado = new Dictionary<string, int>();

        foreach (var h in hitos)
        {
            thgAcumulado[h.PeriodoLabel] = 0m;
            appsAcumulado[h.PeriodoLabel] = 0;
            reqWafAcumulado[h.PeriodoLabel] = 0m;
            subsAcumulado[h.PeriodoLabel] = 0;
        }

        int seq = 1;
        for (int i = 0; i < sortedCandidates.Count; i++)
        {
            var item = sortedCandidates[i];
            var hitosDict = new Dictionary<string, bool>();

            // Determinar hito asignado según el Año y Mes de vencimiento
            string assignedPeriodo;
            if (item.EsPayg || !item.FechaFin.HasValue)
            {
                assignedPeriodo = "PAYG";
            }
            else
            {
                var f = item.FechaFin.Value;
                var matchHito = hitos.FirstOrDefault(h => !h.EsPaygSinVencimiento && h.Anio == f.Year && h.Mes == f.Month);
                if (matchHito is not null)
                {
                    assignedPeriodo = matchHito.PeriodoLabel;
                }
                else
                {
                    assignedPeriodo = hitos.Last().PeriodoLabel;
                }
            }

            foreach (var h in hitos)
            {
                hitosDict[h.PeriodoLabel] = (h.PeriodoLabel == assignedPeriodo);
            }

            string labelVenc = item.EsPayg ? "PAYG" : item.FechaFin?.ToString("dd/MM/yyyy") ?? assignedPeriodo;

            expirationRows.Add(new ContractExpirationRowDto(
                seq++,
                item.EmpresaId,
                item.EmpresaNombre,
                item.ProductoActual,
                item.Throughput,
                item.Apps,
                item.RequestWaf,
                item.FechaFin,
                labelVenc,
                hitosDict,
                item.TipoContrato,
                item.TipoOperacion,
                item.CantidadDrivers,
                item.CostoTotalDrivers,
                item.CompDriversMap,
                item.MontoContrato,
                item.MonedaContrato));
        }

        // Calcular acumulación progresiva a través de los hitos cronológicos
        decimal runThg = 0m;
        int runApps = 0;
        decimal runReq = 0m;
        int runSubs = 0;

        foreach (var h in hitos)
        {
            var rowsInHito = expirationRows.Where(r => r.HitoExpiracionPorPeriodo.TryGetValue(h.PeriodoLabel, out var active) && active).ToList();
            runThg += rowsInHito.Sum(r => r.ThroughputGbMes);
            runApps += rowsInHito.Sum(r => r.CantidadAppFqdn);
            runReq += rowsInHito.Sum(r => r.RequestWafMillonesMes);
            runSubs += rowsInHito.Count;

            thgAcumulado[h.PeriodoLabel] = Math.Round(runThg, 3);
            appsAcumulado[h.PeriodoLabel] = runApps;
            reqWafAcumulado[h.PeriodoLabel] = Math.Round(runReq, 3);
            subsAcumulado[h.PeriodoLabel] = runSubs;
        }

        if (esWaap && processDrivers.Count == 0)
        {
            var defaultWaapDrivers = new[] { "Throughput (mensual)", "Cantidad de aplicaciones (FQDN)", "Millones Request WAF (mensual)" };
            foreach (var d in defaultWaapDrivers.Reverse())
            {
                if (allAvailableDriverNames.Contains(d, StringComparer.OrdinalIgnoreCase))
                {
                    allAvailableDriverNames.RemoveAll(x => string.Equals(x, d, StringComparison.OrdinalIgnoreCase));
                }
                allAvailableDriverNames.Insert(0, d);
            }
        }

        var driversSeleccionados = new List<string>();
        if (!string.IsNullOrWhiteSpace(proceso.DriversReporteVencimiento))
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(proceso.DriversReporteVencimiento);
                if (parsed != null && parsed.Count > 0)
                {
                    driversSeleccionados = parsed.Where(p => allAvailableDriverNames.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
                }
            }
            catch
            {
                driversSeleccionados = proceso.DriversReporteVencimiento
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => allAvailableDriverNames.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        if (driversSeleccionados.Count == 0)
        {
            if (esWaap && processDrivers.Count == 0)
            {
                driversSeleccionados = ["Throughput (mensual)", "Cantidad de aplicaciones (FQDN)", "Millones Request WAF (mensual)"];
            }
            else if (allAvailableDriverNames.Count > 0)
            {
                driversSeleccionados = allAvailableDriverNames.Take(4).ToList();
            }
        }

        var vencimientoReport = new ContractExpirationReportDto(
            hitos,
            expirationRows,
            thgAcumulado,
            appsAcumulado,
            reqWafAcumulado,
            Math.Round(expirationRows.Sum(r => r.ThroughputGbMes), 3),
            expirationRows.Sum(r => r.CantidadAppFqdn),
            Math.Round(expirationRows.Sum(r => r.RequestWafMillonesMes), 3),
            subsAcumulado,
            expirationRows.Count,
            allAvailableDriverNames,
            driversSeleccionados,
            driverUnits);

        // REPORTE 3: VOLUMETRÍA POR EMPRESA (con columnas dinámicas y totales)
        var totalInversionDrivers = processDrivers.Count > 0
            ? companyDrivers.Sum(cd => cd.CostoTotal ?? ((cd.Cantidad ?? 0m) * (cd.PrecioUnitario ?? 0m)))
            : genericDrivers.Where(g => g.DriverId > 0).Sum(g => g.CostoTotal);
        var totalCantidadDrivers = processDrivers.Count > 0
            ? processDrivers.Count
            : genericDrivers.Count(g => g.DriverId > 0);

        // Construcción de la matriz de columnas dinámicas por Driver real: "[Descripción] / [Unidad]"
        var activeDriverItems = genericDrivers.Where(g => !string.IsNullOrWhiteSpace(g.DescripcionDriver) && (processDrivers.Count > 0 || g.DriverId > 0)).ToList();
        
        List<string> distinctColHeaders;
        if (processDrivers.Count > 0)
        {
            distinctColHeaders = processDrivers
                .Select(dp => $"{dp.DescripcionDriver.Trim()} / {dp.UnidadMedida.Trim()}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            distinctColHeaders = activeDriverItems
                .Select(d => $"{d.DescripcionDriver.Trim()} / {d.UnidadMedida.Trim()}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var matrixDriverRows = new List<CompanyDriverMatrixRowDto>();
        var totalPorColumna = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in distinctColHeaders)
        {
            totalPorColumna[col] = 0m;
        }

        foreach (var alcance in alcanceRows)
        {
            var empDrvValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var empDriversList = activeDriverItems.Where(d => d.EmpresaId == alcance.EmpresaId).ToList();

            foreach (var col in distinctColHeaders)
            {
                var matchingDrv = empDriversList.FirstOrDefault(d => $"{d.DescripcionDriver.Trim()} / {d.UnidadMedida.Trim()}".Equals(col, StringComparison.OrdinalIgnoreCase));
                var cant = matchingDrv?.Cantidad ?? 0m;
                empDrvValues[col] = cant;
                totalPorColumna[col] += cant;
            }

            matrixDriverRows.Add(new CompanyDriverMatrixRowDto(
                alcance.EmpresaId,
                alcance.EmpresaNombre,
                alcance.TecnologiaAsIs,
                empDrvValues));
        }

        var matrizDrivers = new CompanyDriverMatrixDto(
            distinctColHeaders,
            matrixDriverRows,
            totalPorColumna);

        var volumeReport = new CompanyVolumeReportDto(
            volumeRows,
            Math.Round(volumeRows.Sum(v => v.ThroughputMensualGbps), 3),
            Math.Round(volumeRows.Sum(v => v.AnchoBandaMensualGbps), 3),
            volumeRows.Sum(v => v.DominioSubdominios),
            volumeRows.Sum(v => v.CantidadAppsFqdn),
            volumeRows.Sum(v => v.CantidadAppsFqdnApiSecurity),
            Math.Round(volumeRows.Sum(v => v.ApiProtectionRequestMillonesMes), 3),
            Math.Round(volumeRows.Sum(v => v.MillonesRequestAntibot), 3),
            Math.Round(volumeRows.Sum(v => v.MillonesRequestWaf), 3),
            Math.Round(volumeRows.Sum(v => v.MillonesRequestAntibotWaf), 3),
            Math.Round(volumeRows.Sum(v => v.DataTransferTbMensual), 3),
            genericDrivers,
            totalInversionDrivers,
            totalCantidadDrivers,
            matrizDrivers,
            TotalDominio: volumeRows.Sum(v => v.Dominio),
            TotalSubDominios: volumeRows.Sum(v => v.SubDominios));

        // REPORTE 5: PROYECCIÓN DE COSTOS AS-IS (valorizado)
        var asIsCostRows = new List<AsIsCostItemDto>();
        foreach (var d in genericDrivers)
        {
            asIsCostRows.Add(new AsIsCostItemDto(
                d.DriverId,
                d.EmpresaId,
                d.EmpresaNombre,
                d.TecnologiaAsIs,
                d.DescripcionDriver,
                d.UnidadMedida,
                d.Cantidad,
                d.PrecioUnitario,
                d.Moneda,
                d.CostoTotal));
        }

        var costosAsIsReport = new AsIsCostsReportDto(
            asIsCostRows,
            totalInversionDrivers,
            totalCantidadDrivers);

        // REPORTE 4: MATRIZ DE CAPACIDADES POR EMPRESA
        // Se alinea estrictamente a las capacidades asociadas al Building Block en la tabla maestra (TCapacidadDeSeguridad)
        var standardCaps = capacidadesBb
            .Where(c => !string.IsNullOrWhiteSpace(c.Nombre))
            .Select(c => c.Nombre!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Si el Building Block no tiene capacidades registradas en la tabla maestra,
        // se deja standardCaps vacío y la vista mostrará un mensaje informativo
        // en lugar de inyectar capacidades de otro Building Block (ej. WAAP)

        var procEmpresaIds = convocadas.Select(c => c.IdProcesoAdopcionEmpresa).ToList();
        var savedCompanyCaps = await dbContext.ProcessCompanyCapabilities.AsNoTracking()
            .Where(c => procEmpresaIds.Contains(c.IdProcesoAdopcionEmpresa))
            .ToListAsync(cancellationToken);

        var matrixRows = new List<CompanyCapabilityMatrixRowDto>();
        for (int i = 0; i < volumeRows.Count; i++)
        {
            var v = volumeRows[i];
            var matchingProcEmp = convocadas.FirstOrDefault(c => c.IdEmpresaSubsidiaria == v.EmpresaId);
            var capDict = new Dictionary<string, CompanyCapabilityMatrixCellDto>();

            for (int ci = 0; ci < standardCaps.Count; ci++)
            {
                var capName = standardCaps[ci];
                var capEntity = capacidadesBb.FirstOrDefault(c => (c.Nombre ?? "").Trim().Equals(capName, StringComparison.OrdinalIgnoreCase));
                var capId = capEntity?.Id ?? (ci + 1);

                string estadoCodigo = "NA";
                string? com = null;

                if (matchingProcEmp is not null)
                {
                    var saved = savedCompanyCaps.FirstOrDefault(sc => sc.IdProcesoAdopcionEmpresa == matchingProcEmp.IdProcesoAdopcionEmpresa && sc.IdCapacidad == capId);
                    if (saved is not null)
                    {
                        estadoCodigo = string.IsNullOrWhiteSpace(saved.EstadoCobertura) ? "NA" : saved.EstadoCobertura.Trim();
                        com = saved.Comentario;
                    }
                }

                capDict[capName] = new CompanyCapabilityMatrixCellDto(capId, capName, estadoCodigo, com);
            }

            string? comentarioSub = matchingProcEmp?.ComentarioCapacidades;

            matrixRows.Add(new CompanyCapabilityMatrixRowDto(
                v.EmpresaId,
                v.EmpresaNombre,
                v.TecnologiaAsIs,
                capDict,
                comentarioSub,
                matchingProcEmp?.IdProcesoAdopcionEmpresa ?? 0));
        }

        var capsMatrixReport = new CompanyCapabilitiesMatrixDto(standardCaps, matrixRows);

        return new EvaluationReportsDto(
            proceso.IdProcesoAdopcionTSI,
            proceso.CodigoProceso,
            proceso.NombreProceso,
            proceso.IdBuildingBlock,
            bb?.Nombre ?? "Building Block",
            dominio?.Dominio ?? "Dominio TSI",
            proceso.LiderCorporativoTSI,
            proceso.FechaInicio,
            proceso.FechaEstimadaCierre,
            estado?.Nombre ?? "En Evaluación",
            alcanceRows,
            vencimientoReport,
            volumeReport,
            capsMatrixReport,
            esWaap,
            costosAsIsReport,
            proceso.OrdenEmpresas);
    }

    public async Task<AdoptionResult> DeactivateProcessAsync(int procesoId, string motivoBaja, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null) return new AdoptionResult(false, "El proceso de adopción no existe.");

        proceso.Objetivo = $"[DADO DE BAJA: {motivoBaja}] {proceso.Objetivo}";
        if (!proceso.NombreProceso.StartsWith("[DADO DE BAJA", StringComparison.OrdinalIgnoreCase))
        {
            proceso.NombreProceso = $"[DADO DE BAJA: {motivoBaja}] {proceso.NombreProceso}";
        }
        proceso.FechaEstimadaCierre = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditTrail.RecordUpdateAsync("proceso-adopcion-tsi", "TProcesoAdopcionTSI", proceso.IdProcesoAdopcionTSI,
            proceso.NombreProceso, actorUserId, correlationId, $"Dado de baja. Motivo: {motivoBaja}", cancellationToken);

        return new AdoptionResult(true, "La evaluación ha sido dada de baja exitosamente.");
    }

    public async Task<AdoptionResult> DeleteProcessCascadeAsync(int procesoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null)
            return new AdoptionResult(false, "El proceso de evaluación de adopción no existe.");

        var procCode = proceso.CodigoProceso ?? $"PROC-{proceso.IdProcesoAdopcionTSI}";
        var procName = proceso.NombreProceso ?? $"Proceso {proceso.IdProcesoAdopcionTSI}";

        await using var tx = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            // 1. Obtener empresas convocadas para desvincular tecnologías implementadas de las subsidiarias
            var empresasProceso = await dbContext.AdoptionProcessCompanies
                .Where(ep => ep.IdProcesoAdopcionTSI == procesoId)
                .ToListAsync(cancellationToken);

            var empresaProcesoIds = empresasProceso.Select(ep => ep.IdProcesoAdopcionEmpresa).ToHashSet();

            if (empresaProcesoIds.Count > 0)
            {
                var techsReferencingProcess = await dbContext.ImplementedTechnologies
                    .Where(it => it.IdProcesoAdopcionEmpresa.HasValue && empresaProcesoIds.Contains(it.IdProcesoAdopcionEmpresa.Value))
                    .ToListAsync(cancellationToken);

                foreach (var tech in techsReferencingProcess)
                {
                    tech.IdProcesoAdopcionEmpresa = null;
                }
            }

            // 2. Servicios de TI vinculados al proceso (TServicioTecnologia) y sus tarifarios hijos
            var services = await dbContext.TechnologyServices
                .Include(s => s.TarifariosProyecto)
                .Include(s => s.TarifariosOperacion)
                .Where(s => s.IdProcesoAdopcionTSI == procesoId)
                .ToListAsync(cancellationToken);

            if (services.Count > 0)
            {
                foreach (var s in services)
                {
                    if (s.TarifariosProyecto.Count > 0)
                        dbContext.ProjectRateCards.RemoveRange(s.TarifariosProyecto);

                    if (s.TarifariosOperacion.Count > 0)
                        dbContext.OperationRateCards.RemoveRange(s.TarifariosOperacion);
                }

                dbContext.TechnologyServices.RemoveRange(services);
            }

            // 3. Estándares históricos designados vinculados al proceso
            var standardHistories = await dbContext.StandardTechnologyHistories
                .Where(e => e.IdProcesoAdopcionTSI == procesoId)
                .ToListAsync(cancellationToken);

            if (standardHistories.Count > 0)
            {
                dbContext.StandardTechnologyHistories.RemoveRange(standardHistories);
            }

            // 4. Empresas convocadas al proceso
            if (empresasProceso.Count > 0)
            {
                dbContext.AdoptionProcessCompanies.RemoveRange(empresasProceso);
            }

            // 5. Eliminar el proceso de evaluación en sí
            dbContext.AdoptionProcesses.Remove(proceso);

            await dbContext.SaveChangesAsync(cancellationToken);

            // 6. Registrar en la pista de auditoría
            await auditTrail.RecordRelationAsync(
                "DELETE_EVALUATION_PROCESS",
                "proceso-adopcion-tsi",
                "TProcesoAdopcionTSI",
                procesoId,
                $"{procCode} - {procName}",
                actorUserId,
                correlationId,
                $"Eliminación en cascada de la evaluación '{procCode}' y todas sus tablas hijas asociadas ({empresasProceso.Count} subsidiarias convocadas, {services.Count} servicios técnicos, {standardHistories.Count} designaciones de estándar).",
                cancellationToken);

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return new AdoptionResult(true, $"La evaluación '{procCode}' y todos sus registros asociados fueron eliminados permanentemente.");
        }
        catch (Exception ex)
        {
            if (tx != null)
            {
                await tx.RollbackAsync(CancellationToken.None);
            }
            return new AdoptionResult(false, $"Error al eliminar la evaluación: {ex.Message}");
        }
    }

    public async Task<AdoptionResult> FinalizeEvaluationWithStandardAsync(FinalizeEvaluationWithStandardCommand command, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == command.ProcesoId, cancellationToken);
        if (proceso is null) return new AdoptionResult(false, "El proceso de adopción no existe.");

        var bbExists = await dbContext.BuildingBlocks.AnyAsync(b => b.Id == command.BuildingBlockId, cancellationToken);
        if (!bbExists) return new AdoptionResult(false, "El Building Block especificado no existe.");

        var tech = await dbContext.Technologies.FirstOrDefaultAsync(t => t.Id == command.TecnologiaId, cancellationToken);
        if (tech is null) return new AdoptionResult(false, "La tecnología especificada no existe.");

        var techName = tech.NombreCorporativo ?? tech.NombreLocal ?? $"Tecnología #{tech.Id}";

        // 1. Establecer el estándar corporativo
        var standardCmd = new SetCorporateStandardCommand(
            BuildingBlockId: command.BuildingBlockId,
            TecnologiaId: command.TecnologiaId,
            ProcesoAdopcionId: command.ProcesoId,
            RolEstandar: command.RolEstandar,
            FechaInicio: command.FechaInicioVigencia,
            MotivoCambio: command.MotivoAdjudicacion ?? $"Adjudicación de estándar resultante de la evaluación '{proceso.CodigoProceso}'",
            SustentoArquitectura: command.SustentoArquitectura,
            ActorUserId: command.ActorUserId,
            CorrelationId: command.CorrelationId);

        var standardResult = await SetCorporateStandardAsync(standardCmd, cancellationToken);
        if (!standardResult.Succeeded)
        {
            return standardResult;
        }

        // 2. Procesar las empresas que se alinean de inmediato con la nueva instancia corporativa
        var alignedCompanyIds = command.SubsidiariasAlineadasIds ?? [];
        foreach (var empresaId in alignedCompanyIds)
        {
            var procEmp = await dbContext.AdoptionProcessCompanies
                .FirstOrDefaultAsync(cp => cp.IdProcesoAdopcionTSI == command.ProcesoId && cp.IdEmpresaSubsidiaria == empresaId, cancellationToken);

            var existingTech = await dbContext.ImplementedTechnologies
                .FirstOrDefaultAsync(it => it.IdEmpresaSubsidiaria == empresaId && it.IdTecnologiaTSI == command.TecnologiaId && it.IdBuildingBlock == command.BuildingBlockId, cancellationToken);

            int implId;
            if (existingTech is not null)
            {
                existingTech.EsInstanciaCorporativa = true;
                existingTech.EsTecnologiaPrimaria = true;
                existingTech.IdProcesoAdopcionEmpresa = procEmp?.IdProcesoAdopcionEmpresa;
                implId = existingTech.IdTecnologiaTSIimplementadaSubsidiaria;
            }
            else
            {
                var newImpl = new TTecnologiaTSIimplementadaSubsidiaria
                {
                    IdEmpresaSubsidiaria = empresaId,
                    IdTecnologiaTSI = command.TecnologiaId,
                    IdBuildingBlock = command.BuildingBlockId,
                    IdProcesoAdopcionEmpresa = procEmp?.IdProcesoAdopcionEmpresa,
                    EsTecnologiaPrimaria = true,
                    EsInstanciaCorporativa = true,
                    VersionDesplegada = "Instancia Corporativa"
                };
                dbContext.ImplementedTechnologies.Add(newImpl);
                await dbContext.SaveChangesAsync(cancellationToken);
                implId = newImpl.IdTecnologiaTSIimplementadaSubsidiaria;
            }

            // Si se suministró contrato corporativo maestro, vincularlo
            if (!string.IsNullOrWhiteSpace(command.NumeroContratoCorporativo))
            {
                var contractExists = await dbContext.TechnologyContracts
                    .AnyAsync(c => c.IdTecnologiaTSIimplementadaSubsidiaria == implId && c.NumeroContrato == command.NumeroContratoCorporativo.Trim(), cancellationToken);

                if (!contractExists)
                {
                    var corpContract = new TContratoTecnologia
                    {
                        IdTecnologiaTSIimplementadaSubsidiaria = implId,
                        NumeroContrato = command.NumeroContratoCorporativo.Trim(),
                        EsAdenda = false,
                        EsPayg = command.EsPaygContratoCorporativo,
                        FechaInicio = command.EsPaygContratoCorporativo ? null : command.FechaInicioContratoCorporativo,
                        FechaFin = command.EsPaygContratoCorporativo ? null : command.FechaFinContratoCorporativo,
                        FechaAdjudicacion = command.EsPaygContratoCorporativo ? null : command.FechaAdjudicacionContratoCorporativo,
                        MontoContratado = command.MontoContratoCorporativo,
                        Moneda = string.IsNullOrWhiteSpace(command.MonedaContratoCorporativo) ? "USD" : command.MonedaContratoCorporativo.Trim().ToUpperInvariant(),
                        Observaciones = $"Contrato corporativo adjudicado en proceso {proceso.CodigoProceso}",
                        FechaRegistro = DateTime.UtcNow,
                        UsuarioRegistro = "SYSTEM"
                    };
                    dbContext.TechnologyContracts.Add(corpContract);
                }
            }

            // Si se suministraron drivers corporativos negociados, asociarlos
            if (command.DriversCorporativos != null && command.DriversCorporativos.Count > 0)
            {
                foreach (var cd in command.DriversCorporativos)
                {
                    if (!string.IsNullOrWhiteSpace(cd.Descripcion))
                    {
                        var driverExists = await dbContext.Drivers.AnyAsync(
                            d => d.IdTecnologiaTSIimplementadaSubsidiaria == implId && d.DescripcionDriver == cd.Descripcion.Trim(), cancellationToken);

                        if (!driverExists)
                        {
                            dbContext.Drivers.Add(new TDriver
                            {
                                IdTecnologiaTSIimplementadaSubsidiaria = implId,
                                DescripcionDriver = cd.Descripcion.Trim(),
                                UnidadMedida = cd.UnidadMedida?.Trim(),
                                Cantidad = cd.Cantidad,
                                PrecioUnitario = cd.PrecioUnitario,
                                Moneda = string.IsNullOrWhiteSpace(cd.Moneda) ? "USD" : cd.Moneda.Trim().ToUpperInvariant()
                            });
                        }
                    }
                }
            }
        }

        // 3. Actualizar el estado del proceso de adopción a ESTANDARIZADO / CERRADO
        var estadoFinal = await dbContext.TechnologyAdoptionStates
            .FirstOrDefaultAsync(s => s.Nombre != null && (s.Nombre.Contains("ESTANDAR") || s.Nombre.Contains("IMPLEMENT")), cancellationToken);

        if (estadoFinal != null)
        {
            proceso.IdEstadoAdopcionTSI = estadoFinal.Id;
        }

        proceso.FechaEstimadaCierre = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordRelationAsync("FINALIZE_EVALUATION_WITH_STANDARD", "proceso-adopcion-tsi", "TProcesoAdopcionTSI",
            command.ProcesoId, techName, command.ActorUserId, command.CorrelationId,
            $"Evaluación finalizada y adjudicada a '{techName}'. Empresas alineadas: {alignedCompanyIds.Count}.", cancellationToken);

        return new AdoptionResult(true, $"La evaluación '{proceso.CodigoProceso}' ha sido finalizada y adjudicada exitosamente a la tecnología '{techName}'.", standardResult.EntityId);
    }

    private async Task EnsureCompanyCapabilitiesInitializedAsync(int buildingBlockId, int procesoAdopcionEmpresaId, CancellationToken cancellationToken)
    {
        var bbCaps = await dbContext.Capabilities.AsNoTracking()
            .Where(c => c.IdBuildingBlock == buildingBlockId)
            .ToListAsync(cancellationToken);

        if (bbCaps.Count == 0) return;

        var existingCaps = await dbContext.ProcessCompanyCapabilities
            .Where(p => p.IdProcesoAdopcionEmpresa == procesoAdopcionEmpresaId)
            .Select(p => p.IdCapacidad)
            .ToListAsync(cancellationToken);

        var missingCaps = bbCaps.Where(c => !existingCaps.Contains(c.Id)).ToList();
        if (missingCaps.Count == 0) return;

        foreach (var cap in missingCaps)
        {
            dbContext.ProcessCompanyCapabilities.Add(new TProcesoEmpresaCapacidad
            {
                IdProcesoAdopcionEmpresa = procesoAdopcionEmpresaId,
                IdCapacidad = cap.Id,
                EstadoCobertura = "NA",
                Comentario = null,
                FechaModificacion = DateTime.UtcNow,
                UsuarioModificacion = "SYSTEM"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdoptionResult> UpdateCompanyCapabilityStateAsync(
        int procesoId,
        int empresaId,
        int capacidadId,
        string estadoCodigo,
        string? comentario,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var procEmp = await dbContext.AdoptionProcessCompanies
            .FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId && p.IdEmpresaSubsidiaria == empresaId, cancellationToken);
        if (procEmp is null)
            return new AdoptionResult(false, "La empresa no se encuentra registrada en el proceso de adopción.");

        var capRow = await dbContext.ProcessCompanyCapabilities
            .FirstOrDefaultAsync(c => c.IdProcesoAdopcionEmpresa == procEmp.IdProcesoAdopcionEmpresa && c.IdCapacidad == capacidadId, cancellationToken);

        var validCodes = new[] { "A", "F", "NA" };
        var cleanCode = validCodes.Contains(estadoCodigo?.Trim().ToUpperInvariant()) ? estadoCodigo!.Trim().ToUpperInvariant() : "NA";

        if (capRow is null)
        {
            capRow = new TProcesoEmpresaCapacidad
            {
                IdProcesoAdopcionEmpresa = procEmp.IdProcesoAdopcionEmpresa,
                IdCapacidad = capacidadId,
                EstadoCobertura = cleanCode,
                Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim(),
                FechaModificacion = DateTime.UtcNow,
                UsuarioModificacion = actorUserId.ToString()
            };
            dbContext.ProcessCompanyCapabilities.Add(capRow);
        }
        else
        {
            capRow.EstadoCobertura = cleanCode;
            if (comentario is not null)
            {
                capRow.Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            }
            capRow.FechaModificacion = DateTime.UtcNow;
            capRow.UsuarioModificacion = actorUserId.ToString();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdoptionResult(true, "Estado de capacidad actualizado exitosamente.", capRow.IdProcesoEmpresaCapacidad);
    }

    public async Task<AdoptionResult> UpdateCompanyCapabilityCommentAsync(
        int procesoId,
        int empresaId,
        string? comentario,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var procEmp = await dbContext.AdoptionProcessCompanies
            .FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId && p.IdEmpresaSubsidiaria == empresaId, cancellationToken);
        if (procEmp is null)
            return new AdoptionResult(false, "La empresa no se encuentra registrada en el proceso de adopción.");

        procEmp.ComentarioCapacidades = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
        procEmp.FechaModificacion = DateTime.UtcNow;
        procEmp.UsuarioModificacion = actorUserId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AdoptionResult(true, "Comentario de subsidiaria actualizado exitosamente.", procEmp.IdProcesoAdopcionEmpresa);
    }

    public async Task<AdoptionResult> UpdateCompanyDriverVolumeAsync(
        int procesoId,
        int empresaId,
        string driverKey,
        decimal cantidad,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null) return new AdoptionResult(false, "El proceso de adopción no existe.");

        var procEmp = await dbContext.AdoptionProcessCompanies
            .FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId && p.IdEmpresaSubsidiaria == empresaId, cancellationToken);

        // Buscar tecnología primaria implementada de la empresa respetando la misma jerarquía de aislamiento de Detalles y Reportes:
        // 1. Proceso-Empresa vinculado
        // 2. Fallback: tecnología primaria o más reciente de la empresa en el building block
        TTecnologiaTSIimplementadaSubsidiaria? implTech = null;

        if (procEmp != null)
        {
            implTech = await dbContext.ImplementedTechnologies
                .FirstOrDefaultAsync(it => it.IdProcesoAdopcionEmpresa == procEmp.IdProcesoAdopcionEmpresa && it.EsTecnologiaPrimaria, cancellationToken);

            if (implTech is null)
            {
                implTech = await dbContext.ImplementedTechnologies
                    .FirstOrDefaultAsync(it => it.IdProcesoAdopcionEmpresa == procEmp.IdProcesoAdopcionEmpresa, cancellationToken);
            }
        }

        if (implTech is null)
        {
            implTech = await dbContext.ImplementedTechnologies
                .Where(it => it.IdEmpresaSubsidiaria == empresaId &&
                             (it.IdProcesoAdopcionEmpresa == null || it.IdProcesoAdopcionEmpresa == 0) &&
                             it.IdBuildingBlock == proceso.IdBuildingBlock)
                .OrderByDescending(it => it.EsTecnologiaPrimaria ? 1 : 0)
                .ThenByDescending(it => it.IdTecnologiaTSIimplementadaSubsidiaria)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var compDrivers = implTech is not null
            ? await dbContext.Drivers
                .Where(d => d.IdTecnologiaTSIimplementadaSubsidiaria == implTech.IdTecnologiaTSIimplementadaSubsidiaria)
                .ToListAsync(cancellationToken)
            : new List<TDriver>();

        TDriver? targetDriver = null;
        string targetDesc = "";
        string targetUnit = "Unidades";
        bool isAntibotOrWaf = false;

        switch (driverKey.Trim().ToLowerInvariant())
        {
            case "throughput":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().StartsWith("throughput"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Throughput (mensual)";
                targetUnit = targetDriver?.UnidadMedida ?? "Gbps";
                break;
            case "ancho_banda":
            case "anchobanda":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().StartsWith("ancho de banda"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Ancho de Banda (mensual)";
                targetUnit = targetDriver?.UnidadMedida ?? "Gbps";
                break;
            case "dominio":
            case "dominios":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().Equals("dominio", StringComparison.OrdinalIgnoreCase));
                targetDesc = targetDriver?.DescripcionDriver ?? "Dominio";
                targetUnit = targetDriver?.UnidadMedida ?? "Dominio";
                break;
            case "subdominio":
            case "subdominios":
            case "sub_dominios":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().Contains("sub dominio"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Sub Dominios";
                targetUnit = targetDriver?.UnidadMedida ?? "Sub Dominios";
                break;
            case "apps_fqdn":
            case "apps":
                targetDriver = compDrivers.FirstOrDefault(d => {
                    var s = (d.DescripcionDriver ?? "").Trim().ToLowerInvariant();
                    return (s.Contains("aplicaciones") || s.Contains("fqdn")) && !s.Contains("api security");
                });
                targetDesc = targetDriver?.DescripcionDriver ?? "Cantidad de aplicaciones (FQDN)";
                targetUnit = targetDriver?.UnidadMedida ?? "FQDN";
                break;
            case "apps_api_sec":
            case "api_security":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().Contains("api security"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Cantidad de Aplicaciones FQDN en API Security";
                targetUnit = targetDriver?.UnidadMedida ?? "FQDN";
                break;
            case "api_prot":
            case "api_protection":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().Contains("api protection"));
                targetDesc = targetDriver?.DescripcionDriver ?? "API Protection / API Security Request";
                targetUnit = targetDriver?.UnidadMedida ?? "M x Mes";
                break;
            case "antibot":
                targetDriver = compDrivers.FirstOrDefault(d => {
                    var s = (d.DescripcionDriver ?? "").Trim().ToLowerInvariant();
                    return s.Contains("antibot") && !s.Contains("waf");
                });
                targetDesc = targetDriver?.DescripcionDriver ?? "Millones de Request (mensual / antibot)";
                targetUnit = targetDriver?.UnidadMedida ?? "Millones";
                isAntibotOrWaf = true;
                break;
            case "waf":
                targetDriver = compDrivers.FirstOrDefault(d => {
                    var s = (d.DescripcionDriver ?? "").Trim().ToLowerInvariant();
                    return s.Contains("waf") && !s.Contains("antibot");
                });
                targetDesc = targetDriver?.DescripcionDriver ?? "Millones Request WAF (mensual)";
                targetUnit = targetDriver?.UnidadMedida ?? "Millones";
                isAntibotOrWaf = true;
                break;
            case "data_transfer":
            case "datatransfer":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().Contains("transfer"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Data Transfer (mensual)";
                targetUnit = targetDriver?.UnidadMedida ?? "TB";
                break;
            case "request_size":
            case "req_size":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().StartsWith("request size"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Request Size (mensual)";
                targetUnit = targetDriver?.UnidadMedida ?? "KB / GB";
                break;
            case "response_size":
            case "resp_size":
                targetDriver = compDrivers.FirstOrDefault(d => (d.DescripcionDriver ?? "").Trim().ToLowerInvariant().StartsWith("response size"));
                targetDesc = targetDriver?.DescripcionDriver ?? "Response Size (mensual)";
                targetUnit = targetDriver?.UnidadMedida ?? "KB / GB / TB";
                break;
            default:
                // El driverKey puede venir en formato "Descripcion / Unidad" desde la matriz de reportería.
                // Primero intentar coincidencia exacta por descripción completa.
                var trimmedKey = driverKey.Trim();
                string? parsedDesc = null;
                string? parsedUnit = null;

                // Separar descripción y unidad si el key tiene formato "Desc / Unidad"
                var slashIdx = trimmedKey.LastIndexOf(" / ", StringComparison.Ordinal);
                if (slashIdx > 0 && slashIdx < trimmedKey.Length - 3)
                {
                    parsedDesc = trimmedKey[..slashIdx].Trim();
                    parsedUnit = trimmedKey[(slashIdx + 3)..].Trim();
                }

                // 1. Buscar por descripción exacta (sin la unidad)
                if (parsedDesc is not null)
                {
                    targetDriver = compDrivers.FirstOrDefault(d =>
                        (d.DescripcionDriver ?? "").Trim().Equals(parsedDesc, StringComparison.OrdinalIgnoreCase));
                }

                // 2. Fallback: buscar por el key completo como descripción
                targetDriver ??= compDrivers.FirstOrDefault(d =>
                    (d.DescripcionDriver ?? "").Trim().Equals(trimmedKey, StringComparison.OrdinalIgnoreCase));

                targetDesc = targetDriver?.DescripcionDriver ?? parsedDesc ?? trimmedKey;
                targetUnit = targetDriver?.UnidadMedida ?? parsedUnit ?? "Unidades";
                break;
        }

        if (implTech is not null)
        {
            if (targetDriver is null)
            {
                targetDriver = new TDriver
                {
                    IdTecnologiaTSIimplementadaSubsidiaria = implTech.IdTecnologiaTSIimplementadaSubsidiaria,
                    DescripcionDriver = targetDesc,
                    UnidadMedida = targetUnit,
                    Cantidad = cantidad,
                    PrecioUnitario = 0m,
                    Moneda = "USD"
                };
                dbContext.Drivers.Add(targetDriver);
            }
            else
            {
                targetDriver.Cantidad = cantidad;
            }

            // Si se actualizó antibot o waf, mantener actualizado también el driver combinado si existe
            if (isAntibotOrWaf)
            {
                var combinedDriver = compDrivers.FirstOrDefault(d => {
                    var s = (d.DescripcionDriver ?? "").Trim().ToLowerInvariant();
                    return s.Contains("antibot") && s.Contains("waf");
                });

                if (combinedDriver != null)
                {
                    decimal currentAntibot = 0m;
                    decimal currentWaf = 0m;

                    if (driverKey.Trim().ToLowerInvariant() == "antibot")
                    {
                        currentAntibot = cantidad;
                        var wafDrv = compDrivers.FirstOrDefault(d => {
                            var s = (d.DescripcionDriver ?? "").Trim().ToLowerInvariant();
                            return s.Contains("waf") && !s.Contains("antibot");
                        });
                        currentWaf = wafDrv?.Cantidad ?? 0m;
                    }
                    else
                    {
                        currentWaf = cantidad;
                        var abDrv = compDrivers.FirstOrDefault(d => {
                            var s = (d.DescripcionDriver ?? "").Trim().ToLowerInvariant();
                            return s.Contains("antibot") && !s.Contains("waf");
                        });
                        currentAntibot = abDrv?.Cantidad ?? 0m;
                    }

                    combinedDriver.Cantidad = currentAntibot + currentWaf;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Sincronizar en el nuevo modelo TDriverEmpresa si la empresa está convocada
        TDriverEmpresa? de = null;
        if (procEmp is not null && !string.IsNullOrWhiteSpace(targetDesc))
        {
            var cleanDesc = targetDesc.Trim();
            var cleanUnit = targetUnit.Trim();
            var dp = await dbContext.ProcessDrivers.FirstOrDefaultAsync(p => 
                p.IdProcesoAdopcionTSI == procesoId && 
                p.DescripcionDriver.ToLower() == cleanDesc.ToLower(), cancellationToken);

            if (dp is null)
            {
                dp = new TDriverProceso
                {
                    IdProcesoAdopcionTSI = procesoId,
                    DescripcionDriver = cleanDesc,
                    UnidadMedida = cleanUnit,
                    OrdenVisual = 999,
                    FechaCreacion = DateTime.UtcNow
                };
                dbContext.ProcessDrivers.Add(dp);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            de = await dbContext.CompanyDrivers.FirstOrDefaultAsync(cd => 
                cd.IdProcesoAdopcionEmpresa == procEmp.IdProcesoAdopcionEmpresa && 
                cd.IdDriverProceso == dp.IdDriverProceso, cancellationToken);

            var unitPrice = targetDriver?.PrecioUnitario ?? de?.PrecioUnitario ?? 0m;
            var moneda = !string.IsNullOrWhiteSpace(targetDriver?.Moneda)
                ? targetDriver.Moneda
                : (!string.IsNullOrWhiteSpace(de?.Moneda) ? de.Moneda : "USD");

            if (de is null)
            {
                de = new TDriverEmpresa
                {
                    IdProcesoAdopcionEmpresa = procEmp.IdProcesoAdopcionEmpresa,
                    IdDriverProceso = dp.IdDriverProceso,
                    Cantidad = cantidad,
                    PrecioUnitario = unitPrice,
                    CostoTotal = cantidad * unitPrice,
                    Moneda = moneda,
                    FechaModificacion = DateTime.UtcNow,
                    UsuarioModificacion = "REPORTERIA"
                };
                dbContext.CompanyDrivers.Add(de);
            }
            else
            {
                de.Cantidad = cantidad;
                de.CostoTotal = cantidad * (de.PrecioUnitario ?? 0m);
                de.FechaModificacion = DateTime.UtcNow;
                de.UsuarioModificacion = "REPORTERIA";
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AdoptionResult(true, "Driver de volumetría actualizado exitosamente.", targetDriver?.IdDriver ?? de?.IdDriverEmpresa ?? 0);
    }

    public async Task<AdoptionResult> SaveVencimientoReportDriversAsync(
        int procesoId,
        IEnumerable<string> drivers,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null)
        {
            return new AdoptionResult(false, "El proceso de adopción no existe.");
        }

        var validDrivers = drivers?
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        proceso.DriversReporteVencimiento = System.Text.Json.JsonSerializer.Serialize(validDrivers);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-adopcion-tsi", "TProcesoAdopcionTSI", procesoId, proceso.NombreProceso,
            actorUserId, correlationId, $"Se actualizaron los drivers proyectados del proceso {procesoId}: {string.Join(", ", validDrivers)}", cancellationToken);

        return new AdoptionResult(true, "Preferencias de drivers guardadas en base de datos.");
    }

    public async Task<AdoptionResult> UpdateCompanyOrderAsync(
        int procesoId,
        IEnumerable<int> orderedCompanyIds,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso is null)
        {
            return new AdoptionResult(false, "El proceso de evaluación no existe.");
        }

        var convocadas = await dbContext.AdoptionProcessCompanies
            .Where(c => c.IdProcesoAdopcionTSI == procesoId && c.Aplica)
            .Select(c => c.IdEmpresaSubsidiaria)
            .ToListAsync(cancellationToken);

        var convocadasSet = new HashSet<int>(convocadas);

        var cleanList = (orderedCompanyIds ?? [])
            .Where(id => id > 0 && convocadasSet.Contains(id))
            .Distinct()
            .ToList();

        // Si faltan algunas empresas convocadas en la lista enviada, agregarlas al final
        foreach (var id in convocadas)
        {
            if (!cleanList.Contains(id))
            {
                cleanList.Add(id);
            }
        }

        proceso.OrdenEmpresas = System.Text.Json.JsonSerializer.Serialize(cleanList);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AdoptionResult(true, "Orden de empresas guardado exitosamente en base de datos.");
    }

    public async Task<AdoptionResult> SaveProcessDriverAsync(SaveProcessDriverCommand command, CancellationToken cancellationToken = default)
    {
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == command.ProcesoId, cancellationToken);
        if (proceso is null)
        {
            return new AdoptionResult(false, "El proceso de adopción no existe.");
        }

        var cleanDesc = command.Descripcion.Trim();
        var cleanUnit = string.IsNullOrWhiteSpace(command.UnidadMedida) ? "Unidades" : command.UnidadMedida.Trim();

        TDriverProceso? template;
        if (command.DriverProcesoId.HasValue && command.DriverProcesoId.Value > 0)
        {
            template = await dbContext.ProcessDrivers.FirstOrDefaultAsync(d => d.IdDriverProceso == command.DriverProcesoId.Value && d.IdProcesoAdopcionTSI == command.ProcesoId, cancellationToken);
            if (template is null)
            {
                return new AdoptionResult(false, "El driver del proceso no fue encontrado.");
            }
            template.DescripcionDriver = cleanDesc;
            template.UnidadMedida = cleanUnit;
        }
        else
        {
            // Validar unicidad de descripción dentro del proceso
            var exists = await dbContext.ProcessDrivers.AnyAsync(d => 
                d.IdProcesoAdopcionTSI == command.ProcesoId && 
                d.DescripcionDriver.ToLower() == cleanDesc.ToLower(), cancellationToken);

            if (exists)
            {
                return new AdoptionResult(false, $"Ya existe un driver con la descripción '{cleanDesc}' en este proceso.");
            }

            var maxOrder = await dbContext.ProcessDrivers
                .Where(d => d.IdProcesoAdopcionTSI == command.ProcesoId)
                .Select(d => d.OrdenVisual)
                .MaxAsync(cancellationToken) ?? 0;

            template = new TDriverProceso
            {
                IdProcesoAdopcionTSI = command.ProcesoId,
                DescripcionDriver = cleanDesc,
                UnidadMedida = cleanUnit,
                OrdenVisual = maxOrder + 1,
                FechaCreacion = DateTime.UtcNow
            };
            dbContext.ProcessDrivers.Add(template);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // CLONACIÓN / REPLICACIÓN: Asegurar que todas las empresas convocadas en este proceso tengan su fila en TDriverEmpresa
        var convocadas = await dbContext.AdoptionProcessCompanies
            .Where(c => c.IdProcesoAdopcionTSI == command.ProcesoId)
            .ToListAsync(cancellationToken);

        foreach (var conv in convocadas)
        {
            var alreadyHas = await dbContext.CompanyDrivers.AnyAsync(cd => 
                cd.IdProcesoAdopcionEmpresa == conv.IdProcesoAdopcionEmpresa && 
                cd.IdDriverProceso == template.IdDriverProceso, cancellationToken);

            if (!alreadyHas)
            {
                dbContext.CompanyDrivers.Add(new TDriverEmpresa
                {
                    IdProcesoAdopcionEmpresa = conv.IdProcesoAdopcionEmpresa,
                    IdDriverProceso = template.IdDriverProceso,
                    Cantidad = 0.00m,
                    PrecioUnitario = 0.00m,
                    CostoTotal = 0.00m,
                    Moneda = "USD",
                    FechaModificacion = DateTime.UtcNow,
                    UsuarioModificacion = "AUTO_CLONE"
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-adopcion-driver", "TDriverProceso", template.IdDriverProceso, template.DescripcionDriver,
            command.ActorUserId, command.CorrelationId, $"Driver '{cleanDesc}' ({cleanUnit}) guardado y replicado en {convocadas.Count} empresas.", cancellationToken);

        return new AdoptionResult(true, $"Driver '{cleanDesc}' guardado y replicado exitosamente en las empresas convocadas.", template.IdDriverProceso);
    }

    public async Task<AdoptionResult> DeleteProcessDriverAsync(int procesoId, int driverProcesoId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.ProcessDrivers.FirstOrDefaultAsync(d => d.IdDriverProceso == driverProcesoId && d.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (template is null)
        {
            return new AdoptionResult(false, "El driver del proceso no existe.");
        }

        var deletedDesc = template.DescripcionDriver?.Trim() ?? string.Empty;

        // Eliminar valores por empresa correspondientes
        var companyDriverRows = await dbContext.CompanyDrivers
            .Where(cd => cd.IdDriverProceso == driverProcesoId)
            .ToListAsync(cancellationToken);

        dbContext.CompanyDrivers.RemoveRange(companyDriverRows);
        dbContext.ProcessDrivers.Remove(template);

        // Limpiar en DriversReporteVencimiento de TProcesoAdopcionTSI si estaba seleccionado
        var proceso = await dbContext.AdoptionProcesses.FirstOrDefaultAsync(p => p.IdProcesoAdopcionTSI == procesoId, cancellationToken);
        if (proceso != null && !string.IsNullOrWhiteSpace(proceso.DriversReporteVencimiento) && !string.IsNullOrWhiteSpace(deletedDesc))
        {
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(proceso.DriversReporteVencimiento);
                if (list != null)
                {
                    var countBefore = list.Count;
                    list.RemoveAll(x => string.Equals(x.Trim(), deletedDesc, StringComparison.OrdinalIgnoreCase));
                    if (list.Count != countBefore)
                    {
                        proceso.DriversReporteVencimiento = System.Text.Json.JsonSerializer.Serialize(list);
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        // Limpiar en la tabla legacy TDriver para las empresas de este proceso si existiera un registro homólogo
        if (!string.IsNullOrWhiteSpace(deletedDesc))
        {
            var procEmpIds = await dbContext.AdoptionProcessCompanies
                .Where(c => c.IdProcesoAdopcionTSI == procesoId)
                .Select(c => c.IdProcesoAdopcionEmpresa)
                .ToListAsync(cancellationToken);

            var techIds = await dbContext.ImplementedTechnologies
                .Where(it => procEmpIds.Contains(it.IdProcesoAdopcionEmpresa ?? 0))
                .Select(it => it.IdTecnologiaTSIimplementadaSubsidiaria)
                .ToListAsync(cancellationToken);

            if (techIds.Count > 0)
            {
                var legacyRows = await dbContext.Drivers
                    .Where(d => techIds.Contains(d.IdTecnologiaTSIimplementadaSubsidiaria) &&
                                d.DescripcionDriver != null &&
                                (d.DescripcionDriver.ToLower() == deletedDesc.ToLower() ||
                                 d.DescripcionDriver.ToLower().StartsWith(deletedDesc.ToLower())))
                    .ToListAsync(cancellationToken);

                if (legacyRows.Count > 0)
                {
                    dbContext.Drivers.RemoveRange(legacyRows);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordRelationAsync("DELETE_PROCESS_DRIVER", "proceso-adopcion-driver", "TDriverProceso",
            driverProcesoId, null, actorUserId, correlationId, $"Driver '{template.DescripcionDriver}' eliminado de la plantilla del proceso y de sus empresas asociadas.", cancellationToken);

        return new AdoptionResult(true, $"El driver '{template.DescripcionDriver}' fue eliminado exitosamente del proceso.");
    }

    public async Task<AdoptionResult> UpdateCompanyDriverValueAsync(UpdateCompanyDriverValueCommand command, CancellationToken cancellationToken = default)
    {
        var procEmp = await dbContext.AdoptionProcessCompanies.FirstOrDefaultAsync(p => p.IdProcesoAdopcionEmpresa == command.ProcesoEmpresaId, cancellationToken);
        if (procEmp is null)
        {
            return new AdoptionResult(false, "La empresa participante no fue encontrada en este proceso.");
        }

        var template = await dbContext.ProcessDrivers.FirstOrDefaultAsync(d => d.IdDriverProceso == command.DriverProcesoId, cancellationToken);
        if (template is null)
        {
            return new AdoptionResult(false, "El driver especificado no existe en la plantilla del proceso.");
        }

        var de = await dbContext.CompanyDrivers.FirstOrDefaultAsync(cd => 
            cd.IdProcesoAdopcionEmpresa == command.ProcesoEmpresaId && 
            cd.IdDriverProceso == command.DriverProcesoId, cancellationToken);

        var subtotal = command.Cantidad * command.PrecioUnitario;
        var moneda = string.IsNullOrWhiteSpace(command.Moneda) ? "USD" : command.Moneda.Trim();

        if (de is null)
        {
            de = new TDriverEmpresa
            {
                IdProcesoAdopcionEmpresa = command.ProcesoEmpresaId,
                IdDriverProceso = command.DriverProcesoId,
                Cantidad = command.Cantidad,
                PrecioUnitario = command.PrecioUnitario,
                CostoTotal = subtotal,
                Moneda = moneda,
                FechaModificacion = DateTime.UtcNow,
                UsuarioModificacion = "MANUAL_EDIT"
            };
            dbContext.CompanyDrivers.Add(de);
        }
        else
        {
            de.Cantidad = command.Cantidad;
            de.PrecioUnitario = command.PrecioUnitario;
            de.CostoTotal = subtotal;
            de.Moneda = moneda;
            de.FechaModificacion = DateTime.UtcNow;
            de.UsuarioModificacion = "MANUAL_EDIT";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("driver-empresa", "TDriverEmpresa", de.IdDriverEmpresa, template.DescripcionDriver,
            command.ActorUserId, command.CorrelationId, $"Valores actualizados para driver '{template.DescripcionDriver}': Cant={command.Cantidad}, PU={command.PrecioUnitario}, Total={subtotal} {moneda}.", cancellationToken);

        return new AdoptionResult(true, "Valores del driver actualizados correctamente.", de.IdDriverEmpresa);
    }

    private static List<int> ParseOrderedCompanyIds(string? ordenEmpresasJson)
    {
        if (string.IsNullOrWhiteSpace(ordenEmpresasJson)) return [];
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<int>>(ordenEmpresasJson);
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }
}