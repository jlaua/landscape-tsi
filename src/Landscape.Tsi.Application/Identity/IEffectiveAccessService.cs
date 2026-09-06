namespace Landscape.Tsi.Application.Identity;

public interface IEffectiveAccessService
{
    Task<bool> IsAuthorizedAsync(
        Guid userId,
        string permission,
        int empresaSubsidiariaId,
        CancellationToken cancellationToken = default);
}
