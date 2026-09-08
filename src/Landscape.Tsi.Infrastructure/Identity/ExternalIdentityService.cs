using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class ExternalIdentityService(IdentityDbContext dbContext) : IExternalIdentityService
{
    public Task<Guid?> ResolveActiveUserIdAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ValidateStableIdentifier(issuer, subject);
        var utcNow = DateTime.UtcNow;

        return dbContext.ExternalLogins
            .Where(login => login.Issuer == issuer && login.Subject == subject)
            .Where(login => login.User.IsActive)
            .Where(login => login.User.ValidFromUtc == null || login.User.ValidFromUtc <= utcNow)
            .Where(login => login.User.ValidUntilUtc == null || login.User.ValidUntilUtc > utcNow)
            .Select(login => (Guid?)login.UserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task LinkAsync(
        Guid userId,
        string issuer,
        string subject,
        string provider,
        CancellationToken cancellationToken = default)
    {
        ValidateStableIdentifier(issuer, subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        if (!await dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken))
        {
            throw new InvalidOperationException("El usuario interno no existe.");
        }

        var existing = await dbContext.ExternalLogins
            .SingleOrDefaultAsync(login => login.Issuer == issuer && login.Subject == subject, cancellationToken);
        if (existing is not null)
        {
            if (existing.UserId == userId)
            {
                return;
            }

            throw new InvalidOperationException("La identidad externa ya está vinculada a otro usuario.");
        }

        dbContext.ExternalLogins.Add(new IamUsuarioLoginExterno
        {
            UserId = userId,
            LoginProvider = provider,
            ProviderKey = $"{issuer}|{subject}",
            ProviderDisplayName = provider,
            Issuer = issuer,
            Subject = subject
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateStableIdentifier(string issuer, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
    }
}