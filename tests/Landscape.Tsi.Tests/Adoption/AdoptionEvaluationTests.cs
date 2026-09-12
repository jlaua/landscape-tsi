using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Web.Models;

namespace Landscape.Tsi.Tests.Adoption;

public sealed class AdoptionEvaluationTests
{
    [Fact]
    public void CreateEvaluationViewModel_DefaultsAndCollections_AreProperlyInitialized()
    {
        var model = new CreateEvaluationViewModel
        {
            Codigo = "EVAL-TSI-20260912-001",
            Nombre = "Evaluación Corporativa de Soluciones DLP",
            BuildingBlockId = 10,
            DominioId = 2,
            EstadoAdopcionId = 1,
            FechaInicio = new DateTime(2026, 9, 12)
        };

        Assert.Equal("EVAL-TSI-20260912-001", model.Codigo);
        Assert.Equal("Evaluación Corporativa de Soluciones DLP", model.Nombre);
        Assert.Equal(10, model.BuildingBlockId);
        Assert.Equal(2, model.DominioId);
        Assert.Equal(1, model.EstadoAdopcionId);
        Assert.NotNull(model.Subsidiaries);
        Assert.NotNull(model.SubsidiaryAsIsList);
        Assert.NotNull(model.Dominios);
        Assert.NotNull(model.BuildingBlocks);
        Assert.NotNull(model.EstadosAdopcion);
        Assert.NotNull(model.TecnologiasDisponibles);
    }

    [Fact]
    public void SubsidiaryCheckboxItem_SupportsMultiSelectionAndContactAssignment()
    {
        var item = new SubsidiaryCheckboxItem
        {
            EmpresaId = 5,
            EmpresaNombre = "Banco de Crédito de Bolivia",
            Pais = "Bolivia",
            Rubro = "Banca Comercial",
            Selected = true,
            ContactoFocalId = 12,
            ContactoFocalNombre = "Carlos Gutierrez",
            Aplica = true,
            ContactosDisponibles = [
                new CatalogOption(12, "Carlos Gutierrez (cgutierrez@banco.bo)"),
                new CatalogOption(15, "Ana Morales (amorales@banco.bo)")
            ]
        };

        Assert.Equal(5, item.EmpresaId);
        Assert.True(item.Selected);
        Assert.True(item.Aplica);
        Assert.Equal(12, item.ContactoFocalId);
        Assert.Equal(2, item.ContactosDisponibles.Count);
        Assert.Null(item.JustificacionNoAplica);
    }

    [Fact]
    public void SubsidiaryCheckboxItem_WhenNotApplies_ContainsJustification()
    {
        var item = new SubsidiaryCheckboxItem
        {
            EmpresaId = 8,
            EmpresaNombre = "Prima AFP",
            Selected = true,
            Aplica = false,
            JustificacionNoAplica = "La subsidiaria no requiere esta capacidad por restricciones normativas de la SBS."
        };

        Assert.True(item.Selected);
        Assert.False(item.Aplica);
        Assert.NotNull(item.JustificacionNoAplica);
        Assert.Contains("SBS", item.JustificacionNoAplica);
    }

    [Fact]
    public void SubsidiaryAsIsInputModel_CapturesContractAndDriversCorrectly()
    {
        var asIs = new SubsidiaryAsIsInputModel
        {
            EmpresaId = 1,
            EmpresaNombre = "Banco de Crédito del Perú",
            TieneTecnologia = true,
            TecnologiaId = 101,
            TecnologiaNombre = "CyberArk Privileged Access Manager",
            VersionDesplegada = "12.6",
            VendorNombre = "CyberArk Software",
            VendorContacto = "ventas@cyberark.com",
            PartnerNombre = "Noventiq Perú",
            PartnerContacto = "soporte@noventiq.com",
            NumeroContrato = "CT-BCP-PAM-2024",
            FechaInicioContrato = new DateTime(2024, 6, 1),
            FechaFinContrato = new DateTime(2027, 5, 31),
            MontoContratado = 150000m,
            MonedaContrato = "USD",
            Drivers = [
                new DriverInputModel { DescripcionDriver = "Licencias EPV", Cantidad = 500, PrecioUnitario = 200m, Moneda = "USD", UnidadMedida = "Usuarios" },
                new DriverInputModel { DescripcionDriver = "Soporte 24x7", Cantidad = 1, PrecioUnitario = 50000m, Moneda = "USD", UnidadMedida = "Anual" }
            ],
            TipoOperacionId = 2,
            ModalidadLaboralId = 3
        };

        Assert.Equal(1, asIs.EmpresaId);
        Assert.True(asIs.TieneTecnologia);
        Assert.Equal(101, asIs.TecnologiaId);
        Assert.Equal("CT-BCP-PAM-2024", asIs.NumeroContrato);
        Assert.Equal(new DateTime(2027, 5, 31), asIs.FechaFinContrato);
        Assert.Equal(2, asIs.Drivers.Count);
        Assert.Equal(150000m, asIs.MontoContratado);
    }

