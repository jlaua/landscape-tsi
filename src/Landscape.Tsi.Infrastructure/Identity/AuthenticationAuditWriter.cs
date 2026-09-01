using Landscape.Tsi.Domain.Identity;

namespace Landscape.Tsi.Infrastructure.Identity;

public interface IAuthenticationAuditWriter
{
    Task WriteAsync(string eventType, string mechanism, string result, string? userIdentifier, string? correlationId, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationAuditWriter(IdentityDbContext dbContext) : IAuthenticationAuditWriter
{
    public async Task WriteAsync(
        string eventType,
        string mechanism,
        string result,
        string? userIdentifier,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuthenticationEvents.Add(new IamEventoAutenticacion
        {
            EventType = eventType,
            Mechanism = mechanism,
            Result = result,
            UserIdentifier = Normalize(userIdentifier),
            CorrelationId = correlationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}