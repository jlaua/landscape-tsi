using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Landscape.Tsi.Tests.Identity;

public sealed class BootstrapAdminInitializerTests
{
    [Fact]
    public async Task InitializeAsync_Enabled_CreatesTwoAdministratorsIdempotently()
    {
        await using var provider = CreateProvider(enabled: true, includePassword: true);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        var initializer = scope.ServiceProvider.GetRequiredService<BootstrapAdminInitializer>();

        await initializer.InitializeAsync();
        var hashesBefore = await db.Users.OrderBy(x => x.UserName).Select(x => x.PasswordHash).ToArrayAsync();
        await initializer.InitializeAsync();

        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(hashesBefore, await db.Users.OrderBy(x => x.UserName).Select(x => x.PasswordHash).ToArrayAsync());
        Assert.All(await db.Users.ToListAsync(), user => Assert.True(user.BootstrapManaged));
        Assert.All(await db.Users.ToListAsync(), user => Assert.NotEqual(CreatePassword(), user.PasswordHash));
        var role = await db.BusinessRoles.SingleAsync(x => x.Code == SystemRoles.AdministratorCode);
        Assert.Equal(SystemRoles.AdministratorName, role.Name);
        Assert.Equal(SystemRoles.InitialRoles.Count, await db.BusinessRoles.CountAsync());
        Assert.All(SystemRoles.InitialRoles, expected =>
            Assert.Contains(db.BusinessRoles, actual => actual.Code == expected.Key && actual.Name == expected.Value));
        Assert.Equal(2, await db.UserRoles.CountAsync(x => x.RoleId == role.Id));
    }

    [Fact]
    public async Task InitializeAsync_MissingPassword_DoesNotCreateUsers()
    {
        await using var provider = CreateProvider(enabled: true, includePassword: false);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();

        await scope.ServiceProvider.GetRequiredService<BootstrapAdminInitializer>().InitializeAsync();

        Assert.Empty(await db.Users.ToListAsync());
        Assert.Contains(await db.AuthenticationEvents.ToListAsync(), x => x.Result == "SkippedMissingSecret");
    }

    [Fact]
    public async Task InitializeAsync_ManualUser_DoesNotElevateOrReplacePassword()
    {
        await using var provider = CreateProvider(enabled: true, includePassword: true);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var manualPassword = CreatePassword();
        var manual = new IamUsuario { UserName = "jean", BootstrapManaged = false };
        Assert.True((await manager.CreateAsync(manual, manualPassword)).Succeeded);
        var originalHash = manual.PasswordHash;

        await scope.ServiceProvider.GetRequiredService<BootstrapAdminInitializer>().InitializeAsync();

        var persisted = await manager.FindByNameAsync("jean");
        Assert.Equal(originalHash, persisted!.PasswordHash);
        Assert.False(await db.UserRoles.AnyAsync(x => x.UserId == persisted.Id));
        Assert.NotNull(await manager.FindByNameAsync("administrador"));
    }

    [Fact]
    public async Task InitializeAsync_ProductionWithoutAuthorization_DoesNotCreateUsers()
    {
        await using var provider = CreateProvider(enabled: true, includePassword: true, environmentName: Environments.Production);
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();

        await scope.ServiceProvider.GetRequiredService<BootstrapAdminInitializer>().InitializeAsync();

        Assert.Empty(await db.Users.ToListAsync());
    }

    private static ServiceProvider CreateProvider(bool enabled, bool includePassword, string environmentName = "Development")
    {
        var settings = new Dictionary<string, string?>
        {
            ["BootstrapAdmin:Enabled"] = enabled.ToString(),
            ["BootstrapAdmin:AllowedEnvironments:0"] = "Development",
            ["Identity:InMemoryDatabaseName"] = $"test-{Guid.NewGuid():N}"
        };
        if (includePassword)
        {
            settings["BootstrapAdmin:Password"] = CreatePassword();
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestEnvironment { EnvironmentName = environmentName });
        services.AddIdentityInfrastructure(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static string CreatePassword() => $"Aa1!{Guid.NewGuid():N}";

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Landscape.Tsi.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}