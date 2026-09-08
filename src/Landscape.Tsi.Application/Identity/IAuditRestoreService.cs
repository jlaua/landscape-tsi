namespace Landscape.Tsi.Application.Identity;

public sealed record AuditRestorePreview(
    Guid OperationId,
    string Action,
    string Entity,
    int SnapshotCount,
    bool CanUndo,
    string? BlockingReason,
    DateTime? RestoredAtUtc);

public sealed record AuditRestoreResult(bool Succeeded, string Message, Guid? RestoreOperationId = null);

public interface IAuditRestoreService
{
    Task<AuditRestorePreview?> PreviewAsync(Guid actorUserId, Guid operationId, CancellationToken cancellationToken = default);
    Task<AuditRestoreResult> RestoreAsync(Guid actorUserId, Guid operationId, string correlationId, CancellationToken cancellationToken = default);
}
