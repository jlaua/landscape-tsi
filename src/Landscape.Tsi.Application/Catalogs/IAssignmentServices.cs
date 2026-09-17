namespace Landscape.Tsi.Application.Catalogs;

public interface IAssociationImpactService
{
    Task<CapabilityReassignmentImpactDto?> PreviewCapabilityReassignmentAsync(
        int capabilityId,
        int targetBuildingBlockId,
        CancellationToken cancellationToken = default);

    Task<FunctionalityReassignmentImpactDto?> PreviewFunctionalityReassignmentAsync(
        int functionalityId,
        int targetCapabilityId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CapabilityCandidateDto>> GetCapabilityCandidatesAsync(
        int buildingBlockId,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FunctionalityCandidateDto>> GetFunctionalityCandidatesAsync(
        int buildingBlockId,
        int? capabilityId = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<string?> GetCapabilityConcurrencyTokenAsync(
        int capabilityId,
        CancellationToken cancellationToken = default);

    Task<string?> GetFunctionalityConcurrencyTokenAsync(
        int functionalityId,
        CancellationToken cancellationToken = default);
}

public interface IAssignmentService
{
    Task<AssignmentResult> AssignCapabilityAsync(
        AssignCapabilityCommand command,
        CancellationToken cancellationToken = default);

    Task<AssignmentResult> ReassignCapabilityAsync(
        ReassignCapabilityCommand command,
        CancellationToken cancellationToken = default);

    Task<AssignmentResult> AssignFunctionalityAsync(
        AssignFunctionalityCommand command,
        CancellationToken cancellationToken = default);

    Task<AssignmentResult> ReassignFunctionalityAsync(
        ReassignFunctionalityCommand command,
        CancellationToken cancellationToken = default);
}