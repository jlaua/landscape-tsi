using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class AuthorizationAuditQueryTests
{
    [Fact]
    public async Task List_ReturnsOnlyAuthorizedSubsidiaryAndAuditsView()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var user = await SeedAuthorizedUserAsync(scope.ServiceProvider, 101);
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        dbContext.AuthorizationAuditEvents.AddRange(
            EventFor(101, "Visible"),
            EventFor(202, "Hidden"));
        await dbContext.SaveChangesAsync();

        var result = await scope.ServiceProvider.GetRequiredService<IAuthorizationAuditQuery>()
            .ListAsync(user.Id, 101, "audit-query");

        Assert.Contains(result, item => item.EventType == "Visible");
        Assert.DoesNotContain(result, item => item.EventType == "Hidden");
        Assert.Contains(dbContext.AuthorizationAuditEvents, item => item.EventType == "AuditViewed");
    }

    [Fact]
    public async Task List_OutsideScope_IsDeniedWithoutReturningEvents()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var user = await SeedAuthorizedUserAsync(scope.ServiceProvider, 101);
        var query = scope.ServiceProvider.GetRequiredService<IAuthorizationAuditQuery>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.ListAsync(user.Id, 202, "denied-query"));
    }

    [Fact]
    public async Task Search_WithNoMatches_ReturnsEmptyPageWithoutException()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var user = await SeedAuthorizedUserAsync(scope.ServiceProvider, 101);
        var query = scope.ServiceProvider.GetRequiredService<IAuthorizationAuditQuery>();

        var result = await query.SearchAsync(user.Id, new AuditFilter(
            Search: "does-not-exist", EmpresaSubsidiariaId: 101,
            FromUtc: DateTime.UtcNow.AddDays(-1), ToUtc: DateTime.UtcNow.AddDays(1)));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    private static IamEventoAuditoriaAutorizacion EventFor(int subsidiaryId, string type) => new()
    {
        EmpresaSubsidiariaId = subsidiaryId,
        EventType = type,
        Result = "Succeeded",
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    private static async Task<IamUsuario> SeedAuthorizedUserAsync(IServiceProvider services, int subsidiaryId)
    {
        var user = new IamUsuario { UserName = $"auditor-{Guid.NewGuid():N}" };
        Assert.True((await services.GetRequiredService<UserManager<IamUsuario>>().CreateAsync(user)).Succeeded);
        var role = new IamRol { Code = $"AUDITOR-{Guid.NewGuid():N}", Name = "Auditor" };
        var permission = new IamPermiso { Code = Permissions.AuditView, Description = "View audit" };
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        dbContext.AddRange(role, permission);
        dbContext.RolePermissions.Add(new IamRolPermiso { Role = role, Permission = permission });
        dbContext.UserRoles.Add(new IamUsuarioRol { User = user, Role = role });
        dbContext.UserOrganizations.Add(new IamUsuarioOrganizacion
        {
            User = user,
            EmpresaSubsidiariaId = subsidiaryId,
            ApprovedByUserId = Guid.NewGuid()
        });
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"audit-query-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}