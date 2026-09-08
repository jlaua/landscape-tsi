namespace Landscape.Tsi.Application.Identity;

public sealed record OrganizationScopeRow(
    Guid Id,
    bool IsCorporateScope,
    int? EmpresaSubsidiariaId,
    DateTime ValidFromUtc,
    DateTime? ValidUntilUtc,
    Guid ApprovedByUserId,
    string? Justification);

public sealed record OrganizationScopeApprover(Guid Id, string UserName);

public sealed record AssignCorporateScopeCommand(
    Guid UserId,
    Guid RequestedByUserId,
    Guid ApprovedByUserId,
    DateTime ValidFromUtc,
    DateTime? ValidUntilUtc,
    string Justification,
    string CorrelationId);

public interface IOrganizationScopeAdministration
{
    Task<IReadOnlyList<OrganizationScopeRow>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationScopeApprover>> ListApproversAsync(Guid beneficiaryUserId, CancellationToken cancellationToken = default);
    Task AssignCorporateAsync(AssignCorporateScopeCommand command, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid scopeId, Guid executedByUserId, string justification, string correlationId, CancellationToken cancellationToken = default);
}
