using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Catalogs;

[Collection("SqlIntegration")]
public sealed class BuildingBlockTechnologyMappingServiceTests
{
    [SqlIntegrationFact]
    public async Task ListAsync_AppliesFiltersPaginationAndCalculatesKpis()
    {
        await using var scope = new SqlIntegrationDataScope();
        await using var context = CreateContext();
        var service = CreateService(context);
        var baseline = await service.ListAsync(new TechnologyMappingQuery());
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var blocks = new List<int>();
        for (var index = 0; index < 11; index++)
            blocks.Add(await scope.CreateBuildingBlockAsync(domainId, $"BB_{index:D2}"));
        var mappedTechnology = await scope.CreateTechnologyAsync(familyId, "TECH_MAPPED");
        _ = await scope.CreateTechnologyAsync(familyId, "TECH_UNMAPPED");
        await scope.AddBridgeAsync(blocks[0], mappedTechnology);

        var firstPage = await service.ListAsync(new TechnologyMappingQuery(DomainId: domainId, PageSize: 10));
        var secondPage = await service.ListAsync(new TechnologyMappingQuery(DomainId: domainId, Page: 2, PageSize: 10));
        var search = await service.ListAsync(new TechnologyMappingQuery(Search: scope.Prefix, DomainId: domainId));
        Assert.Equal(11, firstPage.TotalCount);
        Assert.Equal(firstPage.PageSize, firstPage.Items.Count);
        Assert.Single(secondPage.Items);
        Assert.Equal(11, search.TotalCount);
        Assert.Equal(baseline.Kpis.TotalBuildingBlocks + 11, firstPage.Kpis.TotalBuildingBlocks);
        Assert.Equal(baseline.Kpis.TotalTechnologies + 2, firstPage.Kpis.TotalTechnologies);
        Assert.Equal(baseline.Kpis.TotalRelations + 1, firstPage.Kpis.TotalRelations);
    }

    [SqlIntegrationFact]
    public async Task ListAsync_FiltersDomainPhasePendingAndOrdersByTechnologyCount()
    {
        await using var scope = new SqlIntegrationDataScope();
        var phases = await scope.ReadPhaseIdsAsync();
        Assert.NotEmpty(phases);
        var phaseA = phases[0];
        var phaseB = phases.Count > 1 ? phases[1] : phases[0];
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var first = await scope.CreateBuildingBlockAsync(domainId, "BB_TWO", phaseA);
        var second = await scope.CreateBuildingBlockAsync(domainId, "BB_ONE", phaseB);
        var pending = await scope.CreateBuildingBlockAsync(domainId, "BB_PENDING", phaseA);
        var techA = await scope.CreateTechnologyAsync(familyId, "TECH_A");
        var techB = await scope.CreateTechnologyAsync(familyId, "TECH_B");
        await scope.AddBridgeAsync(first, techA);
        await scope.AddBridgeAsync(first, techB);
        await scope.AddBridgeAsync(second, techA);
        await using var context = CreateContext();
        var service = CreateService(context);

        var domain = await service.ListAsync(new TechnologyMappingQuery(DomainId: domainId));
        var phase = await service.ListAsync(new TechnologyMappingQuery(DomainId: domainId, PhaseId: phaseB));
        var onlyPending = await service.ListAsync(new TechnologyMappingQuery(DomainId: domainId, OnlyPending: true));
        var ordered = await service.ListAsync(new TechnologyMappingQuery(DomainId: domainId, SortBy: "technologies", SortDirection: "desc"));
        Assert.Equal(3, domain.TotalCount);
        Assert.All(domain.Items, row => Assert.Equal(scope.Prefix + "_DOMAIN", row.DomainName));
        Assert.Contains(phase.Items, row => row.BuildingBlockId == second);
        Assert.Contains(onlyPending.Items, row => row.BuildingBlockId == pending);
        Assert.Equal(first, ordered.Items[0].BuildingBlockId);
    }

