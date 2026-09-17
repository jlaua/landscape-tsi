using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class AuditTrailService(IdentityDbContext dbContext) : IAuditTrailService
{
    public async Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName,
        Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1,
        CancellationToken cancellationToken = default) =>
        await AddOperationAsync("CREATE", entityCode, physicalTableName, recordId, displayName, actorUserId,
            correlationId, affectedRecordCount, description, cancellationToken);

    public async Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName,
        Guid actorUserId, string correlationId, string? description = null,
        CancellationToken cancellationToken = default) =>
        await AddOperationAsync("UPDATE", entityCode, physicalTableName, recordId, displayName, actorUserId,
            correlationId, 1, description, cancellationToken);

    public async Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId,
        string? displayName, Guid actorUserId, string correlationId, string? description = null,
        CancellationToken cancellationToken = default) =>
        await AddOperationAsync(actionType, entityCode, physicalTableName, recordId, displayName, actorUserId,
            correlationId, 1, description, cancellationToken);

    public async Task<AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId,
        string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount,
        string? description = null, CancellationToken cancellationToken = default)
    {
        var operation = new AuditOperation
        {
            CorrelationId = correlationId,
            ActionType = "DELETE",
            EntityCode = entityCode,
            PhysicalTableName = physicalTableName,
            RootRecordId = recordId,
            RootDisplayName = displayName,
            ActorUserId = actorUserId,
            ActorUserNameSnapshot = await dbContext.Users.AsNoTracking()
                .Where(user => user.Id == actorUserId).Select(user => user.UserName).SingleOrDefaultAsync(cancellationToken),
            OccurredAtUtc = DateTime.UtcNow,
            Description = description,
            AffectedRecordCount = affectedRecordCount,
            Status = "SUCCESS"
        };
        dbContext.AuditOperations.Add(operation);
        return operation;
    }

    public void AddSnapshot(AuditOperation operation, string entityCode, string physicalTableName,
        string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder,
        bool isRoot, string? displayName = null) =>
        operation.Snapshots.Add(new AuditDeletedRecordSnapshot
        {
            Operation = operation,
            EntityCode = entityCode,
            PhysicalTableName = physicalTableName,
            PrimaryKeyJson = primaryKeyJson,
            ForeignKeysJson = foreignKeysJson,
            RowDataJson = rowDataJson,
            DeleteOrder = deleteOrder,
            RestoreOrder = restoreOrder,
            IsRoot = isRoot,
            DisplayName = displayName
        });

    private async Task AddOperationAsync(string actionType, string entityCode, string physicalTableName, long recordId,
        string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description,
        CancellationToken cancellationToken)
    {
        dbContext.AuditOperations.Add(new AuditOperation
        {
            CorrelationId = correlationId,
            ActionType = actionType,
            EntityCode = entityCode,
            PhysicalTableName = physicalTableName,
            RootRecordId = recordId,
            RootDisplayName = displayName,
            ActorUserId = actorUserId,
            ActorUserNameSnapshot = await dbContext.Users.AsNoTracking()
                .Where(user => user.Id == actorUserId).Select(user => user.UserName).SingleOrDefaultAsync(cancellationToken),
            OccurredAtUtc = DateTime.UtcNow,
            Description = description,
            AffectedRecordCount = affectedRecordCount,
            Status = "SUCCESS"
        });
    }
}