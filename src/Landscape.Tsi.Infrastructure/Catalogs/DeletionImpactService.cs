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
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public sealed class DeletionImpactService(IdentityDbContext dbContext, IConfiguration configuration, IAuditTrailService auditTrail) : IDeletionImpactService
{
    private const string DomainCode = "dominio";
    private const string DomainTable = "TMDominio";
    private const string BuildingBlockCode = "building-block";
    private const string BuildingBlockTable = "TBuildingBlock";
    private const string FunctionalityCode = "funcionalidad";
    private const string FunctionalityTable = "TFuncionalidad";
    private const string CapacityCode = "capacidad-seguridad";
    private const string CapacityTable = "TCapacidadDeSeguridad";
    private readonly int typedConfirmationThreshold = configuration.GetValue("DeletionSafety:RequireTypedConfirmationAbove", 50);

    public async Task<DeletionImpactResult?> PreviewAsync(string entityCode, int rootId, CancellationToken cancellationToken = default)
    {
        EnsureSupported(entityCode);
        await OpenConnectionAsync(cancellationToken);
        try
        {
            var displayName = await ReadRootNameAsync(entityCode, rootId, cancellationToken);
            if (displayName is null) return null;
            var counts = await ReadCountsAsync(entityCode, rootId, cancellationToken);
            return BuildImpact(entityCode, rootId, displayName, counts);
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    public async Task<DeletionExecutionResult> DeleteAsync(string entityCode, int rootId, string? confirmation, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default)
    {
        EnsureSupported(entityCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        await OpenConnectionAsync(cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var displayName = await ReadRootNameAsync(entityCode, rootId, cancellationToken);
            if (displayName is null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new DeletionExecutionResult(false, 0, "El registro ya no existe.");
            }

            var counts = await ReadCountsAsync(entityCode, rootId, cancellationToken);
            var impact = BuildImpact(entityCode, rootId, displayName, counts);
            if (!impact.CanDelete)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new DeletionExecutionResult(false, 0, impact.BlockingReason);
            }
            if (impact.RequiresTypedConfirmation && !string.Equals(confirmation, "ELIMINAR", StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new DeletionExecutionResult(false, 0, "Se requiere escribir ELIMINAR para confirmar una operación de alto impacto.");
            }

            var operation = await auditTrail.BeginDeleteAsync(
                entityCode,
                MasterCatalogRegistry.GetByCode(entityCode)!.PhysicalTable,
                rootId,
                displayName,
                actorUserId,
                correlationId,
                impact.TotalRecordsToDelete,
                $"{MasterCatalogRegistry.GetByCode(entityCode)!.Name} {displayName} eliminado. Impacto: {impact.TotalRecordsToDelete} registros.",
                cancellationToken);
            await CaptureDeleteSnapshotsAsync(operation, entityCode, rootId, displayName, cancellationToken);

            var deleted = 0;
            if (entityCode == FunctionalityCode)
            {
                deleted += await ExecuteAsync("DELETE FROM [dbo].[TFuncionalidad] WHERE [idFuncionalidad] = @rootId", rootId, cancellationToken);
            }
            else if (entityCode == CapacityCode)
            {
                deleted += await ExecuteAsync("DELETE FROM [dbo].[TFuncionalidad] WHERE [idCapacidad] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE FROM [dbo].[TCapacidadDeSeguridad] WHERE [idCapacidad] = @rootId", rootId, cancellationToken);
            }
            else if (entityCode == DomainCode)
            {
                deleted += await ExecuteAsync("DELETE f FROM [dbo].[TFuncionalidad] f INNER JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idCapacidad] = f.[idCapacidad] INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = c.[idBuildingBlock] WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE c FROM [dbo].[TCapacidadDeSeguridad] c INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = c.[idBuildingBlock] WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE bridge FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = bridge.[idBuildingBlock] WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE b FROM [dbo].[TBuildingBlock] b WHERE b.[idDominio] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE FROM [dbo].[TMDominio] WHERE [iddominio] = @rootId", rootId, cancellationToken);
            }
            else
            {
                deleted += await ExecuteAsync("DELETE f FROM [dbo].[TFuncionalidad] f INNER JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idCapacidad] = f.[idCapacidad] WHERE c.[idBuildingBlock] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE c FROM [dbo].[TCapacidadDeSeguridad] c WHERE c.[idBuildingBlock] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE bridge FROM [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge WHERE bridge.[idBuildingBlock] = @rootId", rootId, cancellationToken);
                deleted += await ExecuteAsync("DELETE FROM [dbo].[TBuildingBlock] WHERE [idBuildingBlock] = @rootId", rootId, cancellationToken);
            }

            dbContext.AuthorizationAuditEvents.Add(new Domain.Identity.IamEventoAuditoriaAutorizacion
            {
                ActorUserId = actorUserId,
                EventType = "Catalog.DeletedCascade",
                PermissionCode = Permissions.CatalogDelete,
                Result = "Succeeded",
                ResourceType = entityCode,
                ResourceId = rootId.ToString(CultureInfo.InvariantCulture),
                AfterJson = JsonSerializer.Serialize(new { RootDisplayName = displayName, TotalRecordsDeleted = deleted, Tables = new[] { "TMDominio", "TBuildingBlock", "TCapacidadDeSeguridad", "TFuncionalidad", "TBuildingBlockVsTTecnologiaTSI" } }),
                CorrelationId = correlationId
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new DeletionExecutionResult(true, deleted);
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

    private DeletionImpactResult BuildImpact(string entityCode, int rootId, string displayName, DependencyCounts counts)
    {
        var functionalityNode = new DeletionDependencyNode("Funcionalidad", "TFuncionalidad", counts.Functionalities, 3, "Capacidad de Seguridad → Funcionalidad", []);
        var capacity = new DeletionDependencyNode("Capacidad de Seguridad", "TCapacidadDeSeguridad", counts.Capacities, 2, "Building Block → Capacidad de Seguridad", [functionalityNode]);
        if (entityCode == BuildingBlockCode)
        {
            var bridgeForBuilding = new DeletionDependencyNode("Relaciones Building Block / Tecnología TSI", "TBuildingBlockVsTTecnologiaTSI", counts.BridgeRows, 1, "Building Block → tabla puente", []);
            return DeletionImpactCalculator.Calculate(entityCode, BuildingBlockTable, rootId, displayName, [capacity, bridgeForBuilding], typedConfirmationThreshold);
        }

        if (entityCode == FunctionalityCode)
        {
            return DeletionImpactCalculator.Calculate(FunctionalityCode, FunctionalityTable, rootId, displayName, [], typedConfirmationThreshold);
        }

        if (entityCode == CapacityCode)
        {
            var capacityFunctionalityNode = new DeletionDependencyNode("Funcionalidad", FunctionalityTable, counts.Functionalities, 1, "Capacidad de Seguridad → Funcionalidad", []);
            return DeletionImpactCalculator.Calculate(CapacityCode, CapacityTable, rootId, displayName, [capacityFunctionalityNode], typedConfirmationThreshold);
        }

        var building = new DeletionDependencyNode("Building Block", BuildingBlockTable, counts.BuildingBlocks, 1, "Dominio → Building Block", [capacity]);
        var bridge = new DeletionDependencyNode("Relaciones Building Block / Tecnología TSI", "TBuildingBlockVsTTecnologiaTSI", counts.BridgeRows, 1, "Building Block → tabla puente", []);
        return DeletionImpactCalculator.Calculate(DomainCode, DomainTable, rootId, displayName, [building, bridge], typedConfirmationThreshold);
    }

    private async Task<string?> ReadRootNameAsync(string entityCode, int rootId, CancellationToken cancellationToken)
    {
        var sql = entityCode == DomainCode
            ? "SELECT [dominio] FROM [dbo].[TMDominio] WHERE [iddominio] = @rootId"
            : entityCode == BuildingBlockCode
                ? "SELECT [nombreBuildingBlock] FROM [dbo].[TBuildingBlock] WHERE [idBuildingBlock] = @rootId"
                : entityCode == CapacityCode
                    ? "SELECT [nombreCapacidad] FROM [dbo].[TCapacidadDeSeguridad] WHERE [idCapacidad] = @rootId"
                    : "SELECT [nombreFuncionalidad] FROM [dbo].[TFuncionalidad] WHERE [idFuncionalidad] = @rootId";
        await using var command = CreateCommand(sql, rootId);
        return await command.ExecuteScalarAsync(cancellationToken) is { } value && value is not DBNull ? value.ToString() : null;
    }

    private async Task<DependencyCounts> ReadCountsAsync(string entityCode, int rootId, CancellationToken cancellationToken)
    {
        var sql = entityCode == FunctionalityCode
            ? "SELECT 0, 0, 0, 0"
            : entityCode == CapacityCode
                ? "SELECT 0, 0, COUNT(*), 0 FROM [dbo].[TFuncionalidad] WHERE [idCapacidad] = @rootId"
            : entityCode == DomainCode ? """
            SELECT COUNT(DISTINCT b.[idBuildingBlock]), COUNT(DISTINCT c.[idCapacidad]), COUNT(DISTINCT f.[idFuncionalidad]), COUNT(DISTINCT bridge.[idBuildingBlock])
            FROM [dbo].[TBuildingBlock] b
            LEFT JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idBuildingBlock] = b.[idBuildingBlock]
            LEFT JOIN [dbo].[TFuncionalidad] f ON f.[idCapacidad] = c.[idCapacidad]
            LEFT JOIN [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge ON bridge.[idBuildingBlock] = b.[idBuildingBlock]
            WHERE b.[idDominio] = @rootId
            """ : """
            SELECT COUNT(DISTINCT b.[idBuildingBlock]), COUNT(DISTINCT c.[idCapacidad]), COUNT(DISTINCT f.[idFuncionalidad]), COUNT(DISTINCT bridge.[idBuildingBlock])
            FROM [dbo].[TBuildingBlock] b
            LEFT JOIN [dbo].[TCapacidadDeSeguridad] c ON c.[idBuildingBlock] = b.[idBuildingBlock]
            LEFT JOIN [dbo].[TFuncionalidad] f ON f.[idCapacidad] = c.[idCapacidad]
            LEFT JOIN [dbo].[TBuildingBlockVsTTecnologiaTSI] bridge ON bridge.[idBuildingBlock] = b.[idBuildingBlock]
            WHERE b.[idBuildingBlock] = @rootId
            """;
        await using var command = CreateCommand(sql, rootId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new DependencyCounts(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3))
            : new DependencyCounts(0, 0, 0, 0);
    }

    private async Task<int> ExecuteAsync(string sql, int rootId, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(sql, rootId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CaptureDeleteSnapshotsAsync(AuditOperation operation, string entityCode, int rootId,
        string rootDisplayName, CancellationToken cancellationToken)
    {
        if (entityCode == DomainCode)
        {
            await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idCapacidad] IN (SELECT c.[idCapacidad] FROM [dbo].[TCapacidadDeSeguridad] c INNER JOIN [dbo].[TBuildingBlock] b ON b.[idBuildingBlock] = c.[idBuildingBlock] WHERE b.[idDominio] = @rootId)", "nombreFuncionalidad", 0, 3, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TCapacidadDeSeguridad", "idCapacidad", "c.[idBuildingBlock] IN (SELECT b.[idBuildingBlock] FROM [dbo].[TBuildingBlock] b WHERE b.[idDominio] = @rootId)", "nombreCapacidad", 1, 2, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlockVsTTecnologiaTSI", "idBuildingBlock", "bridge.[idBuildingBlock] IN (SELECT b.[idBuildingBlock] FROM [dbo].[TBuildingBlock] b WHERE b.[idDominio] = @rootId)", null, 0, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlock", "idBuildingBlock", "b.[idDominio] = @rootId", "nombreBuildingBlock", 2, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TMDominio", "iddominio", "d.[iddominio] = @rootId", "dominio", 3, 0, true, rootId, cancellationToken);
            return;
        }

        if (entityCode == BuildingBlockCode)
        {
            await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idCapacidad] IN (SELECT c.[idCapacidad] FROM [dbo].[TCapacidadDeSeguridad] c WHERE c.[idBuildingBlock] = @rootId)", "nombreFuncionalidad", 0, 3, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TCapacidadDeSeguridad", "idCapacidad", "c.[idBuildingBlock] = @rootId", "nombreCapacidad", 1, 2, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlockVsTTecnologiaTSI", "idBuildingBlock", "bridge.[idBuildingBlock] = @rootId", null, 0, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TBuildingBlock", "idBuildingBlock", "b.[idBuildingBlock] = @rootId", "nombreBuildingBlock", 2, 0, true, rootId, cancellationToken);
            return;
        }

        if (entityCode == CapacityCode)
        {
            await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idCapacidad] = @rootId", "nombreFuncionalidad", 0, 1, false, rootId, cancellationToken);
            await CaptureRowsAsync(operation, entityCode, "TCapacidadDeSeguridad", "idCapacidad", "c.[idCapacidad] = @rootId", "nombreCapacidad", 1, 0, true, rootId, cancellationToken);
            return;
        }

        await CaptureRowsAsync(operation, entityCode, "TFuncionalidad", "idFuncionalidad", "f.[idFuncionalidad] = @rootId", "nombreFuncionalidad", 0, 0, true, rootId, cancellationToken);
    }

    private async Task CaptureRowsAsync(AuditOperation operation, string entityCode, string table, string primaryKey,
        string predicate, string? displayColumn, int deleteOrder, int restoreOrder, bool root, int rootId,
        CancellationToken cancellationToken)
    {
        var alias = table == "TMDominio" ? "d" : table == "TBuildingBlock" ? "b" : table == "TCapacidadDeSeguridad" ? "c" : table == "TFuncionalidad" ? "f" : "bridge";
        await using var command = CreateCommand($"SELECT * FROM [dbo].[{table}] {alias} WHERE {predicate}", rootId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
                values[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            values.TryGetValue(primaryKey, out var key);
            var foreignKeys = values.Where(item => item.Key.StartsWith("id", StringComparison.OrdinalIgnoreCase) && !item.Key.Equals(primaryKey, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(item => item.Key, item => item.Value);
            var displayName = displayColumn is not null && values.TryGetValue(displayColumn, out var display) ? display?.ToString() : null;
            auditTrail.AddSnapshot(operation, entityCode, table,
                JsonSerializer.Serialize(new { id = key }), JsonSerializer.Serialize(foreignKeys),
                JsonSerializer.Serialize(values), deleteOrder, restoreOrder, root && table == operation.PhysicalTableName,
                displayName);
        }
    }

    private DbCommand CreateCommand(string sql, int rootId)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@rootId";
        parameter.Value = rootId;
        command.Parameters.Add(parameter);
        return command;
    }

    private bool openedConnection;
    private async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        openedConnection = connection.State != ConnectionState.Open;
        if (openedConnection) await dbContext.Database.OpenConnectionAsync(cancellationToken);
    }

    private async Task CloseConnectionAsync()
    {
        if (openedConnection)
        {
            await dbContext.Database.CloseConnectionAsync();
            openedConnection = false;
        }
    }

    private static void EnsureSupported(string entityCode)
    {
        var definition = MasterCatalogRegistry.GetByCode(entityCode);
        if (definition is null || (entityCode != DomainCode && entityCode != BuildingBlockCode && entityCode != CapacityCode && entityCode != FunctionalityCode) || !MasterCatalogRegistry.EntityMetadata.Single(entity => entity.Code == entityCode).IsDeletable)
        {
            throw new InvalidOperationException("La entidad no pertenece a la lista blanca de eliminaciones habilitadas.");
        }
    }

    private sealed record DependencyCounts(int BuildingBlocks, int Capacities, int Functionalities, int BridgeRows);
}