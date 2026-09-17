using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class EffectiveAccessServiceTests
{
    [Fact]
    public async Task AssignedSubsidiary_AllowsOnlyThatSubsidiary()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var (user, permission) = await SeedAccessAsync(scope.ServiceProvider, empresaSubsidiariaId: 101);
        var access = scope.ServiceProvider.GetRequiredService<IEffectiveAccessService>();

        Assert.True(await access.IsAuthorizedAsync(user.Id, permission.Code, 101));
        Assert.False(await access.IsAuthorizedAsync(user.Id, permission.Code, 202));
    }

    [Fact]
    public async Task CorporateScope_AllowsMultipleSubsidiaries()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var (user, permission) = await SeedAccessAsync(scope.ServiceProvider, corporate: true);
        var access = scope.ServiceProvider.GetRequiredService<IEffectiveAccessService>();

        Assert.True(await access.IsAuthorizedAsync(user.Id, permission.Code, 101));
        Assert.True(await access.IsAuthorizedAsync(user.Id, permission.Code, 202));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task MissingPermissionOrScope_IsDenied(bool removePermission, bool removeScope)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var (user, permission) = await SeedAccessAsync(scope.ServiceProvider, empresaSubsidiariaId: 101);
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        if (removePermission)
        {
            dbContext.RolePermissions.RemoveRange(dbContext.RolePermissions);
        }

        if (removeScope)
        {
            dbContext.UserOrganizations.RemoveRange(dbContext.UserOrganizations);
        }

        await dbContext.SaveChangesAsync();

        var access = scope.ServiceProvider.GetRequiredService<IEffectiveAccessService>();
        Assert.False(await access.IsAuthorizedAsync(user.Id, permission.Code, 101));
    }

    private static async Task<(IamUsuario User, IamPermiso Permission)> SeedAccessAsync(
        IServiceProvider services,
        int? empresaSubsidiariaId = null,
        bool corporate = false)
    {
        var users = services.GetRequiredService<UserManager<IamUsuario>>();
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        var user = new IamUsuario { UserName = $"user-{Guid.NewGuid():N}" };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        var role = new IamRol { Code = $"ROLE-{Guid.NewGuid():N}", Name = "Test role" };
        var permission = new IamPermiso { Code = $"Permission.{Guid.NewGuid():N}", Description = "Test" };
        dbContext.AddRange(role, permission);
        dbContext.RolePermissions.Add(new IamRolPermiso { Role = role, Permission = permission });
        dbContext.UserRoles.Add(new IamUsuarioRol { User = user, Role = role });
        dbContext.UserOrganizations.Add(new IamUsuarioOrganizacion
        {
            User = user,
            EmpresaSubsidiariaId = empresaSubsidiariaId,
            IsCorporateScope = corporate,
            ApprovedByUserId = Guid.NewGuid()
        });
        await dbContext.SaveChangesAsync();
        return (user, permission);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"access-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}