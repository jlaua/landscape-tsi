using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Landscape.Tsi.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Landscape.Tsi.Tests.Identity;

public sealed class IdentityUserAdministrationTests
{
    [Fact]
    public async Task Create_HasNoImplicitRoleOrScopeAndWritesHistory()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityUserAdministration>();
        var actor = Guid.NewGuid();

        var id = await service.CreateAsync(new CreateIdentityUserCommand(
            "new.user", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(30),
            actor, "Alta aprobada", "create-correlation"));

        var user = await service.FindAsync(id);
        Assert.NotNull(user);
        Assert.True(user.IsActive);
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Empty(dbContext.UserRoles.Where(item => item.UserId == id));
        Assert.Empty(dbContext.UserOrganizations.Where(item => item.UserId == id));
        Assert.Contains(dbContext.AuthorizationAuditEvents, item => item.EventType == "UserCreated");
    }

    [Fact]
    public async Task SuspendAndReactivate_ChangeStateAndPreserveAuditedHistory()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityUserAdministration>();
        var actor = Guid.NewGuid();
        var id = await service.CreateAsync(new CreateIdentityUserCommand(
            "managed.user", null, null, actor, "Alta", "correlation-create"));

        await service.SetActiveAsync(id, false, actor, "Suspensión aprobada", "correlation-suspend");
        Assert.False((await service.FindAsync(id))!.IsActive);
        await service.SetActiveAsync(id, true, actor, "Reactivación aprobada", "correlation-reactivate");

        Assert.True((await service.FindAsync(id))!.IsActive);
        var events = scope.ServiceProvider.GetRequiredService<IdentityDbContext>().AuthorizationAuditEvents.ToArray();
        Assert.Contains(events, item => item.EventType == "UserSuspended" && item.BeforeJson != item.AfterJson);
        Assert.Contains(events, item => item.EventType == "UserReactivated" && item.BeforeJson != item.AfterJson);
    }

    [Fact]
    public async Task ResetLocalPassword_UsesIdentityAndAuditsWithoutCredentialMaterial()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var service = scope.ServiceProvider.GetRequiredService<IIdentityUserAdministration>();
        var user = new IamUsuario { UserName = "reset.user", IsActive = true };
        Assert.True((await manager.CreateAsync(user, "OldPassword!3107")).Succeeded);
        var oldStamp = user.SecurityStamp;

        var errors = await service.ResetLocalPasswordAsync(new ResetLocalPasswordCommand(
            "reset.user", "NewPassword!3107", Guid.NewGuid(), "Restablecimiento aprobado", "reset-correlation"));

        Assert.Empty(errors);
        Assert.False(await manager.CheckPasswordAsync(user, "OldPassword!3107"));
        Assert.True(await manager.CheckPasswordAsync(user, "NewPassword!3107"));
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        var audit = scope.ServiceProvider.GetRequiredService<IdentityDbContext>().AuthorizationAuditEvents.Single(x => x.EventType == "LocalPasswordReset");
        Assert.DoesNotContain("Password", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hash", audit.AfterJson, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"users-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}
