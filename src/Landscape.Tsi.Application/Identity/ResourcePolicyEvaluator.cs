namespace Landscape.Tsi.Application.Identity;

public sealed record ResourceAuthorizationContext(
    Guid ActorUserId,
    Guid? OwnerUserId,
    IReadOnlySet<Guid> AssignedUserIds,
    string CurrentState);

public sealed record ResourcePolicy(
    bool RequiresOwnership,
    bool RequiresAssignment,
    IReadOnlySet<string> AllowedStates);

public interface IResourcePolicyEvaluator
{
    bool IsAuthorized(ResourceAuthorizationContext context, ResourcePolicy policy);
}

public sealed class ResourcePolicyEvaluator : IResourcePolicyEvaluator
{
    public bool IsAuthorized(ResourceAuthorizationContext context, ResourcePolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context.CurrentState);

        if (policy.AllowedStates.Count == 0 || !policy.AllowedStates.Contains(context.CurrentState))
        {
            return false;
        }

        if (policy.RequiresOwnership && context.OwnerUserId != context.ActorUserId)
        {
            return false;
        }

        if (policy.RequiresAssignment && !context.AssignedUserIds.Contains(context.ActorUserId))
        {
            return false;
        }

        return true;
    }
}