using System.Text.Json;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Tests.Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Tests.Identity;

[Collection("SqlIntegration")]
public sealed class AuditRestoreSqlTests
{
    [SqlIntegrationFact]
    public async Task Restore_UsesNewIdentityAndPersistsKeyMap_WithoutSecondRestore()
    {
        await RequireTablesAsync("audit.Operation", "audit.RecordSnapshot", "audit.RecordKeyMap", "dbo.TMDominio");
        var testRunId = $"ITEST_{Guid.NewGuid():N}";
        var deleteCorrelationId = testRunId + "_DELETE";
        var restoreCorrelationId = testRunId + "_RESTORE";
        var operationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var domainName = testRunId + "_DOMAIN";
        var buildingBlockName = testRunId + "_BB";
        var originalId = await CreateDomainAsync(domainName);
        var originalBuildingBlockId = await CreateBuildingBlockAsync(originalId, buildingBlockName);
        int? restoredId = null;
        int? restoredBuildingBlockId = null;

        try
        {
            await DeleteDomainTreeAsync(originalId, originalBuildingBlockId);
            await SeedDeleteOperationAsync(operationId, deleteCorrelationId, originalId, domainName,
                originalBuildingBlockId, buildingBlockName);

            await using var context = CreateContext();
            var service = new AuditRestoreService(context, new Access(), Configuration());
            var result = await service.RestoreAsync(actorId, operationId, restoreCorrelationId);

            Assert.True(result.Succeeded, result.Message);
            Assert.NotNull(result.RestoreOperationId);
            restoredId = await ReadDomainIdAsync(domainName);
            Assert.NotNull(restoredId);
            Assert.NotEqual(originalId, restoredId.Value);
            var restoredBuildingBlock = await ReadBuildingBlockAsync(buildingBlockName);
            Assert.NotNull(restoredBuildingBlock);
            restoredBuildingBlockId = restoredBuildingBlock.Value.Id;
            Assert.NotEqual(originalBuildingBlockId, restoredBuildingBlockId.Value);
            Assert.Equal(restoredId.Value, restoredBuildingBlock.Value.DomainId);

            var domainKeyMap = await ReadKeyMapAsync(operationId, "TMDominio");
            Assert.NotNull(domainKeyMap);
            Assert.Equal(originalId, ReadMappedId(domainKeyMap.Value.OldPrimaryKeyJson));
            Assert.Equal(restoredId.Value, ReadMappedId(domainKeyMap.Value.NewPrimaryKeyJson));
            var buildingBlockKeyMap = await ReadKeyMapAsync(operationId, "TBuildingBlock");
            Assert.NotNull(buildingBlockKeyMap);
            Assert.Equal(originalBuildingBlockId, ReadMappedId(buildingBlockKeyMap.Value.OldPrimaryKeyJson));
            Assert.Equal(restoredBuildingBlockId.Value, ReadMappedId(buildingBlockKeyMap.Value.NewPrimaryKeyJson));

            var restore = await ReadRestoreOperationAsync(operationId, restoreCorrelationId);
            Assert.NotNull(restore);
            Assert.Equal(result.RestoreOperationId, restore.Value.OperationId);
            Assert.Equal("Succeeded", restore.Value.Status);

            var second = await service.RestoreAsync(actorId, operationId, testRunId + "_RESTORE_AGAIN");
            Assert.False(second.Succeeded);
            Assert.Contains("ya fue restaurada", second.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, await CountKeyMapsAsync(operationId));
            Assert.Equal(1, await CountRestoresAsync(operationId));
        }
        finally
        {
            if (restoredId.HasValue && restoredBuildingBlockId.HasValue)
                await DeleteDomainTreeAsync(restoredId.Value, restoredBuildingBlockId.Value);
            await DeleteDomainIfOwnedAsync(originalId, domainName);
        }
    }

    private static IdentityDbContext CreateContext() => new(
        new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(SqlIntegrationTestSettings.ConnectionString)
            .Options);

