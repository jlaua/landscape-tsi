using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class UserRoleAssignmentServiceTests
{
    [Fact]
    public async Task AssignAndRevoke_PreserveActorsVigencyAndAudit()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var (user, role) = await SeedAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<IUserRoleAssignmentService>();
        var requester = Guid.NewGuid();
        var approver = Guid.NewGuid();
        var executor = Guid.NewGuid();
        var starts = DateTime.UtcNow.AddMinutes(-1);
        await service.AssignAsync(new AssignRoleCommand(
            user.Id, role.Id, requester, approver, executor, "Asignación aprobada",
            starts, DateTime.UtcNow.AddDays(10), "assign-correlation"));

        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var assignment = Assert.Single(dbContext.UserRoles);
        Assert.Equal(requester, assignment.RequestedByUserId);
        Assert.Equal(approver, assignment.ApprovedByUserId);
        Assert.Equal(executor, assignment.ExecutedByUserId);

        await service.RevokeAsync(user.Id, role.Id, executor, "Revocación aprobada", "revoke-correlation");

        Assert.False(assignment.IsEffectiveAt(DateTime.UtcNow.AddSeconds(1)));
        Assert.Contains(dbContext.AuthorizationAuditEvents, item => item.EventType == "RoleAssigned");
        Assert.Contains(dbContext.AuthorizationAuditEvents, item => item.EventType == "RoleRevoked");
    }

    [Fact]
    public async Task Assign_RejectsBeneficiaryAsApprover()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var (user, role) = await SeedAsync(scope.ServiceProvider);
        var command = new AssignRoleCommand(
            user.Id, role.Id, Guid.NewGuid(), user.Id, Guid.NewGuid(), "Intento",
            DateTime.UtcNow, null, "correlation");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IUserRoleAssignmentService>().AssignAsync(command));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IdentityDbContext>().UserRoles);
    }

    private static async Task<(IamUsuario User, IamRol Role)> SeedAsync(IServiceProvider services)
    {
        var user = new IamUsuario { UserName = $"assignment-{Guid.NewGuid():N}" };
        Assert.True((await services.GetRequiredService<UserManager<IamUsuario>>().CreateAsync(user)).Succeeded);
        var role = new IamRol { Code = $"ROLE-{Guid.NewGuid():N}", Name = "Role" };
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        dbContext.BusinessRoles.Add(role);
        await dbContext.SaveChangesAsync();
        return (user, role);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"roles-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}