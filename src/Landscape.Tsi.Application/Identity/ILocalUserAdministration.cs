namespace Landscape.Tsi.Application.Identity;

public sealed record LocalUserListQuery(string? Search, string? RoleCode, bool? IsActive, int Page, int PageSize);
public sealed record LocalUserRole(Guid Id, string Code, string Name);
public sealed record LocalUserRow(Guid Id, string UserName, bool IsActive, DateTime? ValidFromUtc, DateTime? ValidUntilUtc, IReadOnlyList<LocalUserRole> Roles);
public sealed record LocalUserPage(IReadOnlyList<LocalUserRow> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
public sealed record LocalUserDetail(Guid Id, string UserName, bool IsActive, DateTime? ValidFromUtc, DateTime? ValidUntilUtc, IReadOnlyList<LocalUserRole> Roles);
public sealed record LocalRoleOption(string Code, string Name);
public sealed record CreateLocalUserCommand(string UserName, string Password, DateTime? ValidFromUtc, DateTime? ValidUntilUtc, Guid ActorUserId, string Justification, string CorrelationId);
public sealed record UpdateLocalUserCommand(Guid UserId, string UserName, DateTime? ValidFromUtc, DateTime? ValidUntilUtc, Guid ActorUserId, string Justification, string CorrelationId);
public sealed record ChangeLocalUserRoleCommand(Guid UserId, string RoleCode, bool Assign, Guid ActorUserId, string Justification, string CorrelationId);
public sealed record ResetLocalUserPasswordCommand(Guid UserId, string Password, Guid ActorUserId, string Justification, string CorrelationId);
public sealed record LocalUserAuditRow(DateTime OccurredAtUtc, string EventType, string Result, Guid? ActorUserId, string? Justification);

public interface ILocalUserAdministration
{
    Task<LocalUserPage> ListAsync(LocalUserListQuery query, CancellationToken cancellationToken = default);
    Task<LocalUserDetail?> FindAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalRoleOption>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreateLocalUserCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateLocalUserCommand command, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid userId, bool active, Guid actorUserId, string justification, string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ResetPasswordAsync(ResetLocalUserPasswordCommand command, CancellationToken cancellationToken = default);
    Task ChangeRoleAsync(ChangeLocalUserRoleCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalUserAuditRow>> GetAuditAsync(Guid userId, CancellationToken cancellationToken = default);
}
