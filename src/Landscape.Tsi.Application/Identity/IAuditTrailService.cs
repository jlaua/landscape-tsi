namespace Landscape.Tsi.Application.Identity;

public interface IAuditTrailService
{
    Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName,
        Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1,
        CancellationToken cancellationToken = default);

    Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName,
        Guid actorUserId, string correlationId, string? description = null,
        CancellationToken cancellationToken = default);

    Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId,
        string? displayName, Guid actorUserId, string correlationId, string? description = null,
        CancellationToken cancellationToken = default);

    Task<Domain.Identity.AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId,
        string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount,
        string? description = null, CancellationToken cancellationToken = default);

    void AddSnapshot(Domain.Identity.AuditOperation operation, string entityCode, string physicalTableName,
        string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder,
        bool isRoot, string? displayName = null);
}