    [SqlIntegrationFact]
    public async Task UnassignedAndSingleRelationCommands_AreIdempotentAndOnlyChangeBridge()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var buildingBlockId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var technologyId = await scope.CreateTechnologyAsync(familyId, "TECH");
        scope.TrackBridge(buildingBlockId, technologyId);
        await using var context = CreateContext();
        var service = CreateService(context);
        Assert.Contains((await service.ListUnassignedAsync(new UnassignedTechnologyQuery(Search: scope.Prefix))).Items, item => item.Id == technologyId);
        await service.AssociateAsync(buildingBlockId, technologyId, Guid.NewGuid(), scope.Prefix + "_ADD");
        await service.AssociateAsync(buildingBlockId, technologyId, Guid.NewGuid(), scope.Prefix + "_ADD_AGAIN");
        Assert.DoesNotContain((await service.ListUnassignedAsync(new UnassignedTechnologyQuery(Search: scope.Prefix))).Items, item => item.Id == technologyId);
        await service.DisassociateAsync(buildingBlockId, technologyId, Guid.NewGuid(), scope.Prefix + "_REMOVE");
        Assert.Contains((await service.ListUnassignedAsync(new UnassignedTechnologyQuery(Search: scope.Prefix))).Items, item => item.Id == technologyId);
    }

    [SqlIntegrationFact]
    public async Task ListAsync_UsesBuildingBlocksAsRootsAndOnlyReturnsTheirRelatedTechnologies()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var buildingBlockId = await scope.CreateBuildingBlockAsync(domainId, "BB_ROOT");
        var technologyA = await scope.CreateTechnologyAsync(familyId, "TECH_A");
        var technologyB = await scope.CreateTechnologyAsync(familyId, "TECH_B");
        await scope.AddBridgeAsync(buildingBlockId, technologyA);
        await scope.AddBridgeAsync(buildingBlockId, technologyB);
        await using var context = CreateContext();
        var page = await CreateService(context).ListAsync(new TechnologyMappingQuery(DomainId: domainId));
        var row = Assert.Single(page.Items, item => item.BuildingBlockId == buildingBlockId);
        Assert.Equal([technologyA, technologyB], row.Technologies.Select(item => item.Id).Order());
    }

    [SqlIntegrationFact]
    public async Task GetBuildingBlockRelationsAsync_MissingBuildingBlock_ReturnsNull()
    {
        await using var context = CreateContext();
        Assert.Null(await CreateService(context).GetBuildingBlockRelationsAsync(int.MaxValue));
    }

    [SqlIntegrationFact]
    public async Task SaveTechnologyRelationsAsync_IsIdempotentAndOnlyChangesBridgeRows()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var buildingBlockId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var technologyId = await scope.CreateTechnologyAsync(familyId, "TECH");
        scope.TrackBridge(buildingBlockId, technologyId);
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.SaveTechnologyRelationsAsync(technologyId, [buildingBlockId], Guid.NewGuid(), scope.Prefix + "_SAVE");
        await service.SaveTechnologyRelationsAsync(technologyId, [buildingBlockId], Guid.NewGuid(), scope.Prefix + "_SAVE_AGAIN");
        Assert.Equal(1, await scope.CountBridgeAsync(buildingBlockId, technologyId));
    }

    [SqlIntegrationFact]
    public async Task PreexistingRelation_RemainsSingleAndSaveIsIdempotent()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var buildingBlockId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var technologyId = await scope.CreateTechnologyAsync(familyId, "TECH");
        await scope.AddBridgeAsync(buildingBlockId, technologyId);
        await using var context = CreateContext();
        await CreateService(context).SaveBuildingBlockRelationsAsync(buildingBlockId, [technologyId], Guid.NewGuid(), scope.Prefix + "_IDEMPOTENT");
        Assert.Equal(1, await scope.CountBridgeAsync(buildingBlockId, technologyId));
    }

    [SqlIntegrationFact]
    public async Task ConcurrentAssociation_PersistsAtMostOnePairAndReturnsOnlyControlledOutcomes()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var buildingBlockId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var technologyId = await scope.CreateTechnologyAsync(familyId, "TECH");
        scope.TrackBridge(buildingBlockId, technologyId);
        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();

        async Task<Exception?> RunAsync(IBuildingBlockTechnologyMappingService service, string correlation)
        {
            try { await service.AssociateAsync(buildingBlockId, technologyId, Guid.NewGuid(), correlation); return null; }
            catch (Exception exception) { return exception; }
        }

        var outcomes = await Task.WhenAll(
            Task.Run(() => RunAsync(CreateService(firstContext), scope.Prefix + "_RACE_A")),
            Task.Run(() => RunAsync(CreateService(secondContext), scope.Prefix + "_RACE_B")));
        Assert.All(outcomes.Where(exception => exception is not null), exception => Assert.IsType<TechnologyMappingConflictException>(exception));
        Assert.Equal(1, await scope.CountBridgeAsync(buildingBlockId, technologyId));
    }

    private static IdentityDbContext CreateContext() => new(new DbContextOptionsBuilder<IdentityDbContext>()
        .UseSqlServer(SqlIntegrationTestSettings.ConnectionString).Options);

    private static BuildingBlockTechnologyMappingService CreateService(IdentityDbContext context) => new(context, new NoOpAuditTrail());

    private sealed class NoOpAuditTrail : IAuditTrailService
    {
        public Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Landscape.Tsi.Domain.Identity.AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void AddSnapshot(Landscape.Tsi.Domain.Identity.AuditOperation operation, string entityCode, string physicalTableName, string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder, bool isRoot, string? displayName = null) => throw new NotSupportedException();
    }
}