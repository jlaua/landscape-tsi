using Landscape.Tsi.Application.Identity;

namespace Landscape.Tsi.Tests.Identity;

public sealed class ResourcePolicyEvaluatorTests
{
    private readonly ResourcePolicyEvaluator evaluator = new();

    [Fact]
    public void MatchingOwnerAssignmentAndState_IsAllowed()
    {
        var actor = Guid.NewGuid();
        var context = new ResourceAuthorizationContext(actor, actor, new HashSet<Guid> { actor }, "PendingEvaluation");
        var policy = new ResourcePolicy(true, true, new HashSet<string>(StringComparer.Ordinal) { "PendingEvaluation" });

        Assert.True(evaluator.IsAuthorized(context, policy));
    }

    [Fact]
    public void UnassignedEvaluator_IsDenied()
    {
        var actor = Guid.NewGuid();
        var context = new ResourceAuthorizationContext(actor, Guid.NewGuid(), new HashSet<Guid>(), "PendingEvaluation");
        var policy = new ResourcePolicy(false, true, new HashSet<string>(StringComparer.Ordinal) { "PendingEvaluation" });

        Assert.False(evaluator.IsAuthorized(context, policy));
    }

    [Fact]
    public void ActionOutsideDeclaredState_IsDenied()
    {
        var actor = Guid.NewGuid();
        var context = new ResourceAuthorizationContext(actor, actor, new HashSet<Guid> { actor }, "Draft");
        var policy = new ResourcePolicy(true, true, new HashSet<string>(StringComparer.Ordinal) { "PendingDecision" });

        Assert.False(evaluator.IsAuthorized(context, policy));
    }

    [Fact]
    public void EmptyAllowedStateSet_DeniesByDefault()
    {
        var actor = Guid.NewGuid();
        var context = new ResourceAuthorizationContext(actor, actor, new HashSet<Guid> { actor }, "Any");
        var policy = new ResourcePolicy(false, false, new HashSet<string>());

        Assert.False(evaluator.IsAuthorized(context, policy));
    }
}
