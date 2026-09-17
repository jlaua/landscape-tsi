namespace Landscape.Tsi.Application.Identity;

public sealed record AssignRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid RequestedByUserId,
    Guid ApprovedByUserId,
    Guid ExecutedByUserId,
    string Justification,
    DateTime ValidFromUtc,
    DateTime? ValidUntilUtc,
    string CorrelationId);

public interface IUserRoleAssignmentService
{
    Task AssignAsync(AssignRoleCommand command, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid userId, Guid roleId, Guid executedByUserId, string justification, string correlationId,
        CancellationToken cancellationToken = default);
}