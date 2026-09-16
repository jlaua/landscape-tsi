using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class CatalogManagementService(IdentityDbContext dbContext, IAuditTrailService auditTrail) : ICatalogManagementService
{
    public async Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null)
    {
        EnsureWhitelisted(definition);
        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 or 100 ? pageSize : 10;
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var joins = BuildJoins(definition);
            var where = BuildSearch(definition, search);
            var countSql = $"SELECT COUNT_BIG(*) FROM {Table(definition)} c {joins} {where.Clause}";
            await using var countCommand = CreateCommand(countSql, where.SearchValue);
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var selectSql = $"SELECT {BuildSelect(definition)} FROM {Table(definition)} c {joins} {where.Clause} " +
                $"ORDER BY {BuildOrderBy(definition, sortColumn, sortDirection)} " +
                "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            await using var command = CreateCommand(selectSql, where.SearchValue);
            AddParameter(command, "@offset", (page - 1) * pageSize);
            AddParameter(command, "@pageSize", pageSize);
            var rows = await ReadRowsAsync(command, definition, cancellationToken);
            return new CatalogPageResult(rows, page, pageSize, totalCount);
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    public async Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        EnsureWhitelisted(definition);
        EnsureForeignKey(definition, foreignKey);
        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 or 100 ? pageSize : 10;
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var joins = BuildJoins(definition);
            var where = BuildSearch(definition, search);
            var relationClause = string.IsNullOrEmpty(where.Clause) ? "WHERE" : $"{where.Clause} AND";
            var predicate = $"{relationClause} c.{Quote(foreignKey.PhysicalName)} = @parentId";
            var countSql = $"SELECT COUNT_BIG(*) FROM {Table(definition)} c {joins} {predicate}";
            await using var countCommand = CreateCommand(countSql, where.SearchValue);
            AddParameter(countCommand, "@parentId", parentId);
            var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);
            var selectSql = $"SELECT {BuildSelect(definition)} FROM {Table(definition)} c {joins} {predicate} " +
                $"ORDER BY c.{Quote(definition.DisplayColumn.PhysicalName)}, c.{Quote(definition.PrimaryKeyColumn)} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            await using var command = CreateCommand(selectSql, where.SearchValue);
            AddParameter(command, "@parentId", parentId);
            AddParameter(command, "@offset", (page - 1) * pageSize);
            AddParameter(command, "@pageSize", pageSize);
            return new CatalogPageResult(await ReadRowsAsync(command, definition, cancellationToken), page, pageSize, totalCount);
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default)
    {
        EnsureWhitelisted(parent);
        EnsureWhitelisted(child);
        EnsureForeignKey(child, foreignKey);
        var sql = $"SELECT p.{Quote(parent.PrimaryKeyColumn)}, p.{Quote(parent.DisplayColumn.PhysicalName)}, COUNT(c.{Quote(child.PrimaryKeyColumn)}) " +
            $"FROM {Table(parent)} p LEFT JOIN {Table(child)} c ON c.{Quote(foreignKey.PhysicalName)} = p.{Quote(parent.PrimaryKeyColumn)} " +
            $"GROUP BY p.{Quote(parent.PrimaryKeyColumn)}, p.{Quote(parent.DisplayColumn.PhysicalName)} " +
            $"ORDER BY p.{Quote(parent.DisplayColumn.PhysicalName)}, p.{Quote(parent.PrimaryKeyColumn)}";
        await OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new List<CatalogRelationBucket>();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new CatalogRelationBucket(reader.GetInt32(0), reader.IsDBNull(1) ? "Sin nombre" : reader.GetValue(1).ToString()!, reader.GetInt32(2)));
            }
            return result;
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default)
    {
        EnsureWhitelisted(child);
        EnsureForeignKey(child, foreignKey);
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var sql = $"SELECT COUNT_BIG(*) FROM {Table(child)} c WHERE c.{Quote(foreignKey.PhysicalName)} = @parentId";
            await using var command = CreateCommand(sql);
            AddParameter(command, "@parentId", parentId);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<IReadOnlyDictionary<int, int>> GetVendorTechnologyCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default)
    {
        var idList = vendorIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<int, int>();

        var result = new Dictionary<int, int>();
        foreach (var id in idList) result[id] = 0;

        await OpenConnectionAsync(cancellationToken);
        try
        {
            var paramNames = new List<string>();
            for (var i = 0; i < idList.Count; i++)
            {
                paramNames.Add($"@p{i}");
            }

            var sql = $@"
SELECT v.idVendor, COUNT(DISTINCT all_techs.idTecnologiaTSI) AS TechCount
FROM [dbo].[TVendor] v
OUTER APPLY (
    SELECT v2.idTecnologiaTSI
    FROM [dbo].[TVendor] v2
    WHERE (v2.idVendor = v.idVendor OR (v2.nombreVendor = v.nombreVendor AND v.nombreVendor IS NOT NULL AND v.nombreVendor <> ''))
      AND v2.idTecnologiaTSI IS NOT NULL
    UNION
    SELECT s.idTecnologiaTSI
    FROM [dbo].[TServicioTecnologia] s
    WHERE (s.idVendor = v.idVendor OR s.idVendor IN (SELECT v3.idVendor FROM [dbo].[TVendor] v3 WHERE v3.nombreVendor = v.nombreVendor AND v.nombreVendor IS NOT NULL AND v.nombreVendor <> ''))
      AND s.idTecnologiaTSI IS NOT NULL
) all_techs
WHERE v.idVendor IN ({string.Join(", ", paramNames)})
GROUP BY v.idVendor";

            await using var command = CreateCommand(sql);
            for (var i = 0; i < idList.Count; i++)
            {
                AddParameter(command, paramNames[i], idList[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var vendorId = reader.GetInt32(0);
                var count = reader.GetInt32(1);
                result[vendorId] = count;
            }

            return result;
        }
        catch (Exception)
        {
            try
            {
                var paramNames = new List<string>();
                for (var i = 0; i < idList.Count; i++) paramNames.Add($"@p{i}");
                var fallbackSql = $@"
SELECT v.idVendor, COUNT(DISTINCT v2.idTecnologiaTSI) AS TechCount
FROM [dbo].[TVendor] v
LEFT JOIN [dbo].[TVendor] v2 ON (v2.idVendor = v.idVendor OR (v2.nombreVendor = v.nombreVendor AND v.nombreVendor IS NOT NULL AND v.nombreVendor <> '')) AND v2.idTecnologiaTSI IS NOT NULL
WHERE v.idVendor IN ({string.Join(", ", paramNames)})
GROUP BY v.idVendor";

                await using var fallbackCmd = CreateCommand(fallbackSql);
                for (var i = 0; i < idList.Count; i++) AddParameter(fallbackCmd, paramNames[i], idList[i]);
                await using var fallbackReader = await fallbackCmd.ExecuteReaderAsync(cancellationToken);
                while (await fallbackReader.ReadAsync(cancellationToken))
                {
                    result[fallbackReader.GetInt32(0)] = fallbackReader.GetInt32(1);
                }
                return result;
            }
            catch
            {
                return result;
            }
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<IReadOnlyList<VendorTechnologyDto>> GetVendorTechnologiesAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var sql = @"
DECLARE @vName NVARCHAR(200);
SELECT @vName = nombreVendor FROM [dbo].[TVendor] WHERE idVendor = @vendorId;

SELECT DISTINCT
    t.idTecnologiaTSI AS Id,
    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'Sin nombre corporativo') AS NombreCorporativo,
    t.[nombreTecnologiaAlternativa2-Local] AS NombreLocal,
    f.nombreFamilia AS Familia,
    ea.nombreEstadoAdopcionTSI AS EstadoAdopcion,
    t.[modeloEsquemaLicenciamientoSubscripcion] AS Licenciamiento,
    t.[entornoImplementacion] AS Entorno
FROM [dbo].[TTecnologiaTSI] t
LEFT JOIN [dbo].[TMFamilia] f ON f.idFamilia = t.idFamilia
LEFT JOIN [dbo].[TMEstadoAdopcionTSI] ea ON ea.idEstadoAdopcionTSI = t.idEstadoAdopcionTSI
WHERE t.idTecnologiaTSI IN (
    SELECT v2.idTecnologiaTSI
    FROM [dbo].[TVendor] v2
    WHERE (v2.idVendor = @vendorId OR (v2.nombreVendor = @vName AND @vName IS NOT NULL AND @vName <> ''))
      AND v2.idTecnologiaTSI IS NOT NULL
    UNION
    SELECT s.idTecnologiaTSI
    FROM [dbo].[TServicioTecnologia] s
    WHERE (s.idVendor = @vendorId OR s.idVendor IN (SELECT v3.idVendor FROM [dbo].[TVendor] v3 WHERE v3.nombreVendor = @vName AND @vName IS NOT NULL AND @vName <> ''))
      AND s.idTecnologiaTSI IS NOT NULL
)
ORDER BY COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'Sin nombre corporativo');";

            await using var command = CreateCommand(sql);
            AddParameter(command, "@vendorId", vendorId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<VendorTechnologyDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new VendorTechnologyDto(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)
                ));
            }
            return list;
        }
        catch (Exception)
        {
            try
            {
                var fallbackSql = @"
DECLARE @vName NVARCHAR(200);
SELECT @vName = nombreVendor FROM [dbo].[TVendor] WHERE idVendor = @vendorId;

SELECT DISTINCT
    t.idTecnologiaTSI AS Id,
    COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'Sin nombre corporativo') AS NombreCorporativo,
    t.[nombreTecnologiaAlternativa2-Local] AS NombreLocal,
    f.nombreFamilia AS Familia,
    ea.nombreEstadoAdopcionTSI AS EstadoAdopcion,
    t.[modeloEsquemaLicenciamientoSubscripcion] AS Licenciamiento,
    t.[entornoImplementacion] AS Entorno
FROM [dbo].[TTecnologiaTSI] t
LEFT JOIN [dbo].[TMFamilia] f ON f.idFamilia = t.idFamilia
LEFT JOIN [dbo].[TMEstadoAdopcionTSI] ea ON ea.idEstadoAdopcionTSI = t.idEstadoAdopcionTSI
WHERE t.idTecnologiaTSI IN (
    SELECT v2.idTecnologiaTSI
    FROM [dbo].[TVendor] v2
    WHERE (v2.idVendor = @vendorId OR (v2.nombreVendor = @vName AND @vName IS NOT NULL AND @vName <> ''))
      AND v2.idTecnologiaTSI IS NOT NULL
)
ORDER BY COALESCE(t.[nombreTecnologiaAlternativa1-Corporativo], N'Sin nombre corporativo');";

                await using var fallbackCmd = CreateCommand(fallbackSql);
                AddParameter(fallbackCmd, "@vendorId", vendorId);
                await using var fallbackReader = await fallbackCmd.ExecuteReaderAsync(cancellationToken);
                var fallbackList = new List<VendorTechnologyDto>();
                while (await fallbackReader.ReadAsync(cancellationToken))
                {
                    fallbackList.Add(new VendorTechnologyDto(
                        fallbackReader.GetInt32(0),
                        fallbackReader.GetString(1),
                        fallbackReader.IsDBNull(2) ? null : fallbackReader.GetString(2),
                        fallbackReader.IsDBNull(3) ? null : fallbackReader.GetString(3),
                        fallbackReader.IsDBNull(4) ? null : fallbackReader.GetString(4),
                        fallbackReader.IsDBNull(5) ? null : fallbackReader.GetString(5),
                        fallbackReader.IsDBNull(6) ? null : fallbackReader.GetString(6)
                    ));
                }
                return fallbackList;
            }
            catch
            {
                return [];
            }
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<IReadOnlyDictionary<int, int>> GetVendorContactCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default)
    {
        var idList = vendorIds.Distinct().ToList();
        var result = new Dictionary<int, int>();
        if (idList.Count == 0) return result;
        foreach (var id in idList) result[id] = 0;

        await OpenConnectionAsync(cancellationToken);
        try
        {
            var paramNames = new List<string>();
            for (var i = 0; i < idList.Count; i++) paramNames.Add($"@p{i}");

            var sql = $@"
SELECT v.idVendor, COUNT(DISTINCT cv.idContactoVendor) AS ContactCount
FROM [dbo].[TVendor] v
LEFT JOIN [dbo].[TVendor] v2 ON (v2.idVendor = v.idVendor OR (v2.nombreVendor = v.nombreVendor AND v.nombreVendor IS NOT NULL AND v.nombreVendor <> ''))
LEFT JOIN [dbo].[TContactoVendor] cv ON cv.idVendor = v2.idVendor
WHERE v.idVendor IN ({string.Join(", ", paramNames)})
GROUP BY v.idVendor";

            await using var command = CreateCommand(sql);
            for (var i = 0; i < idList.Count; i++)
            {
                AddParameter(command, paramNames[i], idList[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result[reader.GetInt32(0)] = reader.GetInt32(1);
            }

            return result;
        }
        catch
        {
            try
            {
                var paramNames = new List<string>();
                for (var i = 0; i < idList.Count; i++) paramNames.Add($"@p{i}");
                var fallbackSql = $@"
SELECT v.idVendor, COUNT(DISTINCT cv.idContactoVendor) AS ContactCount
FROM [dbo].[TVendor] v
LEFT JOIN [dbo].[TContactoVendor] cv ON cv.idVendor = v.idVendor
WHERE v.idVendor IN ({string.Join(", ", paramNames)})
GROUP BY v.idVendor";

                await using var fallbackCmd = CreateCommand(fallbackSql);
                for (var i = 0; i < idList.Count; i++) AddParameter(fallbackCmd, paramNames[i], idList[i]);
                await using var fallbackReader = await fallbackCmd.ExecuteReaderAsync(cancellationToken);
                while (await fallbackReader.ReadAsync(cancellationToken))
                {
                    result[fallbackReader.GetInt32(0)] = fallbackReader.GetInt32(1);
                }
                return result;
            }
            catch
            {
                return result;
            }
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<IReadOnlyDictionary<int, int>> GetPartnerContactCountsAsync(IEnumerable<int> partnerIds, CancellationToken cancellationToken = default)
    {
        var idList = partnerIds.Distinct().ToList();
        var result = new Dictionary<int, int>();
        if (idList.Count == 0) return result;
        foreach (var id in idList) result[id] = 0;

        await OpenConnectionAsync(cancellationToken);
        try
        {
            var paramNames = new List<string>();
            for (var i = 0; i < idList.Count; i++) paramNames.Add($"@p{i}");

            var sql = $@"
SELECT p.idPartner, COUNT(DISTINCT cp.idContactoPartner) AS ContactCount
FROM [dbo].[TPartner] p
LEFT JOIN [dbo].[TPartner] p2 ON (p2.idPartner = p.idPartner OR (p2.nombrePartner = p.nombrePartner AND p.nombrePartner IS NOT NULL AND p.nombrePartner <> ''))
LEFT JOIN [dbo].[TContactoPartner] cp ON cp.idPartner = p2.idPartner
WHERE p.idPartner IN ({string.Join(", ", paramNames)})
GROUP BY p.idPartner";

            await using var command = CreateCommand(sql);
            for (var i = 0; i < idList.Count; i++)
            {
                AddParameter(command, paramNames[i], idList[i]);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result[reader.GetInt32(0)] = reader.GetInt32(1);
            }

            return result;
        }
        catch
        {
            try
            {
                var paramNames = new List<string>();
                for (var i = 0; i < idList.Count; i++) paramNames.Add($"@p{i}");
                var fallbackSql = $@"
SELECT p.idPartner, COUNT(DISTINCT cp.idContactoPartner) AS ContactCount
FROM [dbo].[TPartner] p
LEFT JOIN [dbo].[TContactoPartner] cp ON cp.idPartner = p.idPartner
WHERE p.idPartner IN ({string.Join(", ", paramNames)})
GROUP BY p.idPartner";

                await using var fallbackCmd = CreateCommand(fallbackSql);
                for (var i = 0; i < idList.Count; i++) AddParameter(fallbackCmd, paramNames[i], idList[i]);
                await using var fallbackReader = await fallbackCmd.ExecuteReaderAsync(cancellationToken);
                while (await fallbackReader.ReadAsync(cancellationToken))
                {
                    result[fallbackReader.GetInt32(0)] = fallbackReader.GetInt32(1);
                }
                return result;
            }
            catch
            {
                return result;
            }
        }
        finally { await CloseConnectionAsync(); }
    }

    public async Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default)
    {
        EnsureWhitelisted(definition);
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var sql = $"SELECT {BuildSelect(definition)} FROM {Table(definition)} c {BuildJoins(definition)} " +
                $"WHERE c.{Quote(definition.PrimaryKeyColumn)} = @id";
            await using var command = CreateCommand(sql);
            AddParameter(command, "@id", id);
            return (await ReadRowsAsync(command, definition, cancellationToken)).SingleOrDefault();
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default)
    {
        EnsureWhitelisted(definition);
        var result = new Dictionary<string, IReadOnlyList<CatalogOption>>(StringComparer.Ordinal);
        await OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var column in definition.Columns.Where(column => column.Type == CatalogFieldType.ForeignKey))
            {
                var referenced = MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)
                    ?? throw new InvalidOperationException($"Referencia de catálogo no registrada: {column.ReferenceCatalogCode}.");
                var sql = $"SELECT {Quote(referenced.PrimaryKeyColumn)}, {Quote(referenced.DisplayColumn.PhysicalName)} " +
                    $"FROM {Table(referenced)} ORDER BY {Quote(referenced.DisplayColumn.PhysicalName)}, {Quote(referenced.PrimaryKeyColumn)}";
                await using var command = CreateCommand(sql);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var options = new List<CatalogOption>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    options.Add(new CatalogOption(reader.GetInt32(0), reader.IsDBNull(1) ? "Sin nombre" : reader.GetValue(1).ToString()!));
                }

                result[column.Code] = options;
            }

            return result;
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) =>
        SaveAsync(definition, null, values, actorUserId, correlationId, cancellationToken);

    public async Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        if (await GetAsync(definition, id, cancellationToken) is null)
        {
            return false;
        }

        await SaveAsync(definition, id, values, actorUserId, correlationId, cancellationToken);
        return true;
    }

    private async Task<int> SaveAsync(MasterCatalogDefinition definition, int? id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken)
    {
        EnsureWhitelisted(definition);
        if (definition.IsReadOnly)
        {
            throw new CatalogValidationException($"El catálogo {definition.Name} es de solo lectura.");
        }
        var before = id.HasValue ? await GetAsync(definition, id.Value, cancellationToken) : null;
        var normalized = CatalogCommandValidator.Normalize(definition, values);
        if (before is not null)
        {
            foreach (var column in definition.Columns)
            {
                normalized[column.Code] = CatalogDisplayPrivacy.PreserveExistingOnBlankUpdate(
                    definition,
                    column.Code,
                    normalized[column.Code],
                    before.Values.GetValueOrDefault(column.Code));
            }
        }
        await ValidateForeignKeysAsync(definition, normalized, cancellationToken);
        await OpenConnectionAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var commandText = id.HasValue ? BuildUpdate(definition) : BuildInsert(definition);
            await using var command = CreateCommand(commandText);
            command.Transaction = transaction.GetDbTransaction();
            foreach (var column in definition.Columns)
            {
                AddParameter(command, $"@{column.Code}", normalized[column.Code] ?? DBNull.Value);
            }

            if (id.HasValue)
            {
                AddParameter(command, "@id", id.Value);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                id = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            }

            var displayName = normalized.TryGetValue(definition.DisplayColumn.Code, out var displayValue)
                ? displayValue?.ToString()
                : null;
            if (id.HasValue && before is null)
            {
                await auditTrail.RecordCreateAsync(definition.Code, definition.PhysicalTable, id.Value, displayName,
                    actorUserId, correlationId, $"Creación de {definition.Name}.", 1, cancellationToken);
            }
            else if (id.HasValue)
            {
                await auditTrail.RecordUpdateAsync(definition.Code, definition.PhysicalTable, id.Value, displayName,
                    actorUserId, correlationId, $"Actualización de {definition.Name}.", cancellationToken);
            }

            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                ActorUserId = actorUserId,
                EventType = id.HasValue && before is not null ? "MasterCatalog.Updated" : "MasterCatalog.Created",
                PermissionCode = before is null ? Permissions.CatalogCreate : Permissions.CatalogEdit,
                Result = "Succeeded",
                ResourceType = definition.Name,
                ResourceId = id.Value.ToString(CultureInfo.InvariantCulture),
                BeforeJson = before is null ? null : JsonSerializer.Serialize(RedactSensitiveValues(definition, before.DisplayValues.ToDictionary(item => item.Key, item => (object?)item.Value, StringComparer.Ordinal))),
                AfterJson = JsonSerializer.Serialize(RedactSensitiveValues(definition, normalized)),
                CorrelationId = correlationId
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return id.Value;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    private static IReadOnlyDictionary<string, object?> RedactSensitiveValues(MasterCatalogDefinition definition, IReadOnlyDictionary<string, object?> values)
    {
        if (!string.Equals(definition.Code, "ciso", StringComparison.Ordinal)) return values;
        return values.ToDictionary(item => item.Key, item => item.Key is "email" or "telefono" ? "[REDACTED]" : item.Value, StringComparer.Ordinal);
    }

    private async Task ValidateForeignKeysAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken)
    {
        var foreignKeys = definition.Columns.Where(column => column.Type == CatalogFieldType.ForeignKey && values[column.Code] is not null).ToArray();
        if (foreignKeys.Length == 0) return;
        await OpenConnectionAsync(cancellationToken);
        try
        {
            foreach (var column in foreignKeys)
            {
                var referenced = MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)!;
                var sql = $"SELECT COUNT_BIG(*) FROM {Table(referenced)} WHERE {Quote(referenced.PrimaryKeyColumn)} = @id";
                await using var command = CreateCommand(sql);
                AddParameter(command, "@id", values[column.Code]!);
                var exists = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) > 0;
                if (!exists) throw new CatalogValidationException($"Seleccione un valor válido para {column.Label}.");
            }
        }
        finally { await CloseConnectionAsync(); }
    }

    private static string BuildSelect(MasterCatalogDefinition definition)
    {
        var fields = new List<string> { $"c.{Quote(definition.PrimaryKeyColumn)} AS {Quote("__id")}" };
        var foreignIndex = 0;
        foreach (var column in definition.Columns)
        {
            if (column.Type == CatalogFieldType.ForeignKey)
            {
                var referenced = MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)!;
                fields.Add($"c.{Quote(column.PhysicalName)} AS {Quote($"__raw_{column.Code}")}");
                fields.Add($"f{foreignIndex}.{Quote(referenced.DisplayColumn.PhysicalName)} AS {Quote(column.Code)}");
                foreignIndex++;
            }
            else
            {
                fields.Add($"c.{Quote(column.PhysicalName)} AS {Quote(column.Code)}");
            }
        }

        return string.Join(", ", fields);
    }

    private static string BuildJoins(MasterCatalogDefinition definition)
    {
        var joins = new List<string>();
        var foreignIndex = 0;
        foreach (var column in definition.Columns.Where(column => column.Type == CatalogFieldType.ForeignKey))
        {
            var referenced = MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)!;
            joins.Add($"LEFT JOIN {Table(referenced)} f{foreignIndex} ON c.{Quote(column.PhysicalName)} = f{foreignIndex}.{Quote(referenced.PrimaryKeyColumn)}");
            foreignIndex++;
        }

        return string.Join(" ", joins);
    }

    private static (string Clause, string? SearchValue) BuildSearch(MasterCatalogDefinition definition, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return (string.Empty, null);
        }

        var predicates = new List<string>();
        var foreignIndex = 0;
        foreach (var column in definition.Columns)
        {
            if (column.Type == CatalogFieldType.ForeignKey)
            {
                var referenced = MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)!;
                predicates.Add($"COALESCE(CONVERT(nvarchar(max), f{foreignIndex}.{Quote(referenced.DisplayColumn.PhysicalName)}), N'') LIKE @search");
                foreignIndex++;
            }
            else
            {
                predicates.Add($"COALESCE(CONVERT(nvarchar(max), c.{Quote(column.PhysicalName)}), N'') LIKE @search");
            }
        }

        return ($"WHERE ({string.Join(" OR ", predicates)})", $"%{search.Trim()}%");
    }

    private static string BuildInsert(MasterCatalogDefinition definition) =>
        $"INSERT INTO {Table(definition)} ({string.Join(", ", definition.Columns.Select(column => Quote(column.PhysicalName)))}) " +
        $"OUTPUT INSERTED.{Quote(definition.PrimaryKeyColumn)} VALUES ({string.Join(", ", definition.Columns.Select(column => $"@{column.Code}"))})";

    private static string BuildUpdate(MasterCatalogDefinition definition) =>
        $"UPDATE {Table(definition)} SET {string.Join(", ", definition.Columns.Select(column => $"{Quote(column.PhysicalName)} = @{column.Code}"))} " +
        $"WHERE {Quote(definition.PrimaryKeyColumn)} = @id";

    private static async Task<List<CatalogRow>> ReadRowsAsync(DbCommand command, MasterCatalogDefinition definition, CancellationToken cancellationToken)
    {
        var rows = new List<CatalogRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            var display = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var column in definition.Columns)
            {
                var rawName = column.Type == CatalogFieldType.ForeignKey ? $"__raw_{column.Code}" : column.Code;
                var rawValue = reader[rawName] is DBNull ? null : reader[rawName];
                values[column.Code] = rawValue;
                var displayValue = reader[column.Code] is DBNull ? null : reader[column.Code];
                var formattedDisplayValue = displayValue switch
                {
                    DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    _ => displayValue?.ToString()
                };
                display[column.Code] = CatalogDisplayPrivacy.Protect(definition, column.Code, formattedDisplayValue);
            }

            rows.Add(new CatalogRow(Convert.ToInt32(reader["__id"], CultureInfo.InvariantCulture), values, display));
        }

        return rows;
    }

    private DbCommand CreateCommand(string commandText, string? searchValue = null)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = commandText;
        if (searchValue is not null)
        {
            AddParameter(command, "@search", searchValue);
        }
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private bool openedConnection;
    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        openedConnection = connection.State != ConnectionState.Open;
        if (openedConnection)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }
    }

    private async Task CloseConnectionAsync()
    {
        if (openedConnection)
        {
            await dbContext.Database.CloseConnectionAsync();
            openedConnection = false;
        }
    }

    private static void EnsureWhitelisted(MasterCatalogDefinition definition)
    {
        if (!ReferenceEquals(MasterCatalogRegistry.GetByCode(definition.Code), definition))
        {
            throw new InvalidOperationException("El catálogo no pertenece a la lista blanca administrable.");
        }
    }

    private static void EnsureForeignKey(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey)
    {
        if (definition.Columns.All(column => !ReferenceEquals(column, foreignKey) || column.Type != CatalogFieldType.ForeignKey))
        {
            throw new InvalidOperationException("La relación solicitada no pertenece al catálogo permitido.");
        }
    }

    private static string BuildOrderBy(MasterCatalogDefinition definition, string? sortColumn, string? sortDirection)
    {
        var column = definition.Columns.SingleOrDefault(item => item.Code == sortColumn && definition.ListColumnCodes.Contains(item.Code, StringComparer.Ordinal)) ?? definition.DisplayColumn;
        var direction = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        if (column.Type == CatalogFieldType.ForeignKey)
        {
            var foreignIndex = 0;
            foreach (var fkCol in definition.Columns.Where(c => c.Type == CatalogFieldType.ForeignKey))
            {
                if (fkCol.Code == column.Code)
                {
                    var referenced = MasterCatalogRegistry.GetByCode(column.ReferenceCatalogCode!)!;
                    return $"f{foreignIndex}.{Quote(referenced.DisplayColumn.PhysicalName)} {direction}, c.{Quote(definition.PrimaryKeyColumn)} ASC";
                }
                foreignIndex++;
            }
        }
        return $"c.{Quote(column.PhysicalName)} {direction}, c.{Quote(definition.PrimaryKeyColumn)} ASC";
    }

    private static string Table(MasterCatalogDefinition definition) => $"[dbo].{Quote(definition.PhysicalTable)}";
    private static string Quote(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}