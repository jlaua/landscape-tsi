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
                    IsActivo = false,
                    IsCancelada = true
                },
                new EvaluationProcessSummaryViewModel
                {
                    Id = 4,
                    Codigo = "EVAL-004",
                    Nombre = "Evaluación SIEM",
                    BuildingBlockId = 25,
                    BuildingBlockName = "Security Information and Event Management",
                    DominioName = "Monitoreo y SOC",
                    EstadoId = 4,
                    EstadoAdopcion = "ESTANDARIZADO",
                    TotalEmpresas = 6,
                    EmpresasConAdopcion = 6,
                    EmpresasNoAplica = 0,
                    IsActivo = true,
                    IsTerminada = true
                }
            ]
        };

        Assert.Equal(2, vm.TotalActivas);
        Assert.Equal(1, vm.TotalTerminadas);
        Assert.Equal(1, vm.TotalCanceladas);
        Assert.Equal(18, vm.TotalConvocadas);
        Assert.Equal(13, vm.TotalConAdopcion);
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

    [Fact]
    public void FinalizeEvaluationWithStandardViewModel_InitializesCorrectly()
    {
        var vm = new FinalizeEvaluationWithStandardViewModel
        {
            ProcesoId = 10,
            CodigoProceso = "PROC-2026-WAAP",
            NombreProceso = "Evaluación WAAP",
            BuildingBlockId = 3,
            BuildingBlockNombre = "WAAP",
            TecnologiaId = 44,
            RolEstandar = "PRINCIPAL",
            FechaInicioVigencia = new DateTime(2026, 9, 15),
            MotivoAdjudicacion = "Sustitución tecnológica por consolidación multicloud",
            SustentoArquitectura = "Mejor costo por request y capacidades de bot protection avanzadas",
            NumeroContratoCorporativo = "CORP-WAAP-2026-001",
            MontoContratoCorporativo = 500000m,
            MonedaContratoCorporativo = "USD",
            EsPaygContratoCorporativo = false,
            SubsidiariasAlineadasIds = [1, 2, 3]
        };

        Assert.Equal(10, vm.ProcesoId);
        Assert.Equal("PROC-2026-WAAP", vm.CodigoProceso);
        Assert.Equal(44, vm.TecnologiaId);
        Assert.Equal("PRINCIPAL", vm.RolEstandar);
        Assert.Equal(3, vm.SubsidiariasAlineadasIds.Count);
        Assert.False(vm.EsPaygContratoCorporativo);
        Assert.Equal(500000m, vm.MontoContratoCorporativo);
    }

    [Fact]
    public void ImplementedTechnologyDto_SupportsCorporateInstanceFlag()
    {
        var dtoCorp = new ImplementedTechnologyDto(
            1, 2, "BCP", 3, 44, "Akamai Kona", "Akamai", true, "v2.0", "ALINEADO", null, [], [], true);
        
        var dtoLocal = new ImplementedTechnologyDto(
            2, 5, "Mibanco", 3, 44, "Akamai Kona", "Akamai", true, "v1.8", "ALINEADO", null, [], [], false);

        Assert.True(dtoCorp.EsInstanciaCorporativa);
        Assert.False(dtoLocal.EsInstanciaCorporativa);
    }

    [Fact]
    public void CreateEvaluation_OnlyParticipatingCompanies_AreEligibleForAsIs()
    {
        var model = new CreateEvaluationViewModel
        {
            Codigo = "EVAL-TEST-001",
            Nombre = "Evaluación WAAP",
            BuildingBlockId = 1,
            Subsidiaries = [
                new SubsidiaryCheckboxItem { EmpresaId = 1, EmpresaNombre = "ASB Panamá", Selected = false, Aplica = false, JustificacionNoAplica = "Se considerará Credicorp Capital" },
                new SubsidiaryCheckboxItem { EmpresaId = 2, EmpresaNombre = "BCP Bolivia", Selected = true, Aplica = true },
                new SubsidiaryCheckboxItem { EmpresaId = 3, EmpresaNombre = "BCP Miami", Selected = true, Aplica = true },
                new SubsidiaryCheckboxItem { EmpresaId = 4, EmpresaNombre = "BCP Perú", Selected = false, Aplica = false, JustificacionNoAplica = "Tienen Radware contratado hasta 2028" },
                new SubsidiaryCheckboxItem { EmpresaId = 5, EmpresaNombre = "Empresa No Convocada", Selected = false, Aplica = false }
            ],
            SubsidiaryAsIsList = [
                new SubsidiaryAsIsInputModel { EmpresaId = 1, EmpresaNombre = "ASB Panamá", TieneTecnologia = true, TecnologiaId = 10 },
                new SubsidiaryAsIsInputModel { EmpresaId = 2, EmpresaNombre = "BCP Bolivia", TieneTecnologia = true, TecnologiaId = 20 },
                new SubsidiaryAsIsInputModel { EmpresaId = 3, EmpresaNombre = "BCP Miami", TieneTecnologia = true, TecnologiaId = 20 },
                new SubsidiaryAsIsInputModel { EmpresaId = 4, EmpresaNombre = "BCP Perú", TieneTecnologia = true, TecnologiaId = 30 },
                new SubsidiaryAsIsInputModel { EmpresaId = 5, EmpresaNombre = "Empresa No Convocada", TieneTecnologia = true, TecnologiaId = 40 }
            ]
        };

        // Participan solo las que tienen Selected = true
        var participatingIds = model.Subsidiaries
            .Where(s => s.Selected)
            .Select(s => s.EmpresaId)
            .ToHashSet();

        // Empresas a registrar en TProcesoAdopcionEmpresa (participantes y no participantes con justificación)
        var companiesToConvene = model.Subsidiaries
            .Where(s => s.Selected || !string.IsNullOrWhiteSpace(s.JustificacionNoAplica))
            .ToList();

        var eligibleAsIs = model.SubsidiaryAsIsList
            .Where(a => participatingIds.Contains(a.EmpresaId) && a.TieneTecnologia && a.TecnologiaId.HasValue && a.TecnologiaId.Value > 0)
            .ToList();

        Assert.Equal(2, eligibleAsIs.Count);
        Assert.Contains(eligibleAsIs, a => a.EmpresaId == 2);
        Assert.Contains(eligibleAsIs, a => a.EmpresaId == 3);
        Assert.DoesNotContain(eligibleAsIs, a => a.EmpresaId == 1); // No seleccionada
        Assert.DoesNotContain(eligibleAsIs, a => a.EmpresaId == 4); // No seleccionada
        Assert.DoesNotContain(eligibleAsIs, a => a.EmpresaId == 5); // No seleccionada

        // Verificar que las empresas no participantes con justificación se mantienen para registro con Aplica=false
        Assert.Equal(4, companiesToConvene.Count);
        Assert.Contains(companiesToConvene, c => c.EmpresaId == 1 && !c.Selected && !string.IsNullOrWhiteSpace(c.JustificacionNoAplica));
        Assert.Contains(companiesToConvene, c => c.EmpresaId == 4 && !c.Selected && !string.IsNullOrWhiteSpace(c.JustificacionNoAplica));
        Assert.DoesNotContain(companiesToConvene, c => c.EmpresaId == 5); // Ni seleccionada ni justificada
    }
}