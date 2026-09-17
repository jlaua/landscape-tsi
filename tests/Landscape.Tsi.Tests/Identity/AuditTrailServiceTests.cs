using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class AuditTrailServiceTests
{
    [Fact]
    public async Task RecordCreateAsync_PersistsOperationWhenUnitOfWorkCommits()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:InMemoryDatabaseName"] = Guid.NewGuid().ToString("N")
            })
            .Build();
        var services = new ServiceCollection();
        services.AddIdentityInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditTrailService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        await audit.RecordCreateAsync("dominio", "TMDominio", 42, "Application Security",
            Guid.NewGuid(), "test-correlation", "Creación de Dominio.");
        await dbContext.SaveChangesAsync();

        var operation = Assert.Single(dbContext.AuditOperations);
        Assert.Equal("CREATE", operation.ActionType);
        Assert.Equal("dominio", operation.EntityCode);
        Assert.Equal(42, operation.RootRecordId);
        Assert.Equal("Application Security", operation.RootDisplayName);
    }
}