    private static async Task<int> CreateDomainAsync(string domainName)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(
            "INSERT dbo.TMDominio (dominio,descripcionDominio) OUTPUT INSERTED.iddominio VALUES (@name,@description)", connection);
        command.Parameters.AddWithValue("@name", domainName);
        command.Parameters.AddWithValue("@description", "Registro temporal de integración para restore.");
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<int> CreateBuildingBlockAsync(int domainId, string buildingBlockName)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(
            "INSERT dbo.TBuildingBlock (nombreBuildingBlock,definicionBuildingBlock,idDominio) OUTPUT INSERTED.idBuildingBlock VALUES (@name,@definition,@domainId)", connection);
        command.Parameters.AddWithValue("@name", buildingBlockName);
        command.Parameters.AddWithValue("@definition", "Registro temporal de integración para restore.");
        command.Parameters.AddWithValue("@domainId", domainId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task DeleteDomainTreeAsync(int domainId, int buildingBlockId)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await using var command = new SqlCommand("""
            DELETE FROM dbo.TBuildingBlock WHERE idBuildingBlock=@buildingBlockId AND idDominio=@domainId;
            DELETE FROM dbo.TMDominio WHERE iddominio=@domainId;
            """, connection, transaction);
        command.Parameters.AddWithValue("@buildingBlockId", buildingBlockId);
        command.Parameters.AddWithValue("@domainId", domainId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static async Task DeleteDomainIfOwnedAsync(int domainId, string domainName)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(
            "DELETE FROM dbo.TMDominio WHERE iddominio=@id AND dominio=@name", connection);
        command.Parameters.AddWithValue("@id", domainId);
        command.Parameters.AddWithValue("@name", domainName);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SeedDeleteOperationAsync(Guid operationId, string correlationId, int originalId,
        string domainName, int originalBuildingBlockId, string buildingBlockName)
    {
        var domainRowJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["iddominio"] = originalId,
            ["dominio"] = domainName,
            ["descripcionDominio"] = "Registro temporal de integración para restore."
        });
        var buildingBlockRowJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["idBuildingBlock"] = originalBuildingBlockId,
            ["nombreBuildingBlock"] = buildingBlockName,
            ["definicionBuildingBlock"] = "Registro temporal de integración para restore.",
            ["idDominio"] = originalId,
            ["idEstadoFaseDeAdopcionBuildingBlock"] = null,
            ["rutaDelEntregable"] = null,
            ["PilarZT"] = null
        });
        var domainKeyJson = JsonSerializer.Serialize(new { id = originalId });
        var buildingBlockKeyJson = JsonSerializer.Serialize(new { id = originalBuildingBlockId });
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new SqlCommand("""
            INSERT INTO audit.Operation
                (OperationId,CorrelationId,ActionType,EntityCode,PhysicalTableName,RootRecordId,RootDisplayName,OccurredAtUtc,AffectedRecordCount,Status,SchemaVersion)
            VALUES (@operationId,@correlationId,N'DELETE',N'dominio',N'TMDominio',@originalId,@domainName,SYSUTCDATETIME(),2,N'SUCCESS',1);
            INSERT INTO audit.RecordSnapshot
                (OperationId,EntityCode,PhysicalTableName,PrimaryKeyJson,ForeignKeysJson,RowDataJson,DeleteOrder,RestoreOrder,IsRoot,DisplayName)
            VALUES
                (@operationId,N'dominio',N'TMDominio',@domainKeyJson,N'{}',@domainRowJson,3,0,1,@domainName),
                (@operationId,N'dominio',N'TBuildingBlock',@buildingBlockKeyJson,@buildingBlockForeignKeysJson,@buildingBlockRowJson,2,1,0,@buildingBlockName);
            """, connection, (SqlTransaction)transaction);
        command.Parameters.AddWithValue("@operationId", operationId);
        command.Parameters.AddWithValue("@correlationId", correlationId);
        command.Parameters.AddWithValue("@originalId", originalId);
        command.Parameters.AddWithValue("@domainName", domainName);
        command.Parameters.AddWithValue("@buildingBlockName", buildingBlockName);
        command.Parameters.AddWithValue("@domainKeyJson", domainKeyJson);
        command.Parameters.AddWithValue("@domainRowJson", domainRowJson);
        command.Parameters.AddWithValue("@buildingBlockKeyJson", buildingBlockKeyJson);
        command.Parameters.AddWithValue("@buildingBlockForeignKeysJson", JsonSerializer.Serialize(new { idDominio = originalId }));
        command.Parameters.AddWithValue("@buildingBlockRowJson", buildingBlockRowJson);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static async Task<int?> ReadDomainIdAsync(string domainName)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand("SELECT iddominio FROM dbo.TMDominio WHERE dominio=@name", connection);
        command.Parameters.AddWithValue("@name", domainName);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }

    private static async Task<(int Id, int DomainId)?> ReadBuildingBlockAsync(string buildingBlockName)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(
            "SELECT idBuildingBlock,idDominio FROM dbo.TBuildingBlock WHERE nombreBuildingBlock=@name", connection);
        command.Parameters.AddWithValue("@name", buildingBlockName);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? (reader.GetInt32(0), reader.GetInt32(1)) : null;
    }

    private static async Task<(string OldPrimaryKeyJson, string NewPrimaryKeyJson)?> ReadKeyMapAsync(Guid operationId, string table)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(
            "SELECT OldPrimaryKeyJson,NewPrimaryKeyJson FROM audit.RecordKeyMap WHERE OperationId=@operationId AND PhysicalTableName=@table", connection);
        command.Parameters.AddWithValue("@operationId", operationId);
        command.Parameters.AddWithValue("@table", table);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? (reader.GetString(0), reader.GetString(1)) : null;
    }

    private static async Task<(Guid OperationId, string Status)?> ReadRestoreOperationAsync(Guid originalOperationId, string correlationId)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(
            "SELECT OperationId,Status FROM audit.Operation WHERE ReversesOperationId=@originalId AND CorrelationId=@correlationId", connection);
        command.Parameters.AddWithValue("@originalId", originalOperationId);
        command.Parameters.AddWithValue("@correlationId", correlationId);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? (reader.GetGuid(0), reader.GetString(1)) : null;
    }

    private static async Task<int> CountKeyMapsAsync(Guid operationId) =>
        await ScalarAsync("SELECT COUNT(*) FROM audit.RecordKeyMap WHERE OperationId=@id", operationId);

    private static async Task<int> CountRestoresAsync(Guid operationId) =>
        await ScalarAsync("SELECT COUNT(*) FROM audit.Operation WHERE ReversesOperationId=@id AND ActionType=N'RESTORE' AND Status=N'Succeeded'", operationId);

    private static async Task<int> ScalarAsync(string sql, Guid operationId)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", operationId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static int ReadMappedId(string json)
    {
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement.GetProperty("id");
        return value.ValueKind == JsonValueKind.String
            ? int.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture)
            : value.GetInt32();
    }

    private static async Task RequireTablesAsync(params string[] tables)
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        foreach (var table in tables)
        {
            await using var command = new SqlCommand("SELECT OBJECT_ID(@table, 'U');", connection);
            command.Parameters.AddWithValue("@table", table);
            if (await command.ExecuteScalarAsync() is DBNull or null)
                throw new InvalidOperationException($"DEV schema mismatch: missing table {table}.");
        }
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Audit:UndoRetentionDays"] = "30" })
        .Build();

    private sealed class Access : IEffectiveAccessService
    {
        public Task<bool> HasActiveCorporateScopeAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> IsAuthorizedAsync(Guid userId, string permission, int empresaSubsidiariaId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}