using System.Data;
using System.Data.Common;
using System.Globalization;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class BuildingBlockRelatedService(IdentityDbContext dbContext) : IBuildingBlockRelatedService
{
    public async Task<BuildingBlockRelatedResult> GetAsync(
        int buildingBlockId,
        string? capabilitySearch,
        string? functionalitySearch,
        string? technologySearch,
        int capabilityPage,
        int functionalityPage,
        int technologyPage,
        int pageSize,
        string? functionalitySortBy = null,
        string? functionalitySortDirection = null,
        string? capabilitySortBy = null,
        string? capabilitySortDirection = null,
        int? functionalityPageSize = null,
        int? capabilityPageSize = null,
        CancellationToken cancellationToken = default)
    {
        var capPageSize = NormalizePageSize(capabilityPageSize ?? pageSize);
        var funcPageSize = NormalizePageSize(functionalityPageSize ?? pageSize);
        var techPageSize = NormalizePageSize(pageSize);

        await OpenAsync(cancellationToken);
        try
        {
            var capDirection = string.Equals(capabilitySortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            var capOrderBy = capabilitySortBy?.Trim().ToLowerInvariant() switch
            {
                "capacity" => $"q.[capacidad] {capDirection}",
                "status" => $"q.[estado] {capDirection}, q.[capacidad] ASC",
                "description" => $"q.[descripcion] {capDirection}, q.[capacidad] ASC",
                _ => "COALESCE(q.[orden], 999999) ASC, q.[capacidad] ASC"
            };

            var capabilities = await QueryAsync(
                @"SELECT c.[idCapacidad] AS [__id], c.[nombreCapacidad] AS [capacidad], s.[nombreEstadoCapacidad] AS [estado], c.[descripcionCapacidad] AS [descripcion], c.[idBuildingBlock] AS [idBuildingBlock], c.[idEstadoCapacidad] AS [idEstadoCapacidad], c.[ordenVisualizacion] AS [orden]
                   FROM [dbo].[TCapacidadDeSeguridad] c LEFT JOIN [dbo].[TMEstadoCapacidad] s ON s.[idEstadoCapacidad] = c.[idEstadoCapacidad]
                   WHERE c.[idBuildingBlock] = @parentId AND (COALESCE(c.[nombreCapacidad], N'') LIKE @search OR COALESCE(s.[nombreEstadoCapacidad], N'') LIKE @search)
                   ", capabilitySearch, buildingBlockId, capabilityPage, capPageSize,
                ["capacidad", "estado", "descripcion", "idBuildingBlock", "idEstadoCapacidad", "orden"], cancellationToken, capOrderBy);

            var direction = string.Equals(functionalitySortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            var funcOrderBy = functionalitySortBy?.Trim().ToLowerInvariant() switch
            {
                "capacity" => $"q.[capacidad] {direction}, q.[funcionalidad] ASC",
                "functionality" => $"q.[funcionalidad] {direction}, q.[capacidad] ASC",
                "status" => $"q.[estado] {direction}, q.[capacidad] ASC, q.[funcionalidad] ASC",
                _ => "q.[capacidad] ASC, q.[funcionalidad] ASC"
            };

            var functionalities = await QueryAsync(
                 @"SELECT f.[idFuncionalidad] AS [__id], c.[nombreCapacidad] AS [capacidad], f.[nombreFuncionalidad] AS [funcionalidad], s.[nombreEstadoFuncionalidad] AS [estado], f.[idCapacidad] AS [idCapacidad], f.[idEstadoCoberturaFuncionalidad] AS [idEstadoCoberturaFuncionalidad]
                   FROM [dbo].[TFuncionalidad] f INNER JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idCapacidad] = f.[idCapacidad]
                   LEFT JOIN [dbo].[TMEstadoFuncionalidad] s ON s.[idEstadoCoberturaFuncionalidad] = f.[idEstadoCoberturaFuncionalidad]
                   WHERE c.[idBuildingBlock] = @parentId AND (COALESCE(f.[nombreFuncionalidad], N'') LIKE @search OR COALESCE(c.[nombreCapacidad], N'') LIKE @search OR COALESCE(s.[nombreEstadoFuncionalidad], N'') LIKE @search)
                    ", functionalitySearch, buildingBlockId, functionalityPage, funcPageSize,
                ["capacidad", "funcionalidad", "estado", "idCapacidad", "idEstadoCoberturaFuncionalidad"], cancellationToken, funcOrderBy);
            var technologies = await QueryAsync(
                 @"SELECT DISTINCT t.[idTecnologiaTSI] AS [__id], t.[nombreTecnologiaAlternativa1-Corporativo] AS [tecnologia], f.[nombreFamilia] AS [familia], a.[nombreEstadoAdopcionTSI] AS [estadoAdopcion]
                   FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] b INNER JOIN [dbo].[TTecnologiaTSI] t ON t.[idTecnologiaTSI] = b.[idTecnologiaTSI]
                   LEFT JOIN [dbo].[TMFamilia] f ON f.[idFamilia] = t.[idFamilia]
                   LEFT JOIN [dbo].[TMEstadoAdopcionTSI] a ON a.[idEstadoAdopcionTSI] = t.[idEstadoAdopcionTSI]
                   WHERE b.[idBuildingBlock] = @parentId AND (COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'') LIKE @search OR COALESCE(f.[nombreFamilia], N'') LIKE @search OR COALESCE(a.[nombreEstadoAdopcionTSI], N'') LIKE @search)
                    ", technologySearch, buildingBlockId, technologyPage, techPageSize,
                ["tecnologia", "familia", "estadoAdopcion"], cancellationToken);
            return new BuildingBlockRelatedResult(capabilities, functionalities, technologies);
        }
        finally { await CloseAsync(); }
    }

    private static int NormalizePageSize(int size) => size is 10 or 25 or 50 or 100 ? size : 10;

    private async Task<CatalogPageResult> QueryAsync(string sql, string? search, int parentId, int page, int pageSize, IReadOnlyList<string> codes, CancellationToken cancellationToken, string? customOrderBy = null)
    {
        page = Math.Max(1, page);
        await using var count = CreateCommand($"SELECT COUNT_BIG(*) FROM ({sql}) q", search, parentId, includePaging: false);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, pages);
        var orderBy = !string.IsNullOrWhiteSpace(customOrderBy) ? customOrderBy : $"q.[{codes[0]}]";
        await using var command = CreateCommand($"SELECT * FROM ({sql}) q ORDER BY {orderBy} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", search, parentId, includePaging: true);
        Add(command, "@offset", (page - 1) * pageSize); Add(command, "@pageSize", pageSize);
        var rows = new List<CatalogRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            var display = new Dictionary<string, string?>(StringComparer.Ordinal);
            for (var index = 0; index < codes.Count; index++)
            {
                var value = reader.GetValue(index + 1) is DBNull ? null : reader.GetValue(index + 1);
                values[codes[index]] = value;
                display[codes[index]] = value?.ToString();
            }
            rows.Add(new CatalogRow(reader.GetInt32(0), values, display));
        }
        return new CatalogPageResult(rows, page, pageSize, total);
    }

    private DbCommand CreateCommand(string sql, string? search, int parentId, bool includePaging)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
        Add(command, "@parentId", parentId); Add(command, "@search", $"%{search?.Trim() ?? string.Empty}%"); return command;
    }
    private static void Add(DbCommand command, string name, object value) { var parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter); }
    private bool opened;
    private async Task OpenAsync(CancellationToken cancellationToken) { opened = dbContext.Database.GetDbConnection().State != ConnectionState.Open; if (opened) await dbContext.Database.OpenConnectionAsync(cancellationToken); }
    private async Task CloseAsync() { if (opened) { await dbContext.Database.CloseConnectionAsync(); opened = false; } }
}