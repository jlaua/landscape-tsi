using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Identity;

public sealed class AppendOnlyAuditTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorizationAudit_CannotBeEditedOrDeleted(bool delete)
    {
        await using var context = CreateContext();
        var audit = new IamEventoAuditoriaAutorizacion
        {
            EventType = "Test",
            Result = "Succeeded",
            CorrelationId = Guid.NewGuid().ToString("N")
        };
        context.AuthorizationAuditEvents.Add(audit);
        await context.SaveChangesAsync();

        if (delete)
        {
            context.AuthorizationAuditEvents.Remove(audit);
        }
        else
        {
            audit.Result = "Changed";
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task AuthenticationAudit_CannotBeEdited()
    {
        await using var context = CreateContext();
        var audit = new IamEventoAutenticacion
        {
            EventType = "Login",
            Mechanism = "Local",
            Result = "Succeeded"
        };
        context.AuthenticationEvents.Add(audit);
        await context.SaveChangesAsync();
        audit.Result = "Changed";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    private static IdentityDbContext CreateContext() => new(
        new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"append-only-{Guid.NewGuid():N}")
            .Options);
}
