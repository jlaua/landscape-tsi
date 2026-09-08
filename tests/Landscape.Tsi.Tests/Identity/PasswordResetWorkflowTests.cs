using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class PasswordResetWorkflowTests
{
    [Theory]
    [InlineData("jean")]
    [InlineData("administrador")]
    public async Task ResetAndUnlock_SupportsDifferentLocalUsers(string userName)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var user = new IamUsuario { UserName = userName, IsActive = true, AccessFailedCount = 2, LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30) };
        Assert.True((await manager.CreateAsync(user, "OldPassword!3107")).Succeeded);

        var result = await Landscape.Tsi.Infrastructure.Identity.PasswordResetWorkflow.ResetAndUnlockAsync(manager, user, "NewPassword!3107");

        Assert.True(result.Succeeded);
        Assert.True(await manager.CheckPasswordAsync(user, "NewPassword!3107"));
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public async Task ResetAndUnlock_ResetsPasswordClearsLockoutAndRotatesStamp()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var user = new IamUsuario { UserName = "locked.jean", IsActive = true, AccessFailedCount = 4, LockoutEnd = DateTimeOffset.UtcNow.AddHours(1) };
        Assert.True((await manager.CreateAsync(user, "OldPassword!3107")).Succeeded);
        var previousStamp = user.SecurityStamp;

        var result = await Landscape.Tsi.Infrastructure.Identity.PasswordResetWorkflow.ResetAndUnlockAsync(manager, user, "NewPassword!3107");

        Assert.True(result.Succeeded);
        Assert.True(await manager.CheckPasswordAsync(user, "NewPassword!3107"));
        Assert.False(await manager.CheckPasswordAsync(user, "OldPassword!3107"));
        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
        Assert.NotEqual(previousStamp, user.SecurityStamp);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"password-reset-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}
