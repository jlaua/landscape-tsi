namespace Landscape.Tsi.Application.Catalogs;

public sealed record DeletionDependencyNode(
    string EntityName,
    string PhysicalTableName,
    int RecordCount,
    int Depth,
    string Relationship,
    IReadOnlyList<DeletionDependencyNode> Children);

public sealed record DeletionImpactResult(
    string RootEntity,
    string RootPhysicalTableName,
    int RootId,
    string RootDisplayName,
    IReadOnlyList<DeletionDependencyNode> DirectDependents,
    IReadOnlyList<DeletionDependencyNode> RecursiveDependents,
    int TotalDependentRecords,
    int TotalRecordsToDelete,
    bool CanDelete,
    string? BlockingReason,
    DeletionDependencyNode DependencyTree,
    bool RequiresTypedConfirmation);

public sealed record DeletionExecutionResult(
    bool Succeeded,
    int TotalRecordsDeleted,
    string? ErrorMessage = null);

public interface IDeletionImpactService
{
    Task<DeletionImpactResult?> PreviewAsync(string entityCode, int rootId, CancellationToken cancellationToken = default);
    Task<DeletionExecutionResult> DeleteAsync(string entityCode, int rootId, string? confirmation, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
}

public static class DeletionImpactCalculator
{
    public static DeletionImpactResult Calculate(
        string rootEntity,
        string rootPhysicalTableName,
        int rootId,
        string rootDisplayName,
        IReadOnlyList<DeletionDependencyNode> dependents,
        int typedConfirmationThreshold)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var flattened = new List<DeletionDependencyNode>();
        var hasCycle = false;
        foreach (var node in dependents)
        {
            Flatten(node, visited, flattened, ref hasCycle);
        }

        var total = flattened.Sum(node => node.RecordCount);
        var root = new DeletionDependencyNode(rootEntity, rootPhysicalTableName, 1, 0, "Registro raíz", dependents);
        return new DeletionImpactResult(
            rootEntity,
            rootPhysicalTableName,
            rootId,
            rootDisplayName,
            dependents.Where(node => node.Depth == 1).ToArray(),
            flattened,
            total,
            total + 1,
            !hasCycle,
            hasCycle ? "No es posible calcular de forma segura la eliminación debido a una relación circular." : null,
            root,
            total + 1 > typedConfirmationThreshold);
    }

    private static void Flatten(DeletionDependencyNode node, HashSet<string> visited, ICollection<DeletionDependencyNode> flattened, ref bool hasCycle)
    {
        var key = $"{node.PhysicalTableName}:{node.Relationship}";
        if (!visited.Add(key))
        {
            hasCycle = true;
            return;
        }

        flattened.Add(node);
        foreach (var child in node.Children)
        {
            Flatten(child, visited, flattened, ref hasCycle);
        }
    }
}
