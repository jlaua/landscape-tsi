namespace Landscape.Tsi.Application.Identity;

public sealed record AuthorizationAuditSummary(
    long Id,
    DateTime OccurredAtUtc,
    string EventType,
    string Result,
    Guid? ActorUserId,
    int? EmpresaSubsidiariaId);

public interface IAuthorizationAuditQuery
{
    Task<IReadOnlyList<AuthorizationAuditSummary>> ListAsync(
        Guid actorUserId,
        int empresaSubsidiariaId,
        string correlationId,
        CancellationToken cancellationToken = default);
}
