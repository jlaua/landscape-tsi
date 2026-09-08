namespace Landscape.Tsi.Application.Identity;

public interface IEffectiveAccessService
{
    Task<bool> HasActiveCorporateScopeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsAuthorizedAsync(
        Guid userId,
        string permission,
        int empresaSubsidiariaId,
        CancellationToken cancellationToken = default);
}
