using System.Data;
using System.Data.Common;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Reporting;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Reporting;

public sealed class CatalogReportingService(ICatalogManagementService catalogs, IdentityDbContext? dbContext = null) : IReportingService
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