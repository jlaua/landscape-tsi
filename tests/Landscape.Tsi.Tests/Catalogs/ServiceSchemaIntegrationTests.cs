using Landscape.Tsi.Tests.Infrastructure;

using Microsoft.Data.SqlClient;

namespace Landscape.Tsi.Tests.Catalogs;

[Collection("SqlIntegration")]
public sealed class ServiceSchemaIntegrationTests
{
    [SqlIntegrationFact]
    public async Task ApplyEvolveServiceSchemaScript_ValidatesAndAppliesSchemaChanges_Safely()
    {
        await using var connection = await SqlIntegrationTestSettings.OpenValidatedConnectionAsync();

        // 1. Validar nombre de base de datos
        await using var dbCommand = new SqlCommand("SELECT DB_NAME();", connection);
        var dbName = Convert.ToString(await dbCommand.ExecuteScalarAsync());
        Assert.Equal("db-landscape-tsi-dev-v2", dbName);

        // 2. Leer y ejecutar scripts/database/evolve-service-schema.sql
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "../../../../../scripts/database/evolve-service-schema.sql");
        if (!File.Exists(scriptPath))
        {
            scriptPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../scripts/database/evolve-service-schema.sql"));
        }
        if (!File.Exists(scriptPath))
        {
            scriptPath = @"D:\JLAU\landscape tsi\scripts\database\evolve-service-schema.sql";
        }
        Assert.True(File.Exists(scriptPath), $"Script not found at {scriptPath}");

        var scriptSql = await File.ReadAllTextAsync(scriptPath);
        await using (var execCommand = new SqlCommand(scriptSql, connection))
        {
            await execCommand.ExecuteNonQueryAsync();
        }

        // 3. Verificar existencia de las 5 nuevas tablas
        var expectedNewTables = new[]
        {
            "TTipoServicio",
            "TServicioTecnologia",
            "TTarifarioProyectoHoras",
            "TActividadNivelSoporte",
            "TTarifarioOperacion"
        };

        foreach (var tableName in expectedNewTables)
        {
            await using var checkTableCmd = new SqlCommand(
                "SELECT COUNT(*) FROM sys.tables WHERE name = @name AND schema_id = SCHEMA_ID('dbo');", connection);
            checkTableCmd.Parameters.AddWithValue("@name", tableName);
            var count = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }

        // 4. Verificar semillas maestras
        await using (var checkTiposCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TTipoServicio;", connection))
        {
            var tipoCount = Convert.ToInt32(await checkTiposCmd.ExecuteScalarAsync());
            Assert.True(tipoCount >= 3, "Deben existir al menos los 3 tipos de servicio (Implementación, Migración, Operación)");
        }

        await using (var checkActividadesCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TActividadNivelSoporte;", connection))
        {
            var actCount = Convert.ToInt32(await checkActividadesCmd.ExecuteScalarAsync());
            Assert.True(actCount >= 25, "Deben existir al menos 25 actividades de soporte (8 N1, 12 N2, 5 N3)");
        }

        // 5. Test transaccional con ITEST_* para validar inserción referencial y limpieza
        var testRunId = $"ITEST_{Guid.NewGuid():N}";
        int insertedServicioId = 0;
        await using (var insertServicioCmd = new SqlCommand(@"
            DECLARE @tipoId INT = (SELECT TOP 1 idTipoServicio FROM dbo.TTipoServicio WHERE codigo = 'IMPLEMENTACION');
            INSERT INTO dbo.TServicioTecnologia (codigoServicio, nombreServicio, idTipoServicio, estadoServicio, moneda)
            VALUES (@codigo, @nombre, @tipoId, 'EVALUACION', 'USD');
            SELECT SCOPE_IDENTITY();", connection))
        {
            insertServicioCmd.Parameters.AddWithValue("@codigo", testRunId);
            insertServicioCmd.Parameters.AddWithValue("@nombre", $"Servicio Test {testRunId}");
            insertedServicioId = Convert.ToInt32(await insertServicioCmd.ExecuteScalarAsync());
            Assert.True(insertedServicioId > 0);
        }

        // Insertar ítem de tarifario de proyecto
        await using (var insertTarifarioCmd = new SqlCommand(@"
            INSERT INTO dbo.TTarifarioProyectoHoras (idServicio, complejidad, rangoHorasDesde, rangoHorasHasta, tarifaHora, horasEstimadas, subtotal, moneda)
            VALUES (@idServicio, 'Proyecto menor', 1, 100, 75.00, 40.00, 3000.00, 'USD');", connection))
        {
            insertTarifarioCmd.Parameters.AddWithValue("@idServicio", insertedServicioId);
            var rows = await insertTarifarioCmd.ExecuteNonQueryAsync();
            Assert.Equal(1, rows);
        }

        // Limpiar registro de test (ON DELETE CASCADE elimina el tarifario)
        await using (var cleanupCmd = new SqlCommand("DELETE FROM dbo.TServicioTecnologia WHERE idServicio = @idServicio;", connection))
        {
            cleanupCmd.Parameters.AddWithValue("@idServicio", insertedServicioId);
            await cleanupCmd.ExecuteNonQueryAsync();
        }

        // Verificar eliminación en cascada
        await using (var checkCleanupCmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TTarifarioProyectoHoras WHERE idServicio = @idServicio;", connection))
        {
            checkCleanupCmd.Parameters.AddWithValue("@idServicio", insertedServicioId);
            var remaining = Convert.ToInt32(await checkCleanupCmd.ExecuteScalarAsync());
            Assert.Equal(0, remaining);
        }
    }
}