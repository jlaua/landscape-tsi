using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class ExternalIdentityServiceTests
{
    [Fact]
    public async Task Resolve_UsesIssuerAndSubjectInsteadOfEmailOrDisplayName()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var identities = scope.ServiceProvider.GetRequiredService<IExternalIdentityService>();
        var first = new IamUsuario { UserName = "first", Email = "shared@example.test" };
        var second = new IamUsuario { UserName = "second", Email = "shared@example.test" };
        Assert.True((await users.CreateAsync(first)).Succeeded);
        Assert.True((await users.CreateAsync(second)).Succeeded);
        await identities.LinkAsync(first.Id, "https://issuer.example", "stable-subject", "CorporateOIDC");

        var resolved = await identities.ResolveActiveUserIdAsync("https://issuer.example", "stable-subject");

        Assert.Equal(first.Id, resolved);
        Assert.NotEqual(second.Id, resolved);
    }

    [Fact]
    public async Task Link_RejectsIssuerSubjectAlreadyOwnedByAnotherUser()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
        var identities = scope.ServiceProvider.GetRequiredService<IExternalIdentityService>();
        var first = new IamUsuario { UserName = "first" };
        var second = new IamUsuario { UserName = "second" };
        Assert.True((await users.CreateAsync(first)).Succeeded);
        Assert.True((await users.CreateAsync(second)).Succeeded);
        await identities.LinkAsync(first.Id, "issuer", "subject", "CorporateOIDC");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            identities.LinkAsync(second.Id, "issuer", "subject", "CorporateOIDC"));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"external-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}
