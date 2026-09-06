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

public sealed class CatalogManagementService(IdentityDbContext dbContext) : ICatalogManagementService
{
    public async Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        EnsureWhitelisted(definition);
        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;
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
                $"ORDER BY c.{Quote(definition.DisplayColumn.PhysicalName)}, c.{Quote(definition.PrimaryKeyColumn)} " +
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
        var normalized = NormalizeValues(definition, values);
        var before = id.HasValue ? await GetAsync(definition, id.Value, cancellationToken) : null;
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

            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                ActorUserId = actorUserId,
                EventType = id.HasValue && before is not null ? "MasterCatalog.Updated" : "MasterCatalog.Created",
                PermissionCode = before is null ? Permissions.CatalogCreate : Permissions.CatalogEdit,
                Result = "Succeeded",
                ResourceType = definition.Name,
                ResourceId = id.Value.ToString(CultureInfo.InvariantCulture),
                BeforeJson = before is null ? null : JsonSerializer.Serialize(before.DisplayValues),
                AfterJson = JsonSerializer.Serialize(normalized),
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

    private static Dictionary<string, object?> NormalizeValues(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var column in definition.Columns)
        {
            values.TryGetValue(column.Code, out var input);
            if (string.IsNullOrWhiteSpace(input))
            {
                normalized[column.Code] = null;
                continue;
            }

            normalized[column.Code] = column.Type switch
            {
                CatalogFieldType.ForeignKey when int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var foreignKey) => foreignKey,
                CatalogFieldType.DateTime when DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date) => date,
                CatalogFieldType.Text => input.Trim(),
                CatalogFieldType.ForeignKey => throw new CatalogValidationException($"Seleccione un valor válido para {column.Label}."),
                CatalogFieldType.DateTime => throw new CatalogValidationException($"Ingrese una fecha válida para {column.Label}."),
                _ => input.Trim()
            };
        }

        return normalized;
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
                display[column.Code] = displayValue switch
                {
                    DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    _ => displayValue?.ToString()
                };
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

    private static string Table(MasterCatalogDefinition definition) => $"[dbo].{Quote(definition.PhysicalTable)}";
    private static string Quote(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}