    [Fact]
    public void EvaluationsIndexViewModel_CalculatesKpisAccurately()
    {
        var vm = new EvaluationsIndexViewModel
        {
            Processes = [
                new EvaluationProcessSummaryViewModel
                {
                    Id = 1,
                    Codigo = "EVAL-001",
                    Nombre = "Evaluación EDR",
                    BuildingBlockId = 10,
                    BuildingBlockName = "Endpoint Detection",
                    DominioName = "Seguridad de Endpoints",
                    EstadoId = 2,
                    EstadoAdopcion = "EVALUACION",
                    TotalEmpresas = 5,
                    EmpresasConAdopcion = 4,
                    EmpresasNoAplica = 1,
                    IsActivo = true
                },
                new EvaluationProcessSummaryViewModel
                {
                    Id = 2,
                    Codigo = "EVAL-002",
                    Nombre = "Evaluación WAF",
                    BuildingBlockId = 15,
                    BuildingBlockName = "Web Application Firewall",
                    DominioName = "Seguridad de Aplicaciones",
                    EstadoId = 2,
                    EstadoAdopcion = "EVALUACION",
                    TotalEmpresas = 4,
                    EmpresasConAdopcion = 2,
                    EmpresasNoAplica = 0,
                    IsActivo = true
                },
                new EvaluationProcessSummaryViewModel
                {
                    Id = 3,
                    Codigo = "EVAL-003",
                    Nombre = "Evaluación DLP Legacy",
                    BuildingBlockId = 20,
                    BuildingBlockName = "Data Loss Prevention",
                    DominioName = "Seguridad de la Información",
                    EstadoId = 3,
                    EstadoAdopcion = "DADO DE BAJA",
                    TotalEmpresas = 3,
                    EmpresasConAdopcion = 1,
                    EmpresasNoAplica = 1,
                    IsActivo = false
                }
            ]
        };

        Assert.Equal(2, vm.TotalActivas);
        Assert.Equal(12, vm.TotalConvocadas);
        Assert.Equal(7, vm.TotalConAdopcion);
        Assert.Equal(2, vm.TotalNoAplica);
    }

    [Fact]
    public void DeactivateEvaluationViewModel_ValidationRequirementsAreEnforced()
    {
        var deact = new DeactivateEvaluationViewModel
        {
            Id = 5,
            Codigo = "EVAL-TSI-2026-005",
            Nombre = "Evaluación Cancelada",
            MotivoBaja = "Cambio en la estrategia corporativa por consolidación de proveedores globales."
        };

        Assert.Equal(5, deact.Id);
        Assert.True(deact.MotivoBaja.Length >= 10);
        Assert.Contains("consolidación", deact.MotivoBaja);
    }

    [Fact]
    public void TechnologyCatalogItem_And_CatalogCollections_AreSupported()
    {
        var item = new TechnologyCatalogItem(42, "CyberArk Privilege Cloud", "Privileged Access Management");
        Assert.Equal(42, item.Id);
        Assert.Equal("CyberArk Privilege Cloud", item.Nombre);
        Assert.Equal("Privileged Access Management", item.Familia);

        var model = new CreateEvaluationViewModel
        {
            Familias = [new CatalogOption(1, "Seguridad Perimetral"), new CatalogOption(2, "Gestión de Identidades")],
            TecnologiasCatalogo = [item, new TechnologyCatalogItem(43, "Palo Alto Prisma Access", "SASE")]
        };

        Assert.Equal(2, model.Familias.Count);
        Assert.Equal(2, model.TecnologiasCatalogo.Count);
        Assert.Equal("CyberArk Privilege Cloud", model.TecnologiasCatalogo[0].Nombre);
    }
}