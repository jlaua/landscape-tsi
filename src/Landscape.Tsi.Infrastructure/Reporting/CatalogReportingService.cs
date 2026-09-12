using System.Data;
using System.Data.Common;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Reporting;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Reporting;

public sealed class CatalogReportingService(
    ICatalogManagementService catalogs,
    IdentityDbContext? dbContext = null,
    CatalogDbContext? catalogDbContext = null) : IReportingService
{
    public async Task<IReadOnlyList<CatalogReportPoint>> GetCatalogTotalsAsync(string? group, CancellationToken cancellationToken = default)
    {
        var definitions = MasterCatalogRegistry.ReportingCatalogs
            .Where(definition => definition.Enabled && (string.IsNullOrWhiteSpace(group) || string.Equals(definition.Group, group, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var result = new List<CatalogReportPoint>(definitions.Length);
        foreach (var definition in definitions)
        {
            var page = await catalogs.ListAsync(definition, null, 1, 1, cancellationToken);
            result.Add(new CatalogReportPoint(definition.Code, definition.Name, definition.Group, page.TotalCount));
        }

        return result;
    }

    public async Task<CatalogReportDetail> GetCatalogDetailAsync(string catalogCode, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null)
    {
        var definition = GetDefinition(catalogCode);
        return ToDetail(definition, await catalogs.ListAsync(definition, search, page, pageSize, cancellationToken, sortColumn, sortDirection));
    }

    public async Task<IReadOnlyList<CatalogReportRelation>> GetCatalogRelationsAsync(string catalogCode, CancellationToken cancellationToken = default)
    {
        var parent = GetDefinition(catalogCode);
        var result = new List<CatalogReportRelation>();
        foreach (var relation in MasterCatalogRegistry.ReportingRelations.Where(item => item.ParentCatalogCode == parent.Code && item.Type == "Uno a muchos"))
        {
            var child = GetDefinition(relation.ChildCatalogCode);
            var foreignKey = child.Columns.SingleOrDefault(column => column.Type == CatalogFieldType.ForeignKey && column.ReferenceCatalogCode == parent.Code);
            if (foreignKey is null) continue;
            var buckets = await catalogs.GetRelationCountsAsync(parent, child, foreignKey, cancellationToken);
            result.Add(new CatalogReportRelation(child.Code, child.Name, relation.Type,
                buckets.Select(bucket => new CatalogReportRelationBucket(bucket.ParentId, bucket.ParentName, bucket.Total)).ToArray()));
        }
        return result;
    }

    public async Task<CatalogReportDetail> GetRelatedCatalogDetailAsync(string parentCode, string childCode, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var parent = GetDefinition(parentCode);
        var child = GetDefinition(childCode);
        if (await catalogs.GetAsync(parent, parentId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("El registro padre no existe.");
        }
        var relation = MasterCatalogRegistry.ReportingRelations.SingleOrDefault(item => item.ParentCatalogCode == parent.Code && item.ChildCatalogCode == child.Code && item.Type == "Uno a muchos")
            ?? throw new InvalidOperationException("La relación solicitada no está disponible.");
        var foreignKey = child.Columns.SingleOrDefault(column => column.Type == CatalogFieldType.ForeignKey && column.ReferenceCatalogCode == parent.Code)
            ?? throw new InvalidOperationException("La relación no tiene una FK registrada.");
        return ToDetail(child, await catalogs.ListRelatedAsync(child, foreignKey, parentId, search, page, pageSize, cancellationToken));
    }

    public async Task<IReadOnlyList<CatalogContextKpi>> GetCatalogContextKpisAsync(string catalogCode, int recordId, CancellationToken cancellationToken = default)
    {
        var parent = GetDefinition(catalogCode);
        if (await catalogs.GetAsync(parent, recordId, cancellationToken) is null)
        {
            throw new KeyNotFoundException("El registro seleccionado no existe.");
        }
        var kpis = new List<CatalogContextKpi>();
        foreach (var relation in MasterCatalogRegistry.ReportingRelations.Where(item => item.ParentCatalogCode == parent.Code && item.Type == "Uno a muchos"))
        {
            if (!string.Equals(relation.ChildCatalogCode, "building-block", StringComparison.Ordinal)) continue;
            var child = GetDefinition(relation.ChildCatalogCode);
            var foreignKey = child.Columns.SingleOrDefault(column => column.Type == CatalogFieldType.ForeignKey && column.ReferenceCatalogCode == parent.Code);
            if (foreignKey is null) continue;
            kpis.Add(new CatalogContextKpi(child.Code, child.Name, await catalogs.GetRelatedCountAsync(child, foreignKey, recordId, cancellationToken), relation.EvidenceObject));
        }
        return kpis;
    }

    public async Task<CompanyCisoReport> GetCompanyCisoReportAsync(CompanyCisoReportQuery query, CancellationToken cancellationToken = default)
    {
        if (dbContext is null) throw new InvalidOperationException("El reporte Empresas y CISO requiere un contexto de datos.");
        query = query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };
        var connection = dbContext.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            var kpis = await ReadKpisAsync(connection, query, cancellationToken);
            var totalRows = await ReadCountAsync(connection, query, cancellationToken);
            var rows = await ReadRowsAsync(connection, query, cancellationToken);
            var options = await ReadOptionsAsync(connection, cancellationToken);
            return new CompanyCisoReport(query, kpis, rows, totalRows, options.Companies, options.Countries, options.Groupings, options.Industries, options.CisoNames);
        }
        finally
        {
            if (close) await connection.CloseAsync();
        }
    }

    public async Task<CompanyAdoptionReport> GetCompanyAdoptionReportAsync(CompanyAdoptionReportQuery query, CancellationToken cancellationToken = default)
    {
        if (catalogDbContext is null) throw new InvalidOperationException("El reporte de adopción de empresas por tecnología requiere un contexto de datos.");
        query = query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) };

        var domains = await catalogDbContext.Domains.AsNoTracking().ToListAsync(cancellationToken);
        var buildingBlocks = await catalogDbContext.BuildingBlocks.AsNoTracking().ToListAsync(cancellationToken);
        var companies = await catalogDbContext.Companies.AsNoTracking().ToListAsync(cancellationToken);
        var technologies = await catalogDbContext.Technologies.AsNoTracking().ToListAsync(cancellationToken);
        var standardHistories = await catalogDbContext.StandardTechnologyHistories.AsNoTracking().ToListAsync(cancellationToken);
        var processes = await catalogDbContext.AdoptionProcesses.AsNoTracking().ToListAsync(cancellationToken);
        var adoptionCompanies = await catalogDbContext.AdoptionProcessCompanies.AsNoTracking().ToListAsync(cancellationToken);
        var implemented = await catalogDbContext.ImplementedTechnologies.AsNoTracking().ToListAsync(cancellationToken);
        var contracts = await catalogDbContext.TechnologyContracts.AsNoTracking().ToListAsync(cancellationToken);

        var vendorDict = new Dictionary<int, string>();
        if (catalogDbContext.Database.ProviderName?.Contains("InMemory", StringComparison.Ordinal) != true)
        {
            try
            {
                var conn = catalogDbContext.Database.GetDbConnection();
                var shouldClose = conn.State != ConnectionState.Open;
                if (shouldClose) await conn.OpenAsync(cancellationToken);
                try
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT idTecnologiaTSI, nombreVendor FROM dbo.TVendor WHERE idTecnologiaTSI IS NOT NULL;";
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        if (!reader.IsDBNull(0) && !reader.IsDBNull(1))
                        {
                            vendorDict[reader.GetInt32(0)] = reader.GetString(1);
                        }
                    }
                }
                finally
                {
                    if (shouldClose) await conn.CloseAsync();
                }
            }
            catch
            {
                // Degradar silenciosamente si la tabla opcional TVendor no existe en el entorno
            }
        }

        // 1. Métricas de Adopción por Dominio y Building Block
        var domainMetrics = new List<DomainAdoptionMetric>();
        foreach (var d in domains.OrderBy(x => x.Dominio))
        {
            var dBuildingBlocks = buildingBlocks.Where(b => b.IdDominio == d.Id).OrderBy(b => b.Nombre).ToList();
            var bbMetrics = new List<BuildingBlockAdoptionMetric>();
            var domainCompanyIdsWithAdoption = new HashSet<int>();

            foreach (var bb in dBuildingBlocks)
            {
                var bbStandards = standardHistories.Where(s => s.IdBuildingBlock == bb.Id).ToList();
                var principalStandard = bbStandards.FirstOrDefault(s => s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE");
                var corporateStandardName = principalStandard is not null
                    ? technologies.FirstOrDefault(t => t.Id == principalStandard.IdTecnologiaTSI)?.NombreCorporativo
                    : null;

                var bbProcesses = processes.Where(p => p.IdBuildingBlock == bb.Id).Select(p => p.IdProcesoAdopcionTSI).ToHashSet();
                var bbConvocadas = adoptionCompanies.Where(c => bbProcesses.Contains(c.IdProcesoAdopcionTSI)).ToList();
                var convocadasCount = bbConvocadas.Count;

                var bbImpl = implemented.Where(i => i.IdBuildingBlock == bb.Id || bbConvocadas.Any(c => c.IdProcesoAdopcionEmpresa == i.IdProcesoAdopcionEmpresa)).ToList();
                var implCompanyIds = bbImpl.Select(i => i.IdEmpresaSubsidiaria).Distinct().ToList();
                foreach (var cId in implCompanyIds)
                {
                    domainCompanyIdsWithAdoption.Add(cId);
                }

                var empresasNombres = companies
                    .Where(c => implCompanyIds.Contains(c.Id))
                    .Select(c => c.Nombre ?? $"Empresa {c.Id}")
                    .OrderBy(n => n)
                    .ToList();

                bbMetrics.Add(new BuildingBlockAdoptionMetric(
                    bb.Id,
                    bb.Nombre ?? $"Building Block {bb.Id}",
                    d.Id,
                    d.Dominio ?? $"Dominio {d.Id}",
                    convocadasCount,
                    implCompanyIds.Count,
                    empresasNombres,
                    corporateStandardName));
            }

            domainMetrics.Add(new DomainAdoptionMetric(
                d.Id,
                d.Dominio ?? $"Dominio {d.Id}",
                dBuildingBlocks.Count,
                domainCompanyIdsWithAdoption.Count,
                bbMetrics));
        }

        // 2. Alineación de Empresas vs Estándar Tecnológico TSI
        var pairKeys = new HashSet<(int CompanyId, int BuildingBlockId)>();

        foreach (var ac in adoptionCompanies)
        {
            var p = processes.FirstOrDefault(x => x.IdProcesoAdopcionTSI == ac.IdProcesoAdopcionTSI);
            if (p is not null)
            {
                pairKeys.Add((ac.IdEmpresaSubsidiaria, p.IdBuildingBlock));
            }
        }

        foreach (var it in implemented)
        {
            if (it.IdBuildingBlock.HasValue)
            {
                pairKeys.Add((it.IdEmpresaSubsidiaria, it.IdBuildingBlock.Value));
            }
            else if (it.IdProcesoAdopcionEmpresa.HasValue)
            {
                var ac = adoptionCompanies.FirstOrDefault(x => x.IdProcesoAdopcionEmpresa == it.IdProcesoAdopcionEmpresa.Value);
                if (ac is not null)
                {
                    var p = processes.FirstOrDefault(x => x.IdProcesoAdopcionTSI == ac.IdProcesoAdopcionTSI);
                    if (p is not null)
                    {
                        pairKeys.Add((it.IdEmpresaSubsidiaria, p.IdBuildingBlock));
                    }
                }
            }
        }

        var allAlignmentRows = new List<CompanyTechnologyAlignmentRow>();

        foreach (var (companyId, buildingBlockId) in pairKeys)
        {
            var company = companies.FirstOrDefault(c => c.Id == companyId);
            var bb = buildingBlocks.FirstOrDefault(b => b.Id == buildingBlockId);
            var domain = bb?.IdDominio is not null ? domains.FirstOrDefault(d => d.Id == bb.IdDominio) : null;

            var bbStandards = standardHistories.Where(s => s.IdBuildingBlock == buildingBlockId).ToList();
            var principalStandard = bbStandards.FirstOrDefault(s => s.RolEstandar == "PRINCIPAL" && s.EstadoVigencia == "ACTIVO_VIGENTE");
            var altStandards = bbStandards.Where(s => s.RolEstandar == "ALTERNATIVA" && s.EstadoVigencia == "ACTIVO_VIGENTE").ToList();
            var corporateStandardTech = principalStandard is not null
                ? technologies.FirstOrDefault(t => t.Id == principalStandard.IdTecnologiaTSI)?.NombreCorporativo
                : null;

            var acList = adoptionCompanies
                .Where(ac => ac.IdEmpresaSubsidiaria == companyId && processes.Any(p => p.IdProcesoAdopcionTSI == ac.IdProcesoAdopcionTSI && p.IdBuildingBlock == buildingBlockId))
                .ToList();

            var applies = acList.Count == 0 || acList.Any(ac => ac.Aplica);
            var justification = acList.FirstOrDefault(ac => !ac.Aplica)?.JustificacionNoAplica;

            var compImpls = implemented
                .Where(i => i.IdEmpresaSubsidiaria == companyId &&
                            (i.IdBuildingBlock == buildingBlockId || acList.Any(ac => ac.IdProcesoAdopcionEmpresa == i.IdProcesoAdopcionEmpresa)))
                .ToList();

            var primaryImpl = compImpls.FirstOrDefault(i => i.EsTecnologiaPrimaria) ?? compImpls.FirstOrDefault();

            var localTech = primaryImpl is not null ? technologies.FirstOrDefault(t => t.Id == primaryImpl.IdTecnologiaTSI) : null;
            var localTechName = localTech?.NombreCorporativo ?? localTech?.NombreLocal;
            var version = primaryImpl?.VersionDesplegada;
            var vendor = primaryImpl is not null ? vendorDict.GetValueOrDefault(primaryImpl.IdTecnologiaTSI) : null;

            var compContracts = primaryImpl is not null
                ? contracts.Where(c => c.IdTecnologiaTSIimplementadaSubsidiaria == primaryImpl.IdTecnologiaTSIimplementadaSubsidiaria).OrderBy(c => c.FechaFin).ToList()
                : [];
            var primaryContract = compContracts.FirstOrDefault();
            var contractNumber = primaryContract?.NumeroContrato;
            var contractEndDate = primaryContract?.FechaFin;

            string alignment;
            if (!applies)
            {
                alignment = "NO_APLICA";
            }
            else if (compImpls.Count == 0)
            {
                alignment = "PENDIENTE";
            }
            else if (principalStandard is not null && compImpls.Any(i => i.IdTecnologiaTSI == principalStandard.IdTecnologiaTSI))
            {
                alignment = "ALINEADO";
            }
            else if (altStandards.Any(alt => compImpls.Any(i => i.IdTecnologiaTSI == alt.IdTecnologiaTSI)))
            {
                alignment = "HOMOLOGADO";
            }
            else
            {
                alignment = "NO_ALINEADO";
            }

            allAlignmentRows.Add(new CompanyTechnologyAlignmentRow(
                companyId,
                company?.Nombre ?? $"Empresa {companyId}",
                company?.Pais,
                domain?.Dominio ?? "Dominio no asignado",
                buildingBlockId,
                bb?.Nombre ?? $"Building Block {buildingBlockId}",
                corporateStandardTech,
                localTechName,
                version,
                vendor,
                contractNumber,
                contractEndDate,
                alignment,
                justification));
        }

        // KPIs calculados sobre el universo global de registros de alineación
        var totalParticipantes = allAlignmentRows.Select(r => r.EmpresaId).Distinct().Count();
        var totalAlineadas = allAlignmentRows.Count(r => r.EstadoAlineacion == "ALINEADO");
        var totalHomologadas = allAlignmentRows.Count(r => r.EstadoAlineacion == "HOMOLOGADO");
        var totalNoAlineadas = allAlignmentRows.Count(r => r.EstadoAlineacion == "NO_ALINEADO");
        var totalNoAplica = allAlignmentRows.Count(r => r.EstadoAlineacion == "NO_APLICA");
        var totalPendientes = allAlignmentRows.Count(r => r.EstadoAlineacion == "PENDIENTE");

        var kpis = new CompanyAdoptionSummaryKpis(
            totalParticipantes,
            totalAlineadas,
            totalHomologadas,
            totalNoAlineadas,
            totalNoAplica,
            totalPendientes);

        // Listas de filtros para el cliente
        var dominiosFilter = domains.OrderBy(d => d.Dominio).Select(d => new CatalogOption(d.Id, d.Dominio ?? $"Dominio {d.Id}")).ToList();
        var buildingBlocksFilter = buildingBlocks.OrderBy(b => b.Nombre).Select(b => new CatalogOption(b.Id, b.Nombre ?? $"Building Block {b.Id}")).ToList();
        var empresasFilter = companies.OrderBy(c => c.Nombre).Select(c => new CatalogOption(c.Id, c.Nombre ?? $"Empresa {c.Id}")).ToList();

        // Aplicación de filtros sobre las filas detalladas
        var filteredRows = allAlignmentRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            filteredRows = filteredRows.Where(r =>
                r.EmpresaNombre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.DominioNombre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.BuildingBlockNombre.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.TecnologiaEstandarCorporativa?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.TecnologiaLocalImplementada?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.VendorLocal?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (query.DominioId.HasValue)
        {
            var targetDomain = domains.FirstOrDefault(d => d.Id == query.DominioId.Value);
            if (targetDomain is not null)
            {
                filteredRows = filteredRows.Where(r => string.Equals(r.DominioNombre, targetDomain.Dominio, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (query.BuildingBlockId.HasValue)
        {
            filteredRows = filteredRows.Where(r => r.BuildingBlockId == query.BuildingBlockId.Value);
        }

        if (query.EmpresaId.HasValue)
        {
            filteredRows = filteredRows.Where(r => r.EmpresaId == query.EmpresaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.AlignmentState))
        {
            filteredRows = filteredRows.Where(r => string.Equals(r.EstadoAlineacion, query.AlignmentState, StringComparison.OrdinalIgnoreCase));
        }

        var rowsList = filteredRows.OrderBy(r => r.DominioNombre).ThenBy(r => r.BuildingBlockNombre).ThenBy(r => r.EmpresaNombre).ToList();
        var totalFiltered = rowsList.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalFiltered / query.PageSize));
        var pagedRows = rowsList.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return new CompanyAdoptionReport(
            query,
            kpis,
            domainMetrics,
            pagedRows,
            totalFiltered,
            totalPages,
            dominiosFilter,
            buildingBlocksFilter,
            empresasFilter);
    }

    private static async Task<CompanyCisoReportKpis> ReadKpisAsync(DbConnection connection, CompanyCisoReportQuery query, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH Stats AS (
                SELECT e.[idEmpresaSubsidiaria], COUNT(c.[idCiso]) AS CisoCount,
                       SUM(CASE WHEN c.[Representante] = 1 THEN 1 ELSE 0 END) AS RepresentativeCount
                FROM [dbo].[TEmpresaSubsidiaria] e
                LEFT JOIN [dbo].[TCISO] c ON c.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria]
                WHERE 1 = @scopeEnabled AND EXISTS (SELECT 1 FROM [dbo].[IamUsuarioOrganizacion] scope WHERE scope.[UserId] = @userId AND scope.[ValidFromUtc] <= SYSUTCDATETIME() AND (scope.[ValidUntilUtc] IS NULL OR scope.[ValidUntilUtc] > SYSUTCDATETIME()) AND (scope.[IsCorporateScope] = 1 OR scope.[EmpresaSubsidiariaId] = e.[idEmpresaSubsidiaria]))
                GROUP BY e.[idEmpresaSubsidiaria]
            )
            SELECT COUNT(*), COALESCE(SUM(CASE WHEN CisoCount > 0 THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN CisoCount = 0 THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN RepresentativeCount > 0 THEN 1 ELSE 0 END), 0),
                   COALESCE(SUM(CASE WHEN RepresentativeCount = 0 THEN 1 ELSE 0 END), 0)
            FROM Stats;
            """;
        AddParameter(command, "scopeEnabled", query.UserId.HasValue ? 1 : 0);
        AddParameter(command, "userId", query.UserId ?? Guid.Empty);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new CompanyCisoReportKpis(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4));
    }

    private static async Task<int> ReadCountAsync(DbConnection connection, CompanyCisoReportQuery query, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var where = AddFilters(command, query, "e", query.AllCiso ? "c" : "selected");
        command.CommandText = query.AllCiso
            ? $"SELECT COUNT(*) FROM [dbo].[TEmpresaSubsidiaria] e LEFT JOIN [dbo].[TCISO] c ON c.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] {where}"
            : $"SELECT COUNT(*) FROM [dbo].[TEmpresaSubsidiaria] e OUTER APPLY (SELECT TOP (1) c1.* FROM [dbo].[TCISO] c1 WHERE c1.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] ORDER BY CASE WHEN c1.[Representante] = 1 THEN 0 ELSE 1 END, c1.[idCiso]) selected {where}";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<CompanyCisoReportRow>> ReadRowsAsync(DbConnection connection, CompanyCisoReportQuery query, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var where = AddFilters(command, query, "e", query.AllCiso ? "c" : "selected");
        var selectCiso = query.AllCiso ? "c.[idCiso], c.[nombreCISO], c.[email], c.[LineaDeNegocio], c.[Representante]" : "selected.[idCiso], selected.[nombreCISO], selected.[email], selected.[LineaDeNegocio], selected.[Representante]";
        command.CommandText = query.AllCiso
            ? $"SELECT e.[idEmpresaSubsidiaria], e.[nombreEmpresa], e.[alias2], e.[alias3-agrupador], e.[Pais], e.[ciudad], e.[Rubro], {selectCiso}, s.[CisoCount], s.[RepresentativeCount], e.[contactoCiso], CASE WHEN EXISTS (SELECT 1 FROM [dbo].[TCISO] comparison WHERE comparison.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] AND NULLIF(LTRIM(RTRIM(comparison.[nombreCISO])), N'') = NULLIF(LTRIM(RTRIM(e.[contactoCiso])), N'')) THEN N'COINCIDE' ELSE N'DIFIERE_O_FALTA' END FROM [dbo].[TEmpresaSubsidiaria] e LEFT JOIN [dbo].[TCISO] c ON c.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] INNER JOIN (SELECT [idEmpresaSubsidiaria], COUNT(*) CisoCount, SUM(CASE WHEN [Representante] = 1 THEN 1 ELSE 0 END) RepresentativeCount FROM [dbo].[TCISO] GROUP BY [idEmpresaSubsidiaria]) s ON s.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] {where} ORDER BY e.[nombreEmpresa], c.[nombreCISO] OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY"
            : $"SELECT e.[idEmpresaSubsidiaria], e.[nombreEmpresa], e.[alias2], e.[alias3-agrupador], e.[Pais], e.[ciudad], e.[Rubro], {selectCiso}, ISNULL(s.[CisoCount], 0), ISNULL(s.[RepresentativeCount], 0), e.[contactoCiso], CASE WHEN EXISTS (SELECT 1 FROM [dbo].[TCISO] comparison WHERE comparison.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] AND NULLIF(LTRIM(RTRIM(comparison.[nombreCISO])), N'') = NULLIF(LTRIM(RTRIM(e.[contactoCiso])), N'')) THEN N'COINCIDE' ELSE N'DIFIERE_O_FALTA' END FROM [dbo].[TEmpresaSubsidiaria] e OUTER APPLY (SELECT TOP (1) c1.* FROM [dbo].[TCISO] c1 WHERE c1.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] ORDER BY CASE WHEN c1.[Representante] = 1 THEN 0 ELSE 1 END, c1.[idCiso]) selected LEFT JOIN (SELECT [idEmpresaSubsidiaria], COUNT(*) CisoCount, SUM(CASE WHEN [Representante] = 1 THEN 1 ELSE 0 END) RepresentativeCount FROM [dbo].[TCISO] GROUP BY [idEmpresaSubsidiaria]) s ON s.[idEmpresaSubsidiaria] = e.[idEmpresaSubsidiaria] {where} ORDER BY e.[nombreEmpresa] OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        AddParameter(command, "skip", (query.Page - 1) * query.PageSize);
        AddParameter(command, "take", query.PageSize);
        var rows = new List<CompanyCisoReportRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            int? cisoId = reader.IsDBNull(7) ? null : reader.GetInt32(7);
            var cisoName = reader.IsDBNull(8) ? null : reader.GetString(8);
            bool? cisoRep = reader.IsDBNull(11) ? null : Convert.ToBoolean(reader.GetValue(11));
            var cisoCount = reader.GetInt32(12);
            var repCount = reader.GetInt32(13);
            rows.Add(new CompanyCisoReportRow(reader.GetInt32(0), reader.GetString(1), ReadString(reader, 2), ReadString(reader, 3), ReadString(reader, 4), ReadString(reader, 5), ReadString(reader, 6), cisoId, cisoName, ReadString(reader, 9), ReadString(reader, 10), cisoRep, cisoCount > 0, repCount > 0, repCount > 1, null, null, ReadString(reader, 14), ReadString(reader, 15)));
        }
        return rows;
    }

    private static async Task<(IReadOnlyList<(int Id, string Name)> Companies, IReadOnlyList<string> Countries, IReadOnlyList<string> Groupings, IReadOnlyList<string> Industries, IReadOnlyList<string> CisoNames)> ReadOptionsAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        static async Task<List<string>> ReadStringsAsync(DbConnection c, string sql, CancellationToken token)
        {
            await using var command = c.CreateCommand(); command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(token); var values = new List<string>();
            while (await reader.ReadAsync(token)) if (!reader.IsDBNull(0)) values.Add(reader.GetString(0));
            return values;
        }
        var companies = new List<(int, string)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT [idEmpresaSubsidiaria], [nombreEmpresa] FROM [dbo].[TEmpresaSubsidiaria] ORDER BY [nombreEmpresa]";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) companies.Add((reader.GetInt32(0), reader.GetString(1)));
        }
        return (companies, await ReadStringsAsync(connection, "SELECT DISTINCT [Pais] FROM [dbo].[TEmpresaSubsidiaria] WHERE [Pais] IS NOT NULL ORDER BY [Pais]", cancellationToken), await ReadStringsAsync(connection, "SELECT DISTINCT [alias3-agrupador] FROM [dbo].[TEmpresaSubsidiaria] WHERE [alias3-agrupador] IS NOT NULL ORDER BY [alias3-agrupador]", cancellationToken), await ReadStringsAsync(connection, "SELECT DISTINCT [Rubro] FROM [dbo].[TEmpresaSubsidiaria] WHERE [Rubro] IS NOT NULL ORDER BY [Rubro]", cancellationToken), await ReadStringsAsync(connection, "SELECT DISTINCT [nombreCISO] FROM [dbo].[TCISO] WHERE [nombreCISO] IS NOT NULL ORDER BY [nombreCISO]", cancellationToken));
    }

    private static string AddFilters(DbCommand command, CompanyCisoReportQuery query, string companyAlias, string cisoAlias)
    {
        var conditions = new List<string>();
        AddScopeFilter(conditions, command, companyAlias, query.UserId);
        if (!string.IsNullOrWhiteSpace(query.Search)) { conditions.Add($"({companyAlias}.[nombreEmpresa] LIKE @search OR {cisoAlias}.[nombreCISO] LIKE @search)"); AddParameter(command, "search", $"%{query.Search.Trim()}%"); }
        if (query.CompanyId.HasValue) { conditions.Add($"{companyAlias}.[idEmpresaSubsidiaria] = @companyId"); AddParameter(command, "companyId", query.CompanyId.Value); }
        if (!string.IsNullOrWhiteSpace(query.Country)) { conditions.Add($"{companyAlias}.[Pais] = @country"); AddParameter(command, "country", query.Country!.Trim()); }
        if (!string.IsNullOrWhiteSpace(query.Grouping)) { conditions.Add($"{companyAlias}.[alias3-agrupador] = @grouping"); AddParameter(command, "grouping", query.Grouping!.Trim()); }
        if (!string.IsNullOrWhiteSpace(query.Industry)) { conditions.Add($"{companyAlias}.[Rubro] = @industry"); AddParameter(command, "industry", query.Industry!.Trim()); }
        if (!string.IsNullOrWhiteSpace(query.Ciso)) { conditions.Add($"{cisoAlias}.[nombreCISO] = @ciso"); AddParameter(command, "ciso", query.Ciso!.Trim()); }
        if (query.RepresentativeOnly.HasValue) { conditions.Add($"{cisoAlias}.[Representante] = @representative"); AddParameter(command, "representative", query.RepresentativeOnly.Value); }
        if (query.OnlyWithoutCiso) conditions.Add($"NOT EXISTS (SELECT 1 FROM [dbo].[TCISO] cx WHERE cx.[idEmpresaSubsidiaria] = {companyAlias}.[idEmpresaSubsidiaria])");
        if (query.OnlyWithoutRepresentative) conditions.Add($"NOT EXISTS (SELECT 1 FROM [dbo].[TCISO] cx WHERE cx.[idEmpresaSubsidiaria] = {companyAlias}.[idEmpresaSubsidiaria] AND cx.[Representante] = 1)");
        return conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions);
    }

    private static string? ReadString(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static void AddParameter(DbCommand command, string name, object value) { var parameter = command.CreateParameter(); parameter.ParameterName = "@" + name; parameter.Value = value; command.Parameters.Add(parameter); }

    private static void AddScopeFilter(List<string> conditions, DbCommand command, string companyAlias, Guid? userId)
    {
        if (!userId.HasValue) { conditions.Add("1 = 0"); return; }
        conditions.Add($"EXISTS (SELECT 1 FROM [dbo].[IamUsuarioOrganizacion] scope WHERE scope.[UserId] = @userId AND scope.[ValidFromUtc] <= SYSUTCDATETIME() AND (scope.[ValidUntilUtc] IS NULL OR scope.[ValidUntilUtc] > SYSUTCDATETIME()) AND (scope.[IsCorporateScope] = 1 OR scope.[EmpresaSubsidiariaId] = {companyAlias}.[idEmpresaSubsidiaria]))");
        AddUserScopeParameter(command, userId);
    }

    private static void AddUserScopeParameter(DbCommand command, Guid? userId)
    {
        if (userId.HasValue && !command.Parameters.Contains("@userId")) AddParameter(command, "userId", userId.Value);
    }

    private static MasterCatalogDefinition GetDefinition(string code) =>
        MasterCatalogRegistry.GetByCode(code) is { Enabled: true, IncludeInReporting: true } definition
            ? definition
            : throw new KeyNotFoundException("El catálogo solicitado no está disponible.");

    private static CatalogReportDetail ToDetail(MasterCatalogDefinition definition, CatalogPageResult page)
    {
        var columnCodes = string.Equals(definition.Code, "dominio", StringComparison.OrdinalIgnoreCase)
            ? definition.ListColumnCodes.Where(code => !string.Equals(code, "descripcion", StringComparison.OrdinalIgnoreCase)).ToArray()
            : definition.ListColumnCodes;

        var columns = columnCodes
            .Select(code => definition.Columns.Single(column => column.Code == code))
            .Select(column => new CatalogReportColumn(column.Code, column.Label)).ToArray();
        var rows = page.Items.Select(row => new CatalogReportDetailRow(row.Id,
            columns.ToDictionary(column => column.Code, column => row.DisplayValues.TryGetValue(column.Code, out var value) ? value : null, StringComparer.Ordinal))).ToArray();
        return new CatalogReportDetail(definition.Code, definition.Name, page.Page, page.PageSize, page.TotalCount, columns, rows);
    }
}