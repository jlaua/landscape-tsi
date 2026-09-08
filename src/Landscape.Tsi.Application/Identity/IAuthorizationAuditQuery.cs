namespace Landscape.Tsi.Application.Identity;

public sealed record AuthorizationAuditSummary(
    long Id,
    DateTime OccurredAtUtc,
    string EventType,
    string Result,
    Guid? ActorUserId,
    int? EmpresaSubsidiariaId);

public sealed record AuditFilter(
    string? Search = null,
    string? Action = null,
    string? Entity = null,
    Guid? ActorUserId = null,
    int? EmpresaSubsidiariaId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 25);

public sealed record AuditFilterOption(string Value, string DisplayName);
public sealed record AuditFilterOptions(
    IReadOnlyList<AuditFilterOption> Entities,
    IReadOnlyList<AuditFilterOption> Users,
    IReadOnlyList<AuditFilterOption> Subsidiaries);

public sealed record AuditEventRow(
    long Id,
    DateTime OccurredAtUtc,
    string Action,
    string Result,
    string? Entity,
    string? RecordId,
    string? RecordDisplayName,
    Guid? ActorUserId,
    string? ActorUserName,
    int? EmpresaSubsidiariaId,
    int AffectedRecordCount,
    bool CanRestore)
{
    public Guid? OperationId { get; init; }
}

public sealed record AuditPage(
    IReadOnlyList<AuditEventRow> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IAuthorizationAuditQuery
{
    Task<IReadOnlyList<AuthorizationAuditSummary>> ListAsync(
        Guid actorUserId,
        int empresaSubsidiariaId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<AuditPage> SearchAsync(
        Guid actorUserId,
        AuditFilter filter,
        CancellationToken cancellationToken = default);

    Task<AuditEventRow?> GetAsync(
        Guid actorUserId,
        long eventId,
        CancellationToken cancellationToken = default);

    Task<AuditEventRow?> GetOperationAsync(Guid actorUserId, Guid operationId, CancellationToken cancellationToken = default);

    Task<AuditFilterOptions> GetFilterOptionsAsync(Guid actorUserId, CancellationToken cancellationToken = default);
}
