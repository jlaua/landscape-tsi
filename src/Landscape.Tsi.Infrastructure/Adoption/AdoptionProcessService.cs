using System.Data;
using System.Data.Common;

using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Adoption;

public sealed class AdoptionProcessService(
    CatalogDbContext dbContext,
    IAuditTrailService auditTrail) : IAdoptionProcessService
{
    public async Task<IReadOnlyList<AdoptionProcessSummaryDto>> ListProcessesAsync(CancellationToken cancellationToken = default)
    {
        var processes = await dbContext.AdoptionProcesses.AsNoTracking().ToListAsync(cancellationToken);
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
        var familia = bb?.IdFamilia is not null ? await dbContext.Families.AsNoTracking().FirstOrDefaultAsync(f => f.Id == bb.IdFamilia, cancellationToken) : null;
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

        // Empresas convocadas
        var companyProcesses = await dbContext.AdoptionProcessCompanies.AsNoTracking()
            .Where(cp => cp.IdProcesoAdopcionTSI == p.IdProcesoAdopcionTSI)
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

        var companyRows = new List<CompanyAdoptionRowDto>();
        foreach (var cp in companyProcesses)
        {
            var comp = allCompanies.GetValueOrDefault(cp.IdEmpresaSubsidiaria);
            (string Nombre, string? Email)? focal = null;
            if (cp.IdContactoEmpresaSubsidiaria.HasValue && allCompanyContacts.TryGetValue(cp.IdContactoEmpresaSubsidiaria.Value, out var contact))
            {
                focal = contact;
            }

            var compImpls = implemented.Where(it => it.IdEmpresaSubsidiaria == cp.IdEmpresaSubsidiaria).ToList();
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
                        .Select(ac => new ContractDto(ac.IdContratoTecnologia, ac.IdTecnologiaTSIimplementadaSubsidiaria, ac.NumeroContrato, ac.EsAdenda, ac.IdContratoPadre, rc.NumeroContrato, ac.FechaInicio, ac.FechaFin, ac.FechaAdjudicacion, ac.RutaDocumentoContrato, ac.MontoContratado, ac.Moneda, ac.Observaciones, []))
                        .ToList();

                    return new ContractDto(rc.IdContratoTecnologia, rc.IdTecnologiaTSIimplementadaSubsidiaria, rc.NumeroContrato, rc.EsAdenda, rc.IdContratoPadre, null, rc.FechaInicio, rc.FechaFin, rc.FechaAdjudicacion, rc.RutaDocumentoContrato, rc.MontoContratado, rc.Moneda, rc.Observaciones, adendas);
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
                    compDrivers));
            }

            var globalAlign = ComputeGlobalAlignment(cp.Aplica, compImplDtos, principalVigente, alternativos);

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
                globalAlign));
        }

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
            companyRows);
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

        var stateExists = await dbContext.TechnologyAdoptionStates.AnyAsync(s => s.Id == nuevoEstadoId, cancellationToken);
        if (!stateExists)
            return new AdoptionResult(false, "El estado de adopción especificado no existe.");

        proceso.IdEstadoAdopcionTSI = nuevoEstadoId;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordUpdateAsync("proceso-adopcion-tsi", "TProcesoAdopcionTSI", proceso.IdProcesoAdopcionTSI,
            proceso.NombreProceso, actorUserId, correlationId, $"Estado actualizado a {nuevoEstadoId}", cancellationToken);

        return new AdoptionResult(true, "Estado de proceso de adopción actualizado exitosamente.");
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
            VersionDesplegada = command.VersionDesplegada
        };

        dbContext.ImplementedTechnologies.Add(tech);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.RecordCreateAsync("tecnologia-implementada", "TTecnologiaTSIimplementadaSubsidiaria",
            tech.IdTecnologiaTSIimplementadaSubsidiaria, $"Empresa {command.EmpresaId} - Tech {command.TecnologiaId}",
            command.ActorUserId, command.CorrelationId, null, affectedRecordCount: 1, cancellationToken);

        return new AdoptionResult(true, "Tecnología implementada registrada exitosamente.", tech.IdTecnologiaTSIimplementadaSubsidiaria);
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

        var contrato = new TContratoTecnologia
        {
            IdTecnologiaTSIimplementadaSubsidiaria = command.TecnologiaImplementadaId,
            NumeroContrato = command.NumeroContrato.Trim(),
            EsAdenda = command.EsAdenda,
            IdContratoPadre = command.ContratoPadreId,
            FechaInicio = command.FechaInicio,
            FechaFin = command.FechaFin,
            FechaAdjudicacion = command.FechaAdjudicacion,
            RutaDocumentoContrato = command.RutaDocumento,
            MontoContratado = command.Monto,
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
                SET idTipoModeloOperacion = @tipoId, idModalidadLaboral = @modalidadId
                WHERE idModeloOperacion = @id;";
            var p1 = updateCmd.CreateParameter(); p1.ParameterName = "@tipoId"; p1.Value = command.TipoOperacionId; updateCmd.Parameters.Add(p1);
            var p2 = updateCmd.CreateParameter(); p2.ParameterName = "@modalidadId"; p2.Value = command.ModalidadLaboralId; updateCmd.Parameters.Add(p2);
            var p3 = updateCmd.CreateParameter(); p3.ParameterName = "@id"; p3.Value = existingId; updateCmd.Parameters.Add(p3);
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"INSERT INTO dbo.TModeloDeOperacion (idTecnologiaTSIimplementadaSubsidiaria, idTipoModeloOperacion, idModalidadLaboral)
                VALUES (@implId, @tipoId, @modalidadId);";
            var ip1 = insertCmd.CreateParameter(); ip1.ParameterName = "@implId"; ip1.Value = command.TecnologiaImplementadaId; insertCmd.Parameters.Add(ip1);
            var ip2 = insertCmd.CreateParameter(); ip2.ParameterName = "@tipoId"; ip2.Value = command.TipoOperacionId; insertCmd.Parameters.Add(ip2);
            var ip3 = insertCmd.CreateParameter(); ip3.ParameterName = "@modalidadId"; ip3.Value = command.ModalidadLaboralId; insertCmd.Parameters.Add(ip3);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await auditTrail.RecordRelationAsync("SAVE_OPERATION_MODEL", "modelo-operacion", "TModeloDeOperacion",
            command.TecnologiaImplementadaId, $"TipoOperacion: {command.TipoOperacionId}", command.ActorUserId, command.CorrelationId, null, cancellationToken);

        return new AdoptionResult(true, "Modelo de operación guardado exitosamente.");
    }

    private async Task<(string? VendorName, string? VendorContact, string? PartnerContact)> GetVendorDetailsAsync(int tecnologiaId, CancellationToken cancellationToken)
    {
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
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT m.idModeloOperacion, m.idTecnologiaTSIimplementadaSubsidiaria,
                   m.idTipoModeloOperacion, t.TipoModeloDeOperacion,
                   m.idModalidadLaboral, l.TipoModalidadLaboral
            FROM dbo.TModeloDeOperacion m
            LEFT JOIN dbo.TTipoOperacion t ON t.idTipoModeloOperacion = m.idTipoModeloOperacion
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
}