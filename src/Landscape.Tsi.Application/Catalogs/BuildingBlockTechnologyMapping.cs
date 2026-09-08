namespace Landscape.Tsi.Application.Catalogs;

public sealed record TechnologyMappingQuery(
    string? Search = null,
    string? MappingState = null,
    int? DomainId = null,
    int? PhaseId = null,
    int? FamilyId = null,
    bool OnlyPending = false,
    string SortBy = "building-block",
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 25);

public sealed record TechnologyMappingKpis(
    int TotalTechnologies,
    int MappedTechnologies,
    int UnmappedTechnologies,
    int TotalBuildingBlocks,
    int BuildingBlocksWithTechnology,
    int BuildingBlocksWithoutTechnology,
    int TotalRelations,
    decimal CoveragePercentage);

public sealed record BuildingBlockMappingRow(
    int BuildingBlockId,
    string BuildingBlockName,
    string? DomainName,
    string? PhaseName,
    IReadOnlyList<TechnologyOption> Technologies,
    bool IsMapped,
    string DetailRoute,
    string ManageRoute);

public sealed record BuildingBlockOption(int Id, string Name, bool Selected, string DetailRoute);
public sealed record FamilyOption(int Id, string Name);
public sealed record DomainOption(int Id, string Name);
public sealed record PhaseOption(int Id, string Name);
public sealed record TechnologyOption(int Id, string Name, string? FamilyName, string? AdoptionStateName, bool Selected, string DetailRoute);
public sealed record TechnologyMappingPage(
    IReadOnlyList<BuildingBlockMappingRow> Items,
    TechnologyMappingKpis Kpis,
    IReadOnlyList<FamilyOption> Families,
    IReadOnlyList<DomainOption> Domains,
    IReadOnlyList<PhaseOption> Phases,
    int Page,
    int PageSize,
    int TotalCount);
public sealed record TechnologyRelationResult(
    int TechnologyId,
    string TechnologyName,
    string? FamilyName,
    IReadOnlyList<BuildingBlockOption> BuildingBlocks);
public sealed record BuildingTechnologyRelationResult(
    int BuildingBlockId,
    string BuildingBlockName,
    string? DomainName,
    IReadOnlyList<TechnologyOption> Technologies);

public sealed record UnassignedTechnologyQuery(string? Search = null, int? FamilyId = null, int Page = 1, int PageSize = 25);
public sealed record UnassignedTechnologyPage(IReadOnlyList<TechnologyOption> Items, IReadOnlyList<FamilyOption> Families,
    IReadOnlyList<BuildingBlockOption> BuildingBlocks, int Page, int PageSize, int TotalCount);

public sealed record FamilyBuildingBlockBucket(int FamilyId, string FamilyName, int BuildingBlockCount);
public sealed record FamilyBuildingBlockRow(int BuildingBlockId, string BuildingBlockName, IReadOnlyList<string> TechnologyNames, int TechnologyCount, string DetailRoute);
public sealed record FamilyBuildingBlockIndicators(int BuildingBlocksWithoutTechnology, int TechnologiesWithoutBuildingBlock, int BuildingBlocksWithMultipleFamilies);
public sealed record FamilyBuildingBlockReport(
    IReadOnlyList<FamilyBuildingBlockBucket> Buckets,
    IReadOnlyList<FamilyBuildingBlockRow> Rows,
    FamilyBuildingBlockIndicators Indicators,
    int? SelectedFamilyId,
    string? SelectedFamilyName,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class TechnologyMappingConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public interface IBuildingBlockTechnologyMappingService
{
    Task<TechnologyMappingPage> ListAsync(TechnologyMappingQuery query, CancellationToken cancellationToken = default);
    Task<UnassignedTechnologyPage> ListUnassignedAsync(UnassignedTechnologyQuery query, CancellationToken cancellationToken = default);
    Task<TechnologyRelationResult?> GetTechnologyRelationsAsync(int technologyId, CancellationToken cancellationToken = default);
    Task<BuildingTechnologyRelationResult?> GetBuildingBlockRelationsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TechnologyOption>> GetTechnologyOptionsAsync(int buildingBlockId, string? search = null, int? familyId = null, CancellationToken cancellationToken = default);
    Task<FamilyBuildingBlockReport> GetFamilyBuildingBlockReportAsync(int? familyId = null, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task SaveTechnologyRelationsAsync(int technologyId, IReadOnlyCollection<int> buildingBlockIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task SaveBuildingBlockRelationsAsync(int buildingBlockId, IReadOnlyCollection<int> technologyIds, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task AssociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
    Task DisassociateAsync(int buildingBlockId, int technologyId, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default);
}
