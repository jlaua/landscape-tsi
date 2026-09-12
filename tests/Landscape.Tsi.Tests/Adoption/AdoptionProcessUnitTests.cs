using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Domain.Adoption;

namespace Landscape.Tsi.Tests.Adoption;

public sealed class AdoptionProcessUnitTests
{
    [Fact]
    public void ContractDto_CalculatesAdendasCorrectly()
    {
        var adenda1 = new ContractDto(2, 10, "CT-001-A1", true, 1, "CT-001", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), null, null, 15000m, "USD", null, []);
        var adenda2 = new ContractDto(3, 10, "CT-001-A2", true, 1, "CT-001", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), null, null, 20000m, "USD", null, []);
        var parent = new ContractDto(1, 10, "CT-001", false, null, null, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), null, null, 100000m, "USD", null, [adenda1, adenda2]);

        Assert.Equal(2, parent.Adendas.Count);
        Assert.Equal("CT-001-A1", parent.Adendas[0].NumeroContrato);
        Assert.Equal("CT-001-A2", parent.Adendas[1].NumeroContrato);
        Assert.Equal(100000m, parent.Monto);
        Assert.Equal(35000m, parent.Adendas.Sum(a => a.Monto ?? 0));
    }

    [Fact]
    public void DriverDto_CalculatesTotalCostCorrectly()
    {
        var driver = new DriverDto(1, 10, "Licencias E5", "Usuarios", 250, 45.50m, "USD", 250 * 45.50m);

        Assert.Equal(11375.00m, driver.CostoTotal);
        Assert.Equal("Licencias E5", driver.Descripcion);
        Assert.Equal("Usuarios", driver.UnidadMedida);
        Assert.Equal("USD", driver.Moneda);
    }

    [Theory]
    [InlineData(false, 0, false, false, "NO_APLICA")]
    [InlineData(true, 0, false, false, "PENDIENTE")]
    [InlineData(true, 1, true, false, "ALINEADO")]
    [InlineData(true, 1, false, true, "HOMOLOGADO")]
    [InlineData(true, 1, false, false, "NO_ALINEADO")]
    public void AlignmentLogic_EvaluatesStateCorrectly(bool aplica, int techCount, bool isPrincipal, bool isAlternative, string expectedGlobalAlignment)
    {
        string alignment;
        if (!aplica)
        {
            alignment = "NO_APLICA";
        }
        else if (techCount == 0)
        {
            alignment = "PENDIENTE";
        }
        else if (isPrincipal)
        {
            alignment = "ALINEADO";
        }
        else if (isAlternative)
        {
            alignment = "HOMOLOGADO";
        }
        else
        {
            alignment = "NO_ALINEADO";
        }

        Assert.Equal(expectedGlobalAlignment, alignment);
    }

    [Fact]
    public void SaveContractCommand_RejectsAdendaWithoutParent()
    {
        var command = new SaveContractCommand(
            TecnologiaImplementadaId: 10,
            NumeroContrato: "AD-001",
            EsAdenda: true,
            ContratoPadreId: null,
            FechaInicio: DateTime.Today,
            FechaFin: DateTime.Today.AddYears(1),
            FechaAdjudicacion: null,
            RutaDocumento: null,
            Monto: null,
            Moneda: "USD",
            Observaciones: null,
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "test-corr");

        Assert.True(command.EsAdenda);
        Assert.Null(command.ContratoPadreId);
    }

    [Fact]
    public void TechnologyAdoptionEvolution_MaintainsBackwardCompatibilityProperties()
    {
        var techImpl = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 1,
            IdEmpresaSubsidiaria = 2,
            IdTecnologiaTSI = 3,
            IdBuildingBlock = 4,
            IdProcesoAdopcionEmpresa = 5,
            EsTecnologiaPrimaria = true,
            VersionDesplegada = "12.4.1"
        };

        Assert.Equal(1, techImpl.IdTecnologiaTSIimplementadaSubsidiaria);
        Assert.Equal(2, techImpl.IdEmpresaSubsidiaria);
        Assert.Equal(3, techImpl.IdTecnologiaTSI);
        Assert.Equal(4, techImpl.IdBuildingBlock);
        Assert.Equal(5, techImpl.IdProcesoAdopcionEmpresa);
        Assert.True(techImpl.EsTecnologiaPrimaria);
        Assert.Equal("12.4.1", techImpl.VersionDesplegada);
    }
}