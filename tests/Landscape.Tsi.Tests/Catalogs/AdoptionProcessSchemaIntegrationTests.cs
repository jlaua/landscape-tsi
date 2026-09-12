using Landscape.Tsi.Tests.Infrastructure;

using Microsoft.Data.SqlClient;

namespace Landscape.Tsi.Tests.Catalogs;

[Collection("SqlIntegration")]
public sealed class AdoptionProcessSchemaIntegrationTests
{
    [SqlIntegrationFact]
    public async Task ApplyEvolveSchemaScript_ValidatesAndAppliesSchemaChanges_Safely()
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();

        // 1. Validar nombre de base de datos
        await using var dbCommand = new SqlCommand("SELECT DB_NAME();", connection);
        var dbName = Convert.ToString(await dbCommand.ExecuteScalarAsync());
        Assert.Equal("db-landscape-tsi-dev-v2", dbName);

        // 2. Leer y ejecutar scripts/database/evolve-adoption-process-schema.sql
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "../../../../../scripts/database/evolve-adoption-process-schema.sql");
        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../scripts/database/evolve-adoption-process-schema.sql"));
        }
        if (!File.Exists(scriptPath))
        {
            scriptPath = @"D:\JLAU\landscape tsi\scripts\database\evolve-adoption-process-schema.sql";
        }
        Assert.True(File.Exists(scriptPath), $"Script not found at {scriptPath}");

        var scriptSql = await File.ReadAllTextAsync(scriptPath);
        await using (var execCommand = new SqlCommand(scriptSql, connection))
        {
            await execCommand.ExecuteNonQueryAsync();
        }

        // 3. Verificar existencia de nuevas tablas
        var expectedNewTables = new[]
        {
            "TProcesoAdopcionTSI",
            "TProcesoAdopcionEmpresa",
            "TEstandarTecnologiaHistorico",
            "TContratoTecnologia"
        };

        foreach (var tableName in expectedNewTables)
        {
            await using var checkTableCmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.tables WHERE name = @name AND schema_id = SCHEMA_ID('dbo');", connection);
            checkTableCmd.Parameters.AddWithValue("@name", tableName);
            var count = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }

        // 4. Verificar existencia de nuevas columnas
        var expectedColumns = new (string Table, string Column)[]
        {
            ("TBuildingBlock", "idFamilia"),
            ("TTecnologiaTSIimplementadaSubsidiaria", "idBuildingBlock"),
            ("TTecnologiaTSIimplementadaSubsidiaria", "idProcesoAdopcionEmpresa"),
            ("TTecnologiaTSIimplementadaSubsidiaria", "esTecnologiaPrimaria"),
            ("TDriver", "unidadMedida"),
            ("TDriver", "cantidad"),
            ("TDriver", "precioUnitario"),
            ("TDriver", "moneda"),
            ("TCasosDeUso", "idEstandarTecnologia")
        };

        foreach (var (table, column) in expectedColumns)
        {
            await using var checkColCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM sys.columns c
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = @table AND c.name = @column AND t.schema_id = SCHEMA_ID('dbo');", connection);
            checkColCmd.Parameters.AddWithValue("@table", table);
            checkColCmd.Parameters.AddWithValue("@column", column);
            var colCount = Convert.ToInt32(await checkColCmd.ExecuteScalarAsync());
            Assert.Equal(1, colCount);
        }

        // 5. Test transaccional con ITEST_* para validar integridad y limpieza
        var testRunId = $"ITEST_{Guid.NewGuid():N}";
        await using (var insertTestCmd = new SqlCommand(@"
            DECLARE @bbId INT = (SELECT TOP 1 idBuildingBlock FROM dbo.TBuildingBlock ORDER BY idBuildingBlock);
            DECLARE @estadoId INT = (SELECT TOP 1 idEstadoAdopcionTSI FROM dbo.TMEstadoAdopcionTSI ORDER BY idEstadoAdopcionTSI);
            
            INSERT INTO dbo.TProcesoAdopcionTSI (codigoProceso, nombreProceso, idBuildingBlock, idEstadoAdopcionTSI, fechaInicio)
            VALUES (@codigo, @nombre, @bbId, @estadoId, CAST(GETUTCDATE() AS DATE));", connection))
        {
            insertTestCmd.Parameters.AddWithValue("@codigo", testRunId);
            insertTestCmd.Parameters.AddWithValue("@nombre", $"{testRunId}_NOMBRE");
            await insertTestCmd.ExecuteNonQueryAsync();
        }

        // Verificar inserción y limpiar
        await using (var verifyCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso = @codigo;", connection))
        {
            verifyCmd.Parameters.AddWithValue("@codigo", testRunId);
            var insertedCount = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync());
            Assert.Equal(1, insertedCount);
        }

        await using (var cleanupCmd = new SqlCommand("DELETE FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso = @codigo;", connection))
        {
            cleanupCmd.Parameters.AddWithValue("@codigo", testRunId);
            await cleanupCmd.ExecuteNonQueryAsync();
        }
    }

    [SqlIntegrationFact]
    public async Task Inspect_RelatedColumns_VerifyMetadata()
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();
        await using var cmd = new SqlCommand(@"
            SELECT t.name AS TableName, c.name AS ColumnName
            FROM sys.tables t
            JOIN sys.columns c ON c.object_id = t.object_id
            WHERE t.name IN ('TContactoEmpresaSubsidiaria', 'TModeloDeOperacion', 'TVendor', 'TContactoVendor', 'TContactoPartner')
            ORDER BY t.name, c.column_id;", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        var columns = new List<string>();
        while (await reader.ReadAsync())
        {
            columns.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        Assert.Contains(columns, c => c.StartsWith("TContactoEmpresaSubsidiaria.idContactoEmpresaSubsidiaria"));
        Assert.Contains(columns, c => c.StartsWith("TModeloDeOperacion.idModeloOperacion"));
        var contactoCols = columns.Where(c => c.StartsWith("TContactoEmpresaSubsidiaria.")).ToList();
        var modeloCols = columns.Where(c => c.StartsWith("TModeloDeOperacion.")).ToList();
        var vendorCols = columns.Where(c => c.StartsWith("TVendor.")).ToList();
        Assert.True(contactoCols.Count > 0);
        Assert.True(modeloCols.Count > 0);
        Assert.True(vendorCols.Count > 0);
    }
}