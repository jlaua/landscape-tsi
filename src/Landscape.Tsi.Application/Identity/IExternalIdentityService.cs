namespace Landscape.Tsi.Application.Identity;

public interface IExternalIdentityService
{
    Task<Guid?> ResolveActiveUserIdAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken = default);

    Task LinkAsync(
        Guid userId,
        string issuer,
        string subject,
        string provider,
        CancellationToken cancellationToken = default);
}
