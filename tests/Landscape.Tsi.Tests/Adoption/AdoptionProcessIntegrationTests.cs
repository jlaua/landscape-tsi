using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Adoption;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Tests.Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Adoption;

[Collection("SqlIntegration")]
public sealed class AdoptionProcessIntegrationTests
{
    [SqlIntegrationFact]
    public async Task AdoptionProcess_Lifecycle_StandardsAndContracts_ExecuteSuccessfully()
    {
        await using var scope = new SqlIntegrationDataScope();
        var prefix = scope.Prefix;

        // Clean up any lingering ITEST records from previous cancelled runs
        await using (var preConn = await scope.OpenAsync())
        {
            await using var preCmd = preConn.CreateCommand();
            preCmd.CommandText = @"
                DELETE FROM dbo.TDriver WHERE idTecnologiaTSIimplementadaSubsidiaria IN (SELECT idTecnologiaTSIimplementadaSubsidiaria FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock IN (SELECT idBuildingBlock FROM dbo.TBuildingBlock WHERE nombreBuildingBlock LIKE 'ITEST_%'));
                DELETE FROM dbo.TContratoTecnologia WHERE idTecnologiaTSIimplementadaSubsidiaria IN (SELECT idTecnologiaTSIimplementadaSubsidiaria FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock IN (SELECT idBuildingBlock FROM dbo.TBuildingBlock WHERE nombreBuildingBlock LIKE 'ITEST_%'));
                DELETE FROM dbo.TModeloDeOperacion WHERE idTecnologiaTSIimplementadaSubsidiaria IN (SELECT idTecnologiaTSIimplementadaSubsidiaria FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock IN (SELECT idBuildingBlock FROM dbo.TBuildingBlock WHERE nombreBuildingBlock LIKE 'ITEST_%'));
                DELETE FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock IN (SELECT idBuildingBlock FROM dbo.TBuildingBlock WHERE nombreBuildingBlock LIKE 'ITEST_%');
                DELETE FROM dbo.TCasosDeUso WHERE idEstandarTecnologia IN (SELECT idEstandarTecnologia FROM dbo.TEstandarTecnologiaHistorico WHERE idProcesoAdopcionTSI IN (SELECT idProcesoAdopcionTSI FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso LIKE 'ITEST_%'));
                DELETE FROM dbo.TEstandarTecnologiaHistorico WHERE idProcesoAdopcionTSI IN (SELECT idProcesoAdopcionTSI FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso LIKE 'ITEST_%');
                DELETE FROM dbo.TProcesoAdopcionEmpresa WHERE idProcesoAdopcionTSI IN (SELECT idProcesoAdopcionTSI FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso LIKE 'ITEST_%');
                DELETE FROM dbo.TProcesoAdopcionTSI WHERE codigoProceso LIKE 'ITEST_%';
            ";
            await preCmd.ExecuteNonQueryAsync();
        }

        // Setup master records
        var domainId = await scope.CreateDomainAsync();
        var familyId = await scope.CreateFamilyAsync();
        var bbId = await scope.CreateBuildingBlockAsync(domainId, "BB_ADOP");
        var techA = await scope.CreateTechnologyAsync(familyId, "TECH_STD_A");
        var techB = await scope.CreateTechnologyAsync(familyId, "TECH_STD_B");
        scope.TrackBridge(bbId, techA);
        scope.TrackBridge(bbId, techB);

        // Fetch a real company and adoption state from DB for testing
        int companyId;
        int stateId;
        await using (var conn = await scope.OpenAsync())
        {
            await using var cmd = new SqlCommand("SELECT TOP 1 idEmpresaSubsidiaria FROM dbo.TEmpresaSubsidiaria ORDER BY idEmpresaSubsidiaria", conn);
            var compObj = await cmd.ExecuteScalarAsync();
            Assert.NotNull(compObj);
            companyId = Convert.ToInt32(compObj);

            await using var cmdState = new SqlCommand("SELECT TOP 1 idEstadoAdopcionTSI FROM dbo.TMEstadoAdopcionTSI ORDER BY idEstadoAdopcionTSI", conn);
            var stateObj = await cmdState.ExecuteScalarAsync();
            Assert.NotNull(stateObj);
            stateId = Convert.ToInt32(stateObj);
        }

        await using var context = CreateContext();
        var service = new AdoptionProcessService(context, new TestAuditTrail());

        var actorUserId = Guid.NewGuid();
        var correlationId = $"{prefix}_CORR";

        // 1. Create Adoption Process
        var openResult = await service.CreateProcessAsync(new CreateAdoptionProcessCommand(
            Codigo: $"{prefix}_PROC",
            Nombre: "Proceso TSI Test",
            BuildingBlockId: bbId,
            EstadoAdopcionId: stateId,
            Objetivo: "Evaluación de adopción de estándar",
            Alcance: "Todas las subsidiarias",
            LiderCorporativo: "Líder TSI",
            FechaInicio: DateTime.Today,
            FechaEstimadaCierre: DateTime.Today.AddMonths(6),
            ActorUserId: actorUserId,
            CorrelationId: correlationId));

        Assert.True(openResult.Succeeded, openResult.Message);
        var processId = openResult.EntityId!.Value;

        try
        {
            // 2. Set Corporate Standard A as PRINCIPAL
            var stdAResult = await service.SetCorporateStandardAsync(new SetCorporateStandardCommand(
                BuildingBlockId: bbId,
                TecnologiaId: techA,
                ProcesoAdopcionId: processId,
                RolEstandar: "PRINCIPAL",
                FechaInicio: DateTime.Today,
                MotivoCambio: "Primer estándar corporativo",
                SustentoArquitectura: "Arquitectura Cloud First",
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(stdAResult.Succeeded, stdAResult.Message);
            var stdAId = stdAResult.EntityId!.Value;

            // 3. Set Corporate Standard B as new PRINCIPAL (should transition A to HISTORICO_REEMPLAZADO)
            var stdBResult = await service.SetCorporateStandardAsync(new SetCorporateStandardCommand(
                BuildingBlockId: bbId,
                TecnologiaId: techB,
                ProcesoAdopcionId: processId,
                RolEstandar: "PRINCIPAL",
                FechaInicio: DateTime.Today,
                MotivoCambio: "Evolución tecnológica",
                SustentoArquitectura: "Mejor soporte nativo",
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(stdBResult.Succeeded, stdBResult.Message);
            var stdBId = stdBResult.EntityId!.Value;

            // Verify historical transition in DB
            var stdA = await context.StandardTechnologyHistories.AsNoTracking().FirstOrDefaultAsync(s => s.IdEstandarTecnologia == stdAId);
            Assert.NotNull(stdA);
            Assert.Equal("HISTORICO_REEMPLAZADO", stdA.EstadoVigencia);
            Assert.NotNull(stdA.FechaFinVigencia);

            var stdB = await context.StandardTechnologyHistories.AsNoTracking().FirstOrDefaultAsync(s => s.IdEstandarTecnologia == stdBId);
            Assert.NotNull(stdB);
            Assert.Equal("ACTIVO_VIGENTE", stdB.EstadoVigencia);
            Assert.Equal("PRINCIPAL", stdB.RolEstandar);

            // 4. Convene Company
            var convResult = await service.ConveneCompanyAsync(new ConveneCompanyCommand(
                ProcesoId: processId,
                EmpresaId: companyId,
                ContactoFocalId: null,
                Aplica: true,
                JustificacionNoAplica: null,
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(convResult.Succeeded, convResult.Message);
            var procesoEmpresaId = convResult.EntityId!.Value;

            // 5. Register Implemented Technology for Company
            var regTechResult = await service.RegisterImplementedTechnologyAsync(new RegisterImplementedTechnologyCommand(
                EmpresaId: companyId,
                TecnologiaId: techB,
                BuildingBlockId: bbId,
                ProcesoEmpresaId: procesoEmpresaId,
                EsPrimaria: true,
                VersionDesplegada: "2.0.1",
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(regTechResult.Succeeded, regTechResult.Message);
            var implId = regTechResult.EntityId!.Value;

            // 6. Contracts: Main Contract and Adenda
            var mainContractResult = await service.SaveContractAsync(new SaveContractCommand(
                TecnologiaImplementadaId: implId,
                NumeroContrato: $"{prefix}_CT_001",
                EsAdenda: false,
                ContratoPadreId: null,
                FechaInicio: new DateTime(2025, 1, 1),
                FechaFin: new DateTime(2026, 1, 1),
                FechaAdjudicacion: new DateTime(2024, 12, 1),
                RutaDocumento: "/contracts/doc1.pdf",
                Monto: 50000m,
                Moneda: "USD",
                Observaciones: "Contrato principal de soporte",
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(mainContractResult.Succeeded, mainContractResult.Message);
            var mainContractId = mainContractResult.EntityId!.Value;

            // Adenda with valid parent
            var adendaResult = await service.SaveContractAsync(new SaveContractCommand(
                TecnologiaImplementadaId: implId,
                NumeroContrato: $"{prefix}_AD_001",
                EsAdenda: true,
                ContratoPadreId: mainContractId,
                FechaInicio: new DateTime(2025, 6, 1),
                FechaFin: new DateTime(2026, 1, 1),
                FechaAdjudicacion: null,
                RutaDocumento: null,
                Monto: 10000m,
                Moneda: "USD",
                Observaciones: "Adenda 1 ampliación",
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(adendaResult.Succeeded, adendaResult.Message);

            // Adenda without parent should fail
            var invalidAdendaResult = await service.SaveContractAsync(new SaveContractCommand(
                TecnologiaImplementadaId: implId,
                NumeroContrato: $"{prefix}_AD_INVALID",
                EsAdenda: true,
                ContratoPadreId: null,
                FechaInicio: DateTime.Today,
                FechaFin: DateTime.Today.AddYears(1),
                FechaAdjudicacion: null,
                RutaDocumento: null,
                Monto: null,
                Moneda: "USD",
                Observaciones: null,
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.False(invalidAdendaResult.Succeeded);

            // 7. Driver Registration with Projected Cost
            var driverResult = await service.SaveDriverAsync(new SaveDriverCommand(
                TecnologiaImplementadaId: implId,
                Descripcion: "Licencias de usuario",
                UnidadMedida: "Usuarios",
                Cantidad: 500,
                PrecioUnitario: 12.50m,
                Moneda: "USD",
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(driverResult.Succeeded, driverResult.Message);
            var driverId = driverResult.EntityId!.Value;

            // 7.b Save Operation Model for implemented technology (Insert)
            int tipoOpId;
            int modLabId;
            await using (var conn = await scope.OpenAsync())
            {
                await using var cmdTipo = new SqlCommand("SELECT TOP 1 idTipoModeloOperacion FROM dbo.TTipoOperacion ORDER BY idTipoModeloOperacion", conn);
                tipoOpId = Convert.ToInt32(await cmdTipo.ExecuteScalarAsync());

                await using var cmdMod = new SqlCommand("SELECT TOP 1 idModalidadLaboral FROM dbo.TModalidadLaboral ORDER BY idModalidadLaboral", conn);
                modLabId = Convert.ToInt32(await cmdMod.ExecuteScalarAsync());
            }

            var opModelResult = await service.SaveOperationModelAsync(new SaveOperationModelCommand(
                TecnologiaImplementadaId: implId,
                TipoOperacionId: tipoOpId,
                ModalidadLaboralId: modLabId,
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(opModelResult.Succeeded, opModelResult.Message);

            // Test Update of Operation Model
            var updateOpModelResult = await service.SaveOperationModelAsync(new SaveOperationModelCommand(
                TecnologiaImplementadaId: implId,
                TipoOperacionId: tipoOpId,
                ModalidadLaboralId: modLabId,
                ActorUserId: actorUserId,
                CorrelationId: correlationId));

            Assert.True(updateOpModelResult.Succeeded, updateOpModelResult.Message);

            // 8. Query Detail and verify alignment, contracts, and operation model
            var detail = await service.GetProcessDetailAsync(processId);
            Assert.NotNull(detail);
            Assert.Equal(bbId, detail.BuildingBlockId);
            Assert.Single(detail.EmpresasParticipantes);
            var compRow = detail.EmpresasParticipantes[0];
            Assert.Equal("ALINEADO", compRow.EstadoAlineamiento);
            Assert.Single(compRow.TecnologiasImplementadas);
            var implDto = compRow.TecnologiasImplementadas[0];
            Assert.Single(implDto.Contratos);
            Assert.Single(implDto.Contratos[0].Adendas);
            Assert.Single(implDto.Drivers);
            Assert.Equal(6250.00m, implDto.Drivers[0].CostoTotal);
            Assert.NotNull(implDto.ModeloOperacion);
            Assert.Equal(tipoOpId, implDto.ModeloOperacion.TipoOperacionId);
            Assert.Equal(modLabId, implDto.ModeloOperacion.ModalidadLaboralId);
        }
        finally
        {
            // Cleanup in reverse dependency order
            await using var cleanupConn = await scope.OpenAsync();
            await using var cleanCmd = cleanupConn.CreateCommand();
            cleanCmd.CommandText = @"
                DELETE FROM dbo.TDriver WHERE idTecnologiaTSIimplementadaSubsidiaria IN (SELECT idTecnologiaTSIimplementadaSubsidiaria FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock = @bbId OR idTecnologiaTSI IN (@techA, @techB));
                DELETE FROM dbo.TContratoTecnologia WHERE idTecnologiaTSIimplementadaSubsidiaria IN (SELECT idTecnologiaTSIimplementadaSubsidiaria FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock = @bbId OR idTecnologiaTSI IN (@techA, @techB));
                DELETE FROM dbo.TModeloDeOperacion WHERE idTecnologiaTSIimplementadaSubsidiaria IN (SELECT idTecnologiaTSIimplementadaSubsidiaria FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock = @bbId OR idTecnologiaTSI IN (@techA, @techB));
                DELETE FROM dbo.TTecnologiaTSIimplementadaSubsidiaria WHERE idBuildingBlock = @bbId OR idTecnologiaTSI IN (@techA, @techB);
                DELETE FROM dbo.TCasosDeUso WHERE idEstandarTecnologia IN (SELECT idEstandarTecnologia FROM dbo.TEstandarTecnologiaHistorico WHERE idProcesoAdopcionTSI = @procId);
                DELETE FROM dbo.TEstandarTecnologiaHistorico WHERE idProcesoAdopcionTSI = @procId;
                DELETE FROM dbo.TProcesoAdopcionEmpresa WHERE idProcesoAdopcionTSI = @procId;
                DELETE FROM dbo.TProcesoAdopcionTSI WHERE idProcesoAdopcionTSI = @procId;
            ";
            cleanCmd.Parameters.AddWithValue("@bbId", bbId);
            cleanCmd.Parameters.AddWithValue("@procId", processId);
            cleanCmd.Parameters.AddWithValue("@techA", techA);
            cleanCmd.Parameters.AddWithValue("@techB", techB);
            await cleanCmd.ExecuteNonQueryAsync();
        }
    }

    private static CatalogDbContext CreateContext() => new(new DbContextOptionsBuilder<CatalogDbContext>()
        .UseSqlServer(SqlIntegrationTestSettings.ConnectionString).Options);

    private sealed class TestAuditTrail : IAuditTrailService
    {
        public Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void AddSnapshot(AuditOperation operation, string entityCode, string physicalTableName, string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder, bool isRoot, string? displayName = null) => throw new NotSupportedException();
    }
}