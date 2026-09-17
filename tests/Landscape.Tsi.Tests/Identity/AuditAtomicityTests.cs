using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Landscape.Tsi.Tests.Identity;

public sealed class AuditAtomicityTests
{
    [Fact]
    public async Task BusinessMutationAndAudit_RollBackTogetherWhenSaveFails()
    {
        var databaseName = $"atomic-{Guid.NewGuid():N}";
        var interceptor = new FailingSaveInterceptor();
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(interceptor)
            .Options;
        await using (var failingContext = new IdentityDbContext(options))
        {
            var user = new IamUsuario { UserName = "atomic-user", NormalizedUserName = "ATOMIC-USER" };
            failingContext.Users.Add(user);
            failingContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
            {
                BeneficiaryUserId = user.Id,
                EventType = "UserCreated",
                Result = "Succeeded",
                CorrelationId = "atomic-correlation"
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => failingContext.SaveChangesAsync());
        }

        interceptor.Fail = false;
        await using var verificationContext = new IdentityDbContext(options);
        Assert.Empty(verificationContext.Users);
        Assert.Empty(verificationContext.AuthorizationAuditEvents);
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public bool Fail { get; set; } = true;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            Fail
                ? ValueTask.FromException<InterceptionResult<int>>(
                    new InvalidOperationException("Fallo inducido antes de confirmar la unidad de trabajo."))
                : ValueTask.FromResult(result);
    }
}