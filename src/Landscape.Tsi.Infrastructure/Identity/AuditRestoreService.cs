using System.Data;
using System.Data.Common;
using System.Text.Json;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class AuditRestoreService(
    IdentityDbContext dbContext,
    IEffectiveAccessService effectiveAccess,
    IConfiguration configuration) : IAuditRestoreService
{
    private static readonly IReadOnlyDictionary<string, TableDefinition> Tables =
        new Dictionary<string, TableDefinition>(StringComparer.Ordinal)
        {
            ["TMDominio"] = new("iddominio", ["iddominio", "dominio", "descripcionDominio", "referencias", "homologacionDimensionSegunCiber", "homologacionDimensionSegunLineamiento", "subDominioCVT", "Ejemplos"]),
            ["TBuildingBlock"] = new("idBuildingBlock", ["idBuildingBlock", "nombreBuildingBlock", "definicionBuildingBlock", "idDominio", "idEstadoFaseDeAdopcionBuildingBlock", "rutaDelEntregable", "PilarZT"]),
            ["TCapacidadDeSeguridad"] = new("idCapacidad", ["idCapacidad", "nombreCapacidad", "descripcionCapacidad", "idBuildingBlock", "idEstadoCapacidad"]),
            ["TFuncionalidad"] = new("idFuncionalidad", ["idFuncionalidad", "nombreFuncionalidad", "descripcionFuncionalidad", "idCapacidad", "idEstadoCoberturaFuncionalidad"]),
            ["TBuildingBlockVsTTecnologiaTSI"] = new(null, ["idBuildingBlock", "idTecnologiaTSI"])
        };

    public async Task<AuditRestorePreview?> PreviewAsync(Guid actorUserId, Guid operationId, CancellationToken cancellationToken = default)
    {
        var operation = await dbContext.AuditOperations.AsNoTracking()
            .Include(x => x.Snapshots)
            .SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (operation is null) return null;
        if (!await HasPermissionAsync(actorUserId, operation.EmpresaSubsidiariaId ?? 0, cancellationToken)) return null;
        var restored = await dbContext.AuditOperations.AsNoTracking()
            .Where(x => x.ReversesOperationId == operationId && x.ActionType == "RESTORE" && x.Status == "Succeeded")
            .Select(x => new { x.OccurredAtUtc })
            .SingleOrDefaultAsync(cancellationToken);
        var reason = GetBlockingReason(operation, restored?.OccurredAtUtc);
        reason ??= ValidateSnapshots(operation.Snapshots);
        return new AuditRestorePreview(operation.OperationId, operation.ActionType, operation.EntityCode,
            operation.Snapshots.Count, reason is null, reason, restored?.OccurredAtUtc);
    }

    public async Task<AuditRestoreResult> RestoreAsync(Guid actorUserId, Guid operationId, string correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var operation = await dbContext.AuditOperations
            .Include(x => x.Snapshots)
            .SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (operation is null) return new(false, "La operación no existe.");
        if (!await HasPermissionAsync(actorUserId, operation.EmpresaSubsidiariaId ?? 0, cancellationToken))
            return new(false, "No autorizado.");
        var restored = await dbContext.AuditOperations.AnyAsync(
            x => x.ReversesOperationId == operationId && x.ActionType == "RESTORE" && x.Status == "Succeeded", cancellationToken);
        var reason = GetBlockingReason(operation, restored ? DateTime.UtcNow : null);
        if (reason is not null) return new(false, reason);
        var snapshotIssue = ValidateSnapshots(operation.Snapshots);
        if (snapshotIssue is not null) return new(false, snapshotIssue);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var keyMap = new Dictionary<(string Table, string Old), string>();
            foreach (var snapshot in operation.Snapshots
                         .OrderBy(x => x.RestoreOrder)
                         .ThenBy(x => RestoreTablePriority(x.PhysicalTableName))
                         .ThenBy(x => x.SnapshotId))
            {
                var definition = Tables[snapshot.PhysicalTableName];
                var row = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(snapshot.RowDataJson)
                    ?? throw new InvalidOperationException("Snapshot incompleto.");
                var values = row.Where(x => definition.AllowedColumns.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                var oldKey = definition.IdentityColumn is null ? null : ReadJsonValue(values, definition.IdentityColumn);
                if (definition.IdentityColumn is not null) values.Remove(definition.IdentityColumn);
                foreach (var key in values.Keys.ToArray())
                {
                    if (!key.StartsWith("id", StringComparison.OrdinalIgnoreCase)) continue;
                    var oldForeignKey = ReadJsonValue(values, key);
                    if (oldForeignKey is not null && keyMap.TryGetValue((ForeignKeyTable(key), oldForeignKey), out var mapped))
                        values[key] = JsonDocument.Parse(mapped).RootElement.Clone();
                }
                var newKey = await InsertAsync(snapshot.PhysicalTableName, definition, values, cancellationToken);
                if (definition.IdentityColumn is not null && oldKey is not null)
                {
                    keyMap[(snapshot.PhysicalTableName, oldKey)] = newKey;
                    dbContext.AuditRecordKeyMaps.Add(new AuditRecordKeyMap
                    {
                        OperationId = operationId,
                        PhysicalTableName = snapshot.PhysicalTableName,
                        OldPrimaryKeyJson = JsonSerializer.Serialize(new { id = oldKey }),
                        NewPrimaryKeyJson = JsonSerializer.Serialize(new { id = newKey })
                    });
                }
            }

            var restore = new AuditOperation
            {
                CorrelationId = correlationId,
                ActionType = "RESTORE",
                EntityCode = operation.EntityCode,
                PhysicalTableName = operation.PhysicalTableName,
                RootRecordId = operation.RootRecordId,
                RootDisplayName = operation.RootDisplayName,
                ActorUserId = actorUserId,
                OccurredAtUtc = DateTime.UtcNow,
                AffectedRecordCount = operation.AffectedRecordCount,
                ReversesOperationId = operationId,
                Status = "Succeeded",
                Description = $"Restauración de la operación {operation.OperationId}."
            };
            dbContext.AuditOperations.Add(restore);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, "Restauración completada.", restore.OperationId);
        }
        catch (DbException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return new(false, "No fue posible restaurar los datos porque existe un conflicto de integridad.");
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("snapshot", StringComparison.OrdinalIgnoreCase))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return new(false, "La operación no tiene snapshots restaurables.");
        }
    }

    private async Task<bool> HasPermissionAsync(Guid actorUserId, int subsidiaryId, CancellationToken cancellationToken)
    {
        return await effectiveAccess.IsAuthorizedAsync(actorUserId, Permissions.AuditoriaRestaurar, subsidiaryId, cancellationToken);
    }

    private string? GetBlockingReason(AuditOperation operation, DateTime? restoredAtUtc)
    {
        if (!string.Equals(operation.ActionType, "DELETE", StringComparison.Ordinal)) return "Solo se pueden restaurar operaciones DELETE.";
        if (!string.Equals(operation.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase) && !string.Equals(operation.Status, "Succeeded", StringComparison.OrdinalIgnoreCase)) return "La operación no terminó correctamente.";
        if (operation.Snapshots.Count == 0) return "La operación no tiene snapshots completos.";
        if (restoredAtUtc.HasValue) return "La operación ya fue restaurada.";
        var days = configuration.GetValue("Audit:UndoRetentionDays", 30);
        if (operation.OccurredAtUtc < DateTime.UtcNow.AddDays(-days)) return "La ventana de restauración expiró.";
        return null;
    }

    private static string? ValidateSnapshots(IEnumerable<AuditDeletedRecordSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (!Tables.ContainsKey(snapshot.PhysicalTableName))
                return "El esquema del snapshot no es compatible con restore.";
            try
            {
                using var document = JsonDocument.Parse(snapshot.RowDataJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.EnumerateObject().Any())
                    return "La operación no tiene snapshots completos.";
            }
            catch (JsonException)
            {
                return "La operación no tiene snapshots completos.";
            }
        }

        return null;
    }

    private async Task<string> InsertAsync(string table, TableDefinition definition, IReadOnlyDictionary<string, JsonElement> values, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var columns = values.Keys.ToArray();
        command.CommandText = $"INSERT INTO [dbo].[{table}] ({string.Join(",", columns.Select(x => $"[{x}]"))}) VALUES ({string.Join(",", columns.Select((_, i) => $"@p{i}"))}); SELECT {(definition.IdentityColumn is null ? "0" : "CONVERT(bigint,SCOPE_IDENTITY())")};";
        for (var i = 0; i < columns.Length; i++)
        {
            var parameter = command.CreateParameter(); parameter.ParameterName = $"@p{i}"; parameter.Value = ToDbValue(values[columns[i]]); command.Parameters.Add(parameter);
        }
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? "0";
        return result;
    }

    private static object ToDbValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => DBNull.Value,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => value.GetString() ?? (object)DBNull.Value,
        _ => value.ToString()
    };

    private static string? ReadJsonValue(IReadOnlyDictionary<string, JsonElement> row, string key) => row.TryGetValue(key, out var value) ? value.ToString() : null;
    private static string ForeignKeyTable(string key) => key.ToLowerInvariant() switch
    {
        "iddominio" => "TMDominio",
        "idbuildingblock" => "TBuildingBlock",
        "idcapacidad" => "TCapacidadDeSeguridad",
        "idfuncionalidad" => "TFuncionalidad",
        "idtecnologiatsi" => "TTecnologiaTSI",
        _ => string.Empty
    };
    private static int RestoreTablePriority(string table) => table switch
    {
        "TMDominio" => 0,
        "TBuildingBlock" => 1,
        "TCapacidadDeSeguridad" => 2,
        "TFuncionalidad" => 3,
        "TBuildingBlockVsTTecnologiaTSI" => 4,
        _ => int.MaxValue
    };

    private sealed record TableDefinition(string? IdentityColumn, IReadOnlyList<string> AllowedColumns);
}