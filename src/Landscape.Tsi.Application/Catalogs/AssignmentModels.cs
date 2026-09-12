namespace Landscape.Tsi.Application.Catalogs;

public enum HierarchyAssignmentStatus
{
    Unassigned,
    AssignedToCurrent,
    AssignedToOther
}

public sealed record CapabilityCandidateDto(
    int Id,
    string Name,
    string? State,
    int? CurrentBuildingBlockId,
    string? CurrentBuildingBlockName,
    HierarchyAssignmentStatus Status);

public sealed record FunctionalityCandidateDto(
    int Id,
    string Name,
    string? State,
    int? CurrentCapabilityId,
    string? CurrentCapabilityName,
    int? CurrentBuildingBlockId,
    string? CurrentBuildingBlockName,
    HierarchyAssignmentStatus Status);

public sealed record CapabilityReassignmentImpactDto(
    int CapabilityId,
    string CapabilityName,
    int? SourceBuildingBlockId,
    string? SourceBuildingBlockName,
    int TargetBuildingBlockId,
    string TargetBuildingBlockName,
    int ChildFunctionalitiesCount,
    IReadOnlyList<string> ChildFunctionalityNames,
    IReadOnlyList<string> RelatedTechnologies,
    IReadOnlyList<string> Warnings,
    string ConcurrencyToken);

public sealed record FunctionalityReassignmentImpactDto(
    int FunctionalityId,
    string FunctionalityName,
    int? SourceCapabilityId,
    string? SourceCapabilityName,
    int? SourceBuildingBlockId,
    string? SourceBuildingBlockName,
    int TargetCapabilityId,
    string TargetCapabilityName,
    int? TargetBuildingBlockId,
    string? TargetBuildingBlockName,
    bool IsCrossBuildingBlock,
    IReadOnlyList<string> Warnings,
    string ConcurrencyToken);

public sealed record AssignCapabilityCommand(
    int CapabilityId,
    int TargetBuildingBlockId,
    string ConcurrencyToken,
    string? Justification,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ReassignCapabilityCommand(
    int CapabilityId,
    int SourceBuildingBlockId,
    int TargetBuildingBlockId,
    string ConcurrencyToken,
    string? Justification,
    Guid ActorUserId,
    string CorrelationId);

public sealed record AssignFunctionalityCommand(
    int FunctionalityId,
    int TargetCapabilityId,
    string ConcurrencyToken,
    string? Justification,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ReassignFunctionalityCommand(
    int FunctionalityId,
    int SourceCapabilityId,
    int TargetCapabilityId,
    string ConcurrencyToken,
    string? Justification,
    Guid ActorUserId,
    string CorrelationId);

public sealed record AssignmentResult(
    bool Succeeded,
    string? ErrorMessage = null,
    bool IsConcurrencyConflict = false)
{
    public static AssignmentResult Success() => new(true);
    public static AssignmentResult Failure(string message) => new(false, message);
    public static AssignmentResult ConcurrencyConflict(string message = "El registro fue modificado por otro proceso. Recargue la página para continuar.") => new(false, message, true);
}