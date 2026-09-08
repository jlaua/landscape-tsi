using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Tests.Identity;

public sealed class AuditRestoreServiceTests
{
    [Fact]
    public async Task Preview_DeleteWithSnapshot_IsRestorable()
    {
        await using var context = CreateContext();
        var operationId = Guid.NewGuid();
        context.AuditOperations.Add(new AuditOperation
        {
            OperationId = operationId, ActionType = "DELETE", EntityCode = "dominio",
            PhysicalTableName = "TMDominio", CorrelationId = "delete-1", Status = "SUCCESS",
            OccurredAtUtc = DateTime.UtcNow, Snapshots = [new AuditDeletedRecordSnapshot
            {
                EntityCode = "dominio", PhysicalTableName = "TMDominio",
                PrimaryKeyJson = "{\"id\":1}", ForeignKeysJson = "{}",
                RowDataJson = "{\"iddominio\":1,\"dominio\":\"Identity\"}", RestoreOrder = 0
            }]
        });
        await context.SaveChangesAsync();

        var preview = await new AuditRestoreService(context, new Access(true), Configuration()).PreviewAsync(Guid.NewGuid(), operationId);

        Assert.NotNull(preview);
        Assert.True(preview.CanUndo);
        Assert.Equal(1, preview.SnapshotCount);
    }

    [Fact]
    public async Task Preview_RestoredOperation_IsBlockedAndPermissionIsEnforced()
    {
        await using var context = CreateContext();
        var operationId = Guid.NewGuid();
        context.AuditOperations.AddRange(
            new AuditOperation
            {
                OperationId = operationId, ActionType = "DELETE", EntityCode = "dominio", CorrelationId = "delete-2", Status = "SUCCESS", OccurredAtUtc = DateTime.UtcNow,
                Snapshots = [new AuditDeletedRecordSnapshot { EntityCode = "dominio", PhysicalTableName = "TMDominio", PrimaryKeyJson = "{}", ForeignKeysJson = "{}", RowDataJson = "{}" }]
            },
            new AuditOperation { ActionType = "RESTORE", EntityCode = "dominio", CorrelationId = "restore-2", Status = "Succeeded", ReversesOperationId = operationId });
        await context.SaveChangesAsync();

        var service = new AuditRestoreService(context, new Access(true), Configuration());
        var preview = await service.PreviewAsync(Guid.NewGuid(), operationId);
        Assert.NotNull(preview);
        Assert.False(preview.CanUndo);
        Assert.Contains("ya fue restaurada", preview.BlockingReason, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await new AuditRestoreService(context, new Access(false), Configuration()).PreviewAsync(Guid.NewGuid(), operationId));
    }

    [Fact]
    public async Task Preview_ExpiredOrIncompatibleSnapshot_IsBlockedWithoutMutation()
    {
        await using var context = CreateContext();
        var expiredId = Guid.NewGuid();
        var incompatibleId = Guid.NewGuid();
        context.AuditOperations.AddRange(
            new AuditOperation
            {
                OperationId = expiredId, ActionType = "DELETE", EntityCode = "dominio", PhysicalTableName = "TMDominio",
                CorrelationId = "expired", Status = "SUCCESS", OccurredAtUtc = DateTime.UtcNow.AddDays(-31),
                Snapshots = [new AuditDeletedRecordSnapshot { EntityCode = "dominio", PhysicalTableName = "TMDominio", PrimaryKeyJson = "{}", ForeignKeysJson = "{}", RowDataJson = "{\"iddominio\":1}" }]
            },
            new AuditOperation
            {
                OperationId = incompatibleId, ActionType = "DELETE", EntityCode = "x", PhysicalTableName = "Unknown",
                CorrelationId = "incompatible", Status = "SUCCESS", OccurredAtUtc = DateTime.UtcNow,
                Snapshots = [new AuditDeletedRecordSnapshot { EntityCode = "x", PhysicalTableName = "Unknown", PrimaryKeyJson = "{}", ForeignKeysJson = "{}", RowDataJson = "{\"id\":1}" }]
            });
        await context.SaveChangesAsync();

        var service = new AuditRestoreService(context, new Access(true), Configuration());
        var expired = await service.PreviewAsync(Guid.NewGuid(), expiredId);
        var incompatible = await service.PreviewAsync(Guid.NewGuid(), incompatibleId);

        Assert.False(expired!.CanUndo);
        Assert.Contains("expiró", expired.BlockingReason, StringComparison.OrdinalIgnoreCase);
        Assert.False(incompatible!.CanUndo);
        Assert.Contains("compatible", incompatible.BlockingReason, StringComparison.OrdinalIgnoreCase);
    }

    private static IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"audit-restore-{Guid.NewGuid():N}").Options;
        return new IdentityDbContext(options);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Audit:UndoRetentionDays"] = "30"
    }).Build();

    private sealed class Access(bool allowed) : IEffectiveAccessService
    {
        public Task<bool> HasActiveCorporateScopeAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(allowed);
        public Task<bool> IsAuthorizedAsync(Guid userId, string permission, int empresaSubsidiariaId, CancellationToken cancellationToken = default) => Task.FromResult(allowed);
    }
}
