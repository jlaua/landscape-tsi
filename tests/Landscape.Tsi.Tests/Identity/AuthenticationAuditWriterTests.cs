using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Identity;

public sealed class AuthenticationAuditWriterTests
{
    [Fact]
    public async Task Write_RecordsTimestampMechanismResultAndSafeIdentifier()
    {
        await using var context = CreateContext();
        var writer = new AuthenticationAuditWriter(context);
        var before = DateTime.UtcNow;

        await writer.WriteAsync("Login", "OAuth", "Failed", "oidc:abcdef", "correlation-id");

        var audit = Assert.Single(context.AuthenticationEvents);
        Assert.InRange(audit.OccurredAtUtc, before, DateTime.UtcNow);
        Assert.Equal("OAuth", audit.Mechanism);
        Assert.Equal("Failed", audit.Result);
        Assert.Equal("OIDC:ABCDEF", audit.UserIdentifier);
        Assert.Equal("correlation-id", audit.CorrelationId);
    }

    [Fact]
    public void AuditEntities_HaveNoCredentialOrTokenFields()
    {
        var forbiddenFragments = new[] { "Password", "Hash", "Secret", "Token", "Cookie" };
        var propertyNames = typeof(IamEventoAutenticacion).GetProperties()
            .Concat(typeof(IamEventoAuditoriaAutorizacion).GetProperties())
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name =>
            forbiddenFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
    }

    private static IdentityDbContext CreateContext() => new(
        new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"authentication-audit-{Guid.NewGuid():N}")
            .Options);
}
