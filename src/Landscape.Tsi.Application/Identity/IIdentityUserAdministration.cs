namespace Landscape.Tsi.Application.Identity;

public sealed record IdentityUserSummary(
    Guid Id,
    string UserName,
    bool IsActive,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc);

public sealed record CreateIdentityUserCommand(
    string UserName,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc,
    Guid ActorUserId,
    string Justification,
    string CorrelationId);

public sealed record ResetLocalPasswordCommand(
    string UserName,
    string NewPassword,
    Guid ActorUserId,
    string Justification,
    string CorrelationId);

public interface IIdentityUserAdministration
{
    Task<Guid> CreateAsync(CreateIdentityUserCommand command, CancellationToken cancellationToken = default);
    Task<IdentityUserSummary?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid userId, bool isActive, Guid actorUserId, string justification, string correlationId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ResetLocalPasswordAsync(ResetLocalPasswordCommand command,
        CancellationToken cancellationToken = default);
}
