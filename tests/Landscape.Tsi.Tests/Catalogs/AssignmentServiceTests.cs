using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Catalogs;

[Collection("SqlIntegration")]
public sealed class AssignmentServiceTests
{
    [SqlIntegrationFact]
    public async Task PreviewCapabilityReassignment_CalculatesImpactAndChildCount()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var sourceBbId = await scope.CreateBuildingBlockAsync(domainId, "BB_SRC");
        var targetBbId = await scope.CreateBuildingBlockAsync(domainId, "BB_TGT");
        var capId = await scope.CreateCapabilityAsync(sourceBbId, "CAP_TEST");
        var func1 = await scope.CreateFunctionalityAsync(capId, "FUNC_1");
        var func2 = await scope.CreateFunctionalityAsync(capId, "FUNC_2");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);

        var impact = await impactService.PreviewCapabilityReassignmentAsync(capId, targetBbId);
        Assert.NotNull(impact);
        Assert.Equal(capId, impact.CapabilityId);
        Assert.Equal(sourceBbId, impact.SourceBuildingBlockId);
        Assert.Equal(targetBbId, impact.TargetBuildingBlockId);
        Assert.Equal(2, impact.ChildFunctionalitiesCount);
        Assert.Contains(impact.Warnings, w => w.Contains("2 funcionalidad(es)"));
        Assert.NotEmpty(impact.ConcurrencyToken);
    }

    [SqlIntegrationFact]
    public async Task PreviewFunctionalityReassignment_DetectsCrossBuildingBlock()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var bb1 = await scope.CreateBuildingBlockAsync(domainId, "BB_1");
        var bb2 = await scope.CreateBuildingBlockAsync(domainId, "BB_2");
        var cap1 = await scope.CreateCapabilityAsync(bb1, "CAP_1");
        var cap2 = await scope.CreateCapabilityAsync(bb2, "CAP_2");
        var func = await scope.CreateFunctionalityAsync(cap1, "FUNC");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);

        var impact = await impactService.PreviewFunctionalityReassignmentAsync(func, cap2);
        Assert.NotNull(impact);
        Assert.True(impact.IsCrossBuildingBlock);
        Assert.Contains(impact.Warnings, w => w.Contains("cambiará de Building Block"));
    }

    [SqlIntegrationFact]
    public async Task AssignCapability_OrphanCapability_SuccessfullyAssigned()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var bbId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var capId = await scope.CreateCapabilityAsync(null, "CAP_ORPHAN");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);
        var token = await impactService.GetCapabilityConcurrencyTokenAsync(capId);
        Assert.NotNull(token);

        var service = new AssignmentService(context, new NoOpAuditTrail());
        var command = new AssignCapabilityCommand(capId, bbId, token, "Asignación inicial", Guid.NewGuid(), scope.Prefix + "_ASSIGN");
        var result = await service.AssignCapabilityAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(bbId, await scope.ReadCapabilityBuildingBlockIdAsync(capId));
    }

    [SqlIntegrationFact]
    public async Task ReassignCapability_PreservesChildFunctionalitiesAndUpdatesParent()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var sourceBb = await scope.CreateBuildingBlockAsync(domainId, "BB_SRC");
        var targetBb = await scope.CreateBuildingBlockAsync(domainId, "BB_TGT");
        var capId = await scope.CreateCapabilityAsync(sourceBb, "CAP_TO_REASSIGN");
        var funcId = await scope.CreateFunctionalityAsync(capId, "FUNC_CHILD");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);
        var token = await impactService.GetCapabilityConcurrencyTokenAsync(capId);
        Assert.NotNull(token);

        var service = new AssignmentService(context, new NoOpAuditTrail());
        var command = new ReassignCapabilityCommand(capId, sourceBb, targetBb, token, "Reorganización", Guid.NewGuid(), scope.Prefix + "_REASSIGN");
        var result = await service.ReassignCapabilityAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(targetBb, await scope.ReadCapabilityBuildingBlockIdAsync(capId));
        // Child functionality remains connected to capability
        Assert.Equal(capId, await scope.ReadFunctionalityCapabilityIdAsync(funcId));
    }

    [SqlIntegrationFact]
    public async Task ReassignCapability_DetectsConcurrencyConflict_WhenTokenIsStale()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var sourceBb = await scope.CreateBuildingBlockAsync(domainId, "BB_SRC");
        var targetBb = await scope.CreateBuildingBlockAsync(domainId, "BB_TGT");
        var capId = await scope.CreateCapabilityAsync(sourceBb, "CAP_CONFLICT");

        await using var context = CreateContext();
        var service = new AssignmentService(context, new NoOpAuditTrail());
        var staleToken = "stale-concurrency-token";

        var command = new ReassignCapabilityCommand(capId, sourceBb, targetBb, staleToken, "Reorganización", Guid.NewGuid(), scope.Prefix + "_CONFLICT");
        var result = await service.ReassignCapabilityAsync(command);

        Assert.False(result.Succeeded);
        Assert.True(result.IsConcurrencyConflict);
        Assert.Equal(sourceBb, await scope.ReadCapabilityBuildingBlockIdAsync(capId));
    }

    [SqlIntegrationFact]
    public async Task AssignFunctionality_OrphanFunctionality_SuccessfullyAssigned()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var bbId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var capId = await scope.CreateCapabilityAsync(bbId, "CAP");
        var funcId = await scope.CreateFunctionalityAsync(null, "FUNC_ORPHAN");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);
        var token = await impactService.GetFunctionalityConcurrencyTokenAsync(funcId);
        Assert.NotNull(token);

        var service = new AssignmentService(context, new NoOpAuditTrail());
        var command = new AssignFunctionalityCommand(funcId, capId, token, "Asociar orphan", Guid.NewGuid(), scope.Prefix + "_FUNC_ASSIGN");
        var result = await service.AssignFunctionalityAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(capId, await scope.ReadFunctionalityCapabilityIdAsync(funcId));
    }

    [SqlIntegrationFact]
    public async Task ReassignFunctionality_CrossCapability_SuccessfullyReassigned()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var bbId = await scope.CreateBuildingBlockAsync(domainId, "BB");
        var cap1 = await scope.CreateCapabilityAsync(bbId, "CAP_1");
        var cap2 = await scope.CreateCapabilityAsync(bbId, "CAP_2");
        var funcId = await scope.CreateFunctionalityAsync(cap1, "FUNC");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);
        var token = await impactService.GetFunctionalityConcurrencyTokenAsync(funcId);
        Assert.NotNull(token);

        var service = new AssignmentService(context, new NoOpAuditTrail());
        var command = new ReassignFunctionalityCommand(funcId, cap1, cap2, token, "Cambio de capacidad", Guid.NewGuid(), scope.Prefix + "_FUNC_REASSIGN");
        var result = await service.ReassignFunctionalityAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(cap2, await scope.ReadFunctionalityCapabilityIdAsync(funcId));
    }

    [SqlIntegrationFact]
    public async Task ReassignCapability_FailingAudit_RollsBackTransaction_FailClosed()
    {
        await using var scope = new SqlIntegrationDataScope();
        var domainId = await scope.CreateDomainAsync();
        var sourceBb = await scope.CreateBuildingBlockAsync(domainId, "BB_SRC");
        var targetBb = await scope.CreateBuildingBlockAsync(domainId, "BB_TGT");
        var capId = await scope.CreateCapabilityAsync(sourceBb, "CAP_FAIL_CLOSED");

        await using var context = CreateContext();
        var impactService = new AssociationImpactService(context);
        var token = await impactService.GetCapabilityConcurrencyTokenAsync(capId);
        Assert.NotNull(token);

        var failingAuditTrail = new FailingAuditTrail();
        var service = new AssignmentService(context, failingAuditTrail);

        var command = new ReassignCapabilityCommand(capId, sourceBb, targetBb, token, "Fallo inducido", Guid.NewGuid(), scope.Prefix + "_FAIL_AUDIT");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReassignCapabilityAsync(command));

        // Verify that database was NOT modified (rolled back to sourceBb)
        Assert.Equal(sourceBb, await scope.ReadCapabilityBuildingBlockIdAsync(capId));
    }

    private static IdentityDbContext CreateContext() => new(new DbContextOptionsBuilder<IdentityDbContext>()
        .UseSqlServer(SqlIntegrationTestSettings.ConnectionString).Options);

    private sealed class NoOpAuditTrail : IAuditTrailService
    {
        public Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void AddSnapshot(AuditOperation operation, string entityCode, string physicalTableName, string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder, bool isRoot, string? displayName = null) => throw new NotImplementedException();
    }

    private sealed class FailingAuditTrail : IAuditTrailService
    {
        public Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Fallo simulado en el servicio de auditoría inmutable.");
        public Task<AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void AddSnapshot(AuditOperation operation, string entityCode, string physicalTableName, string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder, bool isRoot, string? displayName = null) => throw new NotImplementedException();
    }
}