using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class BreakGlassServiceTests
{
    [Fact]
    public async Task Activate_IsTimeBoundAndRaisesAuditableAlert()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var user = await CreateUserAsync(scope.ServiceProvider);
        var now = DateTime.UtcNow;
        var service = scope.ServiceProvider.GetRequiredService<IBreakGlassService>();
        var request = new BreakGlassRequest(
            user.Id, Guid.NewGuid(), 101, "INC-2026-001", "Recuperación de servicio",
            now, now.AddMinutes(30), "correlation-1");

        var id = await service.ActivateAsync(request);

        Assert.True(await service.IsActiveAsync(id, now.AddMinutes(1)));
        Assert.False(await service.IsActiveAsync(id, now.AddMinutes(31)));
        var audit = Assert.Single(scope.ServiceProvider.GetRequiredService<IdentityDbContext>().AuthorizationAuditEvents);
        Assert.Equal("BREAK_GLASS", audit.PermissionCode);
        Assert.Equal("AlertRaised", audit.Result);
    }

    [Fact]
    public async Task Activate_RejectsMissingJustification()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var user = await CreateUserAsync(scope.ServiceProvider);
        var now = DateTime.UtcNow;
        var request = new BreakGlassRequest(
            user.Id, Guid.NewGuid(), null, "INC-1", " ", now, now.AddMinutes(5), "correlation-2");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            scope.ServiceProvider.GetRequiredService<IBreakGlassService>().ActivateAsync(request));
    }

    [Fact]
    public async Task Activate_RejectsSelfApproval()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var user = await CreateUserAsync(scope.ServiceProvider);
        var now = DateTime.UtcNow;
        var request = new BreakGlassRequest(
            user.Id, user.Id, null, "INC-1", "Justificación", now, now.AddMinutes(5), "correlation-3");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IBreakGlassService>().ActivateAsync(request));
    }

    private static async Task<IamUsuario> CreateUserAsync(IServiceProvider services)
    {
        var user = new IamUsuario { UserName = $"break-glass-{Guid.NewGuid():N}" };
        Assert.True((await services.GetRequiredService<UserManager<IamUsuario>>().CreateAsync(user)).Succeeded);
        return user;
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"break-glass-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}
