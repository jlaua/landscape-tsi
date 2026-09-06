namespace Landscape.Tsi.Application.Identity;

public sealed record BreakGlassRequest(
    Guid UserId,
    Guid ApprovedByUserId,
    int? EmpresaSubsidiariaId,
    string IncidentReference,
    string Justification,
    DateTime StartsAtUtc,
    DateTime ExpiresAtUtc,
    string CorrelationId);

public interface IBreakGlassService
{
    Task<Guid> ActivateAsync(BreakGlassRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid accessId, DateTime utcNow, CancellationToken cancellationToken = default);
}
