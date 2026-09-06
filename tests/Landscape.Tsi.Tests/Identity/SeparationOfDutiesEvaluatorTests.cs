using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Tests.Identity;

public sealed class SeparationOfDutiesEvaluatorTests
{
    public static TheoryData<SegregatedOperation> Operations => new()
    {
        SegregatedOperation.ApproveRequest,
        SegregatedOperation.ValidateCriticalEvidence,
        SegregatedOperation.ApproveException,
        SegregatedOperation.ApproveAccessElevation
    };

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task ConflictingActor_IsDeniedAndAudited(SegregatedOperation operation)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var actor = Guid.NewGuid();
        var context = operation switch
        {
            SegregatedOperation.ApproveRequest => new SeparationOfDutiesContext(actor, RequesterUserId: actor),
            SegregatedOperation.ValidateCriticalEvidence => new SeparationOfDutiesContext(actor, EvidenceSubmittedByUserId: actor),
            SegregatedOperation.ApproveException => new SeparationOfDutiesContext(actor, ExceptionRequestedByUserId: actor),
            SegregatedOperation.ApproveAccessElevation => new SeparationOfDutiesContext(actor, AccessBeneficiaryUserId: actor),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        var evaluator = scope.ServiceProvider.GetRequiredService<ISeparationOfDutiesEvaluator>();

        Assert.False(await evaluator.IsAuthorizedAsync(operation, context));
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var audit = Assert.Single(dbContext.AuthorizationAuditEvents);
        Assert.Equal(actor, audit.ActorUserId);
        Assert.Equal("DeniedSeparationOfDuties", audit.Result);
        Assert.Contains(operation.ToString(), audit.Justification, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IndependentActor_IsAllowedWithoutDenialEvent()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var evaluator = scope.ServiceProvider.GetRequiredService<ISeparationOfDutiesEvaluator>();
        var context = new SeparationOfDutiesContext(Guid.NewGuid(), RequesterUserId: Guid.NewGuid());

        Assert.True(await evaluator.IsAuthorizedAsync(SegregatedOperation.ApproveRequest, context));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<IdentityDbContext>().AuthorizationAuditEvents);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Identity:InMemoryDatabaseName"] = $"sod-{Guid.NewGuid():N}" }).Build());
        return services.BuildServiceProvider();
    }
}
