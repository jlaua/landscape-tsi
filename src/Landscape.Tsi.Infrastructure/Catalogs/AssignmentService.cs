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

public sealed class AssignmentService(
    IdentityDbContext dbContext,
    IAuditTrailService auditTrail) : IAssignmentService
{
    public async Task<AssignmentResult> AssignCapabilityAsync(
        AssignCapabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // 1. Verify target Building Block exists
            var targetCount = await ScalarIntAsync(connection, transaction.GetDbTransaction(),
                "SELECT COUNT(*) FROM dbo.TBuildingBlock WHERE idBuildingBlock = @id", ("@id", command.TargetBuildingBlockId), cancellationToken);
            if (targetCount != 1)
            {
                return AssignmentResult.Failure("El Building Block destino no existe.");
            }

            // 2. Read capability
            const string readSql = "SELECT idCapacidad, idBuildingBlock, nombreCapacidad, idEstadoCapacidad FROM dbo.TCapacidadDeSeguridad WHERE idCapacidad = @id";
            await using var readCmd = Create(connection, transaction.GetDbTransaction(), readSql, ("@id", command.CapabilityId));
            await using var reader = await readCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return AssignmentResult.Failure("La Capacidad de Seguridad no existe.");
            }

            var currentBbId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var name = reader.GetString(2);
            var stateId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            await reader.CloseAsync();

            if (currentBbId != null)
            {
                return AssignmentResult.Failure("La Capacidad ya se encuentra asignada a un Building Block. Utilice la acción de reasignación.");
            }

            // 3. Concurrency check
            if (!AssignmentConcurrencyHelper.VerifyCapabilityToken(command.ConcurrencyToken, command.CapabilityId, currentBbId, name, stateId))
            {
                return AssignmentResult.ConcurrencyConflict();
            }

            // 4. Update
            const string updateSql = "UPDATE dbo.TCapacidadDeSeguridad SET idBuildingBlock = @targetId WHERE idCapacidad = @id";
            await using var updateCmd = Create(connection, transaction.GetDbTransaction(), updateSql,
                ("@targetId", command.TargetBuildingBlockId), ("@id", command.CapabilityId));
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            // 5. Audit
            var auditDesc = $"Asociación de Capacidad a Building Block {command.TargetBuildingBlockId}. Justificación: {command.Justification ?? "N/A"}";
            await auditTrail.RecordRelationAsync("ASSOCIATE_CAPABILITY", "capacidad-seguridad", "TCapacidadDeSeguridad",
                command.CapabilityId, name, command.ActorUserId, command.CorrelationId, auditDesc, cancellationToken);

            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                ActorUserId = command.ActorUserId,
                EventType = "Capability.Associated",
                PermissionCode = Permissions.CatalogEdit,
                Result = "Succeeded",
                ResourceType = "TCapacidadDeSeguridad",
                ResourceId = command.CapabilityId.ToString(CultureInfo.InvariantCulture),
                BeforeJson = JsonSerializer.Serialize(new { idBuildingBlock = (int?)null }),
                AfterJson = JsonSerializer.Serialize(new { idBuildingBlock = command.TargetBuildingBlockId, justification = command.Justification }),
                CorrelationId = command.CorrelationId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AssignmentResult.Success();
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AssignmentResult> ReassignCapabilityAsync(
        ReassignCapabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SourceBuildingBlockId == command.TargetBuildingBlockId)
        {
            return AssignmentResult.Failure("La Capacidad ya pertenece al Building Block destino.");
        }

        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // 1. Verify target Building Block exists
            var targetCount = await ScalarIntAsync(connection, transaction.GetDbTransaction(),
                "SELECT COUNT(*) FROM dbo.TBuildingBlock WHERE idBuildingBlock = @id", ("@id", command.TargetBuildingBlockId), cancellationToken);
            if (targetCount != 1)
            {
                return AssignmentResult.Failure("El Building Block destino no existe.");
            }

            // 2. Read capability
            const string readSql = "SELECT idCapacidad, idBuildingBlock, nombreCapacidad, idEstadoCapacidad FROM dbo.TCapacidadDeSeguridad WHERE idCapacidad = @id";
            await using var readCmd = Create(connection, transaction.GetDbTransaction(), readSql, ("@id", command.CapabilityId));
            await using var reader = await readCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return AssignmentResult.Failure("La Capacidad de Seguridad no existe.");
            }

            var currentBbId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var name = reader.GetString(2);
            var stateId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            await reader.CloseAsync();

            if (currentBbId != command.SourceBuildingBlockId)
            {
                return AssignmentResult.ConcurrencyConflict("El Building Block origen de la Capacidad no coincide con el estado actual del registro.");
            }

            // 3. Concurrency check
            if (!AssignmentConcurrencyHelper.VerifyCapabilityToken(command.ConcurrencyToken, command.CapabilityId, currentBbId, name, stateId))
            {
                return AssignmentResult.ConcurrencyConflict();
            }

            // 4. Update
            const string updateSql = "UPDATE dbo.TCapacidadDeSeguridad SET idBuildingBlock = @targetId WHERE idCapacidad = @id";
            await using var updateCmd = Create(connection, transaction.GetDbTransaction(), updateSql,
                ("@targetId", command.TargetBuildingBlockId), ("@id", command.CapabilityId));
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            // 5. Audit
            var auditDesc = $"Reasignación de Capacidad de Building Block {command.SourceBuildingBlockId} a {command.TargetBuildingBlockId}. Justificación: {command.Justification ?? "N/A"}";
            await auditTrail.RecordRelationAsync("REASSIGN_CAPABILITY", "capacidad-seguridad", "TCapacidadDeSeguridad",
                command.CapabilityId, name, command.ActorUserId, command.CorrelationId, auditDesc, cancellationToken);

            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                ActorUserId = command.ActorUserId,
                EventType = "Capability.Reassigned",
                PermissionCode = Permissions.CatalogEdit,
                Result = "Succeeded",
                ResourceType = "TCapacidadDeSeguridad",
                ResourceId = command.CapabilityId.ToString(CultureInfo.InvariantCulture),
                BeforeJson = JsonSerializer.Serialize(new { idBuildingBlock = command.SourceBuildingBlockId }),
                AfterJson = JsonSerializer.Serialize(new { idBuildingBlock = command.TargetBuildingBlockId, justification = command.Justification }),
                CorrelationId = command.CorrelationId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AssignmentResult.Success();
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AssignmentResult> AssignFunctionalityAsync(
        AssignFunctionalityCommand command,
        CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // 1. Verify target Capability exists
            var targetCount = await ScalarIntAsync(connection, transaction.GetDbTransaction(),
                "SELECT COUNT(*) FROM dbo.TCapacidadDeSeguridad WHERE idCapacidad = @id", ("@id", command.TargetCapabilityId), cancellationToken);
            if (targetCount != 1)
            {
                return AssignmentResult.Failure("La Capacidad de Seguridad destino no existe.");
            }

            // 2. Read functionality
            const string readSql = "SELECT idFuncionalidad, idCapacidad, nombreFuncionalidad, idEstadoCoberturaFuncionalidad FROM dbo.TFuncionalidad WHERE idFuncionalidad = @id";
            await using var readCmd = Create(connection, transaction.GetDbTransaction(), readSql, ("@id", command.FunctionalityId));
            await using var reader = await readCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return AssignmentResult.Failure("La Funcionalidad no existe.");
            }

            var currentCapId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var name = reader.GetString(2);
            var stateId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            await reader.CloseAsync();

            if (currentCapId != null)
            {
                return AssignmentResult.Failure("La Funcionalidad ya se encuentra asignada a una Capacidad. Utilice la acción de reasignación.");
            }

            // 3. Concurrency check
            if (!AssignmentConcurrencyHelper.VerifyFunctionalityToken(command.ConcurrencyToken, command.FunctionalityId, currentCapId, name, stateId))
            {
                return AssignmentResult.ConcurrencyConflict();
            }

            // 4. Update
            const string updateSql = "UPDATE dbo.TFuncionalidad SET idCapacidad = @targetId WHERE idFuncionalidad = @id";
            await using var updateCmd = Create(connection, transaction.GetDbTransaction(), updateSql,
                ("@targetId", command.TargetCapabilityId), ("@id", command.FunctionalityId));
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            // 5. Audit
            var auditDesc = $"Asociación de Funcionalidad a Capacidad {command.TargetCapabilityId}. Justificación: {command.Justification ?? "N/A"}";
            await auditTrail.RecordRelationAsync("ASSOCIATE_FUNCTIONALITY", "funcionalidad", "TFuncionalidad",
                command.FunctionalityId, name, command.ActorUserId, command.CorrelationId, auditDesc, cancellationToken);

            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                ActorUserId = command.ActorUserId,
                EventType = "Functionality.Associated",
                PermissionCode = Permissions.CatalogEdit,
                Result = "Succeeded",
                ResourceType = "TFuncionalidad",
                ResourceId = command.FunctionalityId.ToString(CultureInfo.InvariantCulture),
                BeforeJson = JsonSerializer.Serialize(new { idCapacidad = (int?)null }),
                AfterJson = JsonSerializer.Serialize(new { idCapacidad = command.TargetCapabilityId, justification = command.Justification }),
                CorrelationId = command.CorrelationId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AssignmentResult.Success();
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<AssignmentResult> ReassignFunctionalityAsync(
        ReassignFunctionalityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SourceCapabilityId == command.TargetCapabilityId)
        {
            return AssignmentResult.Failure("La Funcionalidad ya pertenece a la Capacidad destino.");
        }

        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // 1. Verify target Capability exists
            var targetCount = await ScalarIntAsync(connection, transaction.GetDbTransaction(),
                "SELECT COUNT(*) FROM dbo.TCapacidadDeSeguridad WHERE idCapacidad = @id", ("@id", command.TargetCapabilityId), cancellationToken);
            if (targetCount != 1)
            {
                return AssignmentResult.Failure("La Capacidad de Seguridad destino no existe.");
            }

            // 2. Read functionality
            const string readSql = "SELECT idFuncionalidad, idCapacidad, nombreFuncionalidad, idEstadoCoberturaFuncionalidad FROM dbo.TFuncionalidad WHERE idFuncionalidad = @id";
            await using var readCmd = Create(connection, transaction.GetDbTransaction(), readSql, ("@id", command.FunctionalityId));
            await using var reader = await readCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return AssignmentResult.Failure("La Funcionalidad no existe.");
            }

            var currentCapId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var name = reader.GetString(2);
            var stateId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
            await reader.CloseAsync();

            if (currentCapId != command.SourceCapabilityId)
            {
                return AssignmentResult.ConcurrencyConflict("La Capacidad origen de la Funcionalidad no coincide con el estado actual del registro.");
            }

            // 3. Concurrency check
            if (!AssignmentConcurrencyHelper.VerifyFunctionalityToken(command.ConcurrencyToken, command.FunctionalityId, currentCapId, name, stateId))
            {
                return AssignmentResult.ConcurrencyConflict();
            }

            // 4. Update
            const string updateSql = "UPDATE dbo.TFuncionalidad SET idCapacidad = @targetId WHERE idFuncionalidad = @id";
            await using var updateCmd = Create(connection, transaction.GetDbTransaction(), updateSql,
                ("@targetId", command.TargetCapabilityId), ("@id", command.FunctionalityId));
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            // 5. Audit
            var auditDesc = $"Reasignación de Funcionalidad de Capacidad {command.SourceCapabilityId} a {command.TargetCapabilityId}. Justificación: {command.Justification ?? "N/A"}";
            await auditTrail.RecordRelationAsync("REASSIGN_FUNCTIONALITY", "funcionalidad", "TFuncionalidad",
                command.FunctionalityId, name, command.ActorUserId, command.CorrelationId, auditDesc, cancellationToken);

            dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                ActorUserId = command.ActorUserId,
                EventType = "Functionality.Reassigned",
                PermissionCode = Permissions.CatalogEdit,
                Result = "Succeeded",
                ResourceType = "TFuncionalidad",
                ResourceId = command.FunctionalityId.ToString(CultureInfo.InvariantCulture),
                BeforeJson = JsonSerializer.Serialize(new { idCapacidad = command.SourceCapabilityId }),
                AfterJson = JsonSerializer.Serialize(new { idCapacidad = command.TargetCapabilityId, justification = command.Justification }),
                CorrelationId = command.CorrelationId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AssignmentResult.Success();
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<int> ScalarIntAsync(DbConnection connection, DbTransaction transaction, string sql, (string Name, object Value) parameter, CancellationToken cancellationToken)
    {
        await using var cmd = Create(connection, transaction, sql, parameter);
        var res = await cmd.ExecuteScalarAsync(cancellationToken);
        return res is null or DBNull ? 0 : Convert.ToInt32(res, CultureInfo.InvariantCulture);
    }

    private static DbCommand Create(DbConnection connection, DbTransaction? transaction, string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = command.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            command.Parameters.Add(p);
        }
        return command;
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }
}