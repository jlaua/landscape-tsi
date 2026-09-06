using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class LocalUserAdministrationTests
{
    [Fact]
    public async Task LocalUserLifecycle_UsesIdentityRolesStateAndAudit()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.BusinessRoles.Add(new IamRol { Code = SystemRoles.TsiEngineerCode, Name = SystemRoles.TsiEngineerName });
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<ILocalUserAdministration>();
        var actor = Guid.NewGuid();

        var id = await service.CreateAsync(new CreateLocalUserCommand("local.lifecycle", "ValidPassword!3107", null, null, actor, "Alta aprobada", "local-create"));
        await service.ChangeRoleAsync(new ChangeLocalUserRoleCommand(id, SystemRoles.TsiEngineerCode, true, actor, "Rol aprobado", "role-add"));
        var detail = await service.FindAsync(id);
        Assert.NotNull(detail);
        Assert.Contains(detail.Roles, role => role.Code == SystemRoles.TsiEngineerCode);

        await service.SetActiveAsync(id, false, actor, "Suspensión aprobada", "state-change");
        Assert.False((await service.FindAsync(id))!.IsActive);
        var events = db.AuthorizationAuditEvents.Where(item => item.BeneficiaryUserId == id).Select(item => item.EventType).ToArray();
        Assert.Contains("UserCreated", events);
        Assert.Contains("RoleAssigned", events);
        Assert.Contains("UserDeactivated", events);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"local-users-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}
