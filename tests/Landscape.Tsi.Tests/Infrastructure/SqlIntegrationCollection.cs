using Microsoft.Data.SqlClient;

namespace Landscape.Tsi.Tests.Infrastructure;

[CollectionDefinition("SqlIntegration", DisableParallelization = true)]
public sealed class SqlIntegrationCollection;

internal sealed class SqlIntegrationDataScope : IAsyncDisposable
{
    private readonly List<(string Table, string KeyColumn, object Key)> created = [];
    private readonly List<(int BuildingBlockId, int TechnologyId)> bridges = [];
    public string TestRunId { get; } = $"ITEST_{Guid.NewGuid():N}";
    public string Prefix => TestRunId;

    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default) =>
        await SqlIntegrationTestSettings.OpenValidatedConnectionAsync(cancellationToken);

    public void Track(string table, string keyColumn, object key) => created.Add((table, keyColumn, key));

    public void TrackBridge(int buildingBlockId, int technologyId) =>
        bridges.Add((buildingBlockId, technologyId));

    public Task<int> CreateDomainAsync(string suffix = "DOMAIN") =>
        InsertAsync("INSERT dbo.TMDominio (dominio) OUTPUT INSERTED.iddominio VALUES (@name)",
            $"{Prefix}_{suffix}", "dbo.TMDominio", "iddominio");

    public Task<int> CreateFamilyAsync(string suffix = "FAMILY") =>
        InsertAsync("INSERT dbo.TMFamilia (nombreFamilia) OUTPUT INSERTED.idFamilia VALUES (@name)",
            $"{Prefix}_{suffix}", "dbo.TMFamilia", "idFamilia");

    public Task<int> CreateBuildingBlockAsync(int domainId, string suffix, int? phaseId = null) =>
        InsertAsync("INSERT dbo.TBuildingBlock (nombreBuildingBlock,idDominio,idEstadoFaseDeAdopcionBuildingBlock) OUTPUT INSERTED.idBuildingBlock VALUES (@name,@parent,@optional)",
            $"{Prefix}_{suffix}", "dbo.TBuildingBlock", "idBuildingBlock", domainId, phaseId);

    public Task<int> CreateTechnologyAsync(int familyId, string suffix) =>
        InsertAsync("INSERT dbo.TTecnologiaTSI ([nombreTecnologiaAlternativa1-Corporativo],idFamilia) OUTPUT INSERTED.idTecnologiaTSI VALUES (@name,@parent)",
            $"{Prefix}_{suffix}", "dbo.TTecnologiaTSI", "idTecnologiaTSI", familyId);

    public async Task<IReadOnlyList<int>> ReadPhaseIdsAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("SELECT TOP (2) idEstadoFaseAdopcion FROM dbo.TEstadoFaseAdopcion ORDER BY idEstadoFaseAdopcion", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<int>();
        while (await reader.ReadAsync()) result.Add(reader.GetInt32(0));
        return result;
    }

    public async Task AddBridgeAsync(int buildingBlockId, int technologyId)
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("INSERT dbo.TBuildingBlockVsTTecnologiaTSI (idBuildingBlock,idTecnologiaTSI) VALUES (@bb,@tech)", connection);
        command.Parameters.AddWithValue("@bb", buildingBlockId);
        command.Parameters.AddWithValue("@tech", technologyId);
        await command.ExecuteNonQueryAsync();
        TrackBridge(buildingBlockId, technologyId);
    }

    public async Task<int> CountBridgeAsync(int buildingBlockId, int technologyId)
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock=@bb AND idTecnologiaTSI=@tech", connection);
        command.Parameters.AddWithValue("@bb", buildingBlockId);
        command.Parameters.AddWithValue("@tech", technologyId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task<int> InsertAsync(string sql, string name, string table, string keyColumn, int? parent = null, int? optional = null)
    {
        await using var connection = await OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@name", name);
        if (sql.Contains("@parent", StringComparison.Ordinal)) command.Parameters.AddWithValue("@parent", (object?)parent ?? DBNull.Value);
        if (sql.Contains("@optional", StringComparison.Ordinal)) command.Parameters.AddWithValue("@optional", (object?)optional ?? DBNull.Value);
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        Track(table, keyColumn, id);
        return id;
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = await OpenAsync();
        foreach (var bridge in bridges.Distinct())
        {
            await using var bridgeCommand = new SqlCommand("DELETE FROM dbo.TBuildingBlockVsTTecnologiaTSI WHERE idBuildingBlock=@bb AND idTecnologiaTSI=@tech", connection);
            bridgeCommand.Parameters.AddWithValue("@bb", bridge.BuildingBlockId);
            bridgeCommand.Parameters.AddWithValue("@tech", bridge.TechnologyId);
            await bridgeCommand.ExecuteNonQueryAsync();
        }
        foreach (var item in created.AsEnumerable().Reverse())
        {
            await using var command = new SqlCommand($"DELETE FROM [{item.Table.Split('.')[0]}].[{item.Table.Split('.')[1]}] WHERE [{item.KeyColumn}] = @id", connection);
            command.Parameters.AddWithValue("@id", item.Key);
            await command.ExecuteNonQueryAsync();
        }
    }
}