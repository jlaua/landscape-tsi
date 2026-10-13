using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Web.Models;

namespace Landscape.Tsi.Tests.Adoption;

public sealed class ImplementationProcessUnitTests
{
    [Fact]
    public void CreateImplementationProcessCommand_InitializesPropertiesCorrectly()
    {
        var actor = Guid.NewGuid();
        var inicioVigencia = new DateTime(2026, 1, 1);
        var finVigencia = new DateTime(2028, 12, 31);
        var sub = new ConveneImplementationCompanyInput(1, 5, true, null, new DateTime(2026, 3, 1), "Ing. Carlos Mendoza", "https://wiki.corp/waf", "IMPLEMENTADO");

        var cmd = new CreateImplementationProcessCommand(
            Codigo: "IMPL-TSI-2026-001",
            Nombre: "Implementación WAAP Cloudflare",
            FamiliaId: 3,
            TecnologiaEstandarId: 45,
            BuildingBlockId: 12,
            FechaInicioVigenciaContrato: inicioVigencia,
            FechaFinVigenciaContrato: finVigencia,
            NumeroContratoCorporativo: "CTR-2026-WAAP-001",
            EstadoAdopcionId: 1,
            Objetivo: "Proteger el perímetro de aplicaciones corporativas",
            Alcance: "Subsidiarias financieras y de seguros",
            LiderCorporativo: "Ing. Roberto TSI",
            FechaInicio: new DateTime(2026, 1, 15),
            FechaEstimadaCierre: new DateTime(2026, 12, 31),
            ProcesoEvaluacionOrigenId: 10,
            Empresas: [sub],
            ActorUserId: actor,
            CorrelationId: "corr-123");

        Assert.Equal("IMPL-TSI-2026-001", cmd.Codigo);
        Assert.Equal("Implementación WAAP Cloudflare", cmd.Nombre);
        Assert.Equal(3, cmd.FamiliaId);
        Assert.Equal(45, cmd.TecnologiaEstandarId);
        Assert.Equal(12, cmd.BuildingBlockId);
        Assert.Equal(inicioVigencia, cmd.FechaInicioVigenciaContrato);
        Assert.Equal(finVigencia, cmd.FechaFinVigenciaContrato);
        Assert.Equal("CTR-2026-WAAP-001", cmd.NumeroContratoCorporativo);
        Assert.Equal(10, cmd.ProcesoEvaluacionOrigenId);
        Assert.Single(cmd.Empresas);
        Assert.Equal(actor, cmd.ActorUserId);
    }

    [Fact]
    public void TProcesoAdopcionTSI_SupportsImplementationPhaseFields()
    {
        var proc = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 1,
            TipoProceso = "IMPLEMENTACION",
            CodigoProceso = "IMPL-TSI-2026-WAAP",
            NombreProceso = "Despliegue WAAP",
            IdFamilia = 2,
            IdTecnologiaEstandar = 10,
            IdBuildingBlock = 5,
            FechaInicioVigenciaContrato = new DateTime(2026, 1, 1),
            FechaFinVigenciaContrato = new DateTime(2027, 1, 1),
            NumeroContratoCorporativo = "CTR-FRAMEWORK-01"
        };

        Assert.Equal("IMPLEMENTACION", proc.TipoProceso);
        Assert.Equal(2, proc.IdFamilia);
        Assert.Equal(10, proc.IdTecnologiaEstandar);
        Assert.Equal(5, proc.IdBuildingBlock);
        Assert.Equal(new DateTime(2026, 1, 1), proc.FechaInicioVigenciaContrato);
        Assert.Equal(new DateTime(2027, 1, 1), proc.FechaFinVigenciaContrato);
        Assert.Equal("CTR-FRAMEWORK-01", proc.NumeroContratoCorporativo);
    }

    [Fact]
    public void TProcesoAdopcionEmpresa_SupportsTechnicalOnboardingFields()
    {
        var pae = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 10,
            IdProcesoAdopcionTSI = 1,
            IdEmpresaSubsidiaria = 5,
            Aplica = true,
            FechaInicioAdopcion = new DateTime(2026, 4, 15),
            EspecialistaTecnico = "Ana Gómez",
            UrlArquitectura = "https://confluence.corp/display/ARCH/WAAP-Peru",
            RutaArchivoArquitectura = "uploads/architectures/1/10_diagram.pdf",
            NombreArchivoArquitectura = "diagram.pdf",
            EstadoImplementacion = "IMPLEMENTADO",
            TecnologiaAsIs = "Cloudflare Enterprise WAAP"
        };

        Assert.Equal(10, pae.IdProcesoAdopcionEmpresa);
        Assert.Equal(new DateTime(2026, 4, 15), pae.FechaInicioAdopcion);
        Assert.Equal("Ana Gómez", pae.EspecialistaTecnico);
        Assert.Equal("https://confluence.corp/display/ARCH/WAAP-Peru", pae.UrlArquitectura);
        Assert.Equal("uploads/architectures/1/10_diagram.pdf", pae.RutaArchivoArquitectura);
        Assert.Equal("diagram.pdf", pae.NombreArchivoArquitectura);
        Assert.Equal("IMPLEMENTADO", pae.EstadoImplementacion);
        Assert.Equal("Cloudflare Enterprise WAAP", pae.TecnologiaAsIs);
    }

    [Fact]
    public void ImplementationReportsDto_CalculatesAlignmentMetricsProperly()
    {
        var filaAlineada = new ImplementationCompanyAlignmentRowDto(1, 10, "Empresa A", "Perú", "Banca", true, null, "IMPLEMENTADO", new DateTime(2026, 2, 1), "Ing. Perez", "https://wiki", "hld.pdf", true);
        var filaDespliegue = new ImplementationCompanyAlignmentRowDto(2, 20, "Empresa B", "Chile", "Seguros", true, null, "EN_DESPLIEGUE", new DateTime(2026, 5, 1), "Ing. Silva", null, null, false);
        var filaPendiente = new ImplementationCompanyAlignmentRowDto(3, 30, "Empresa C", "Colombia", "Retail", true, null, "PENDIENTE", null, null, null, null, false);
        var filaNoAplica = new ImplementationCompanyAlignmentRowDto(4, 40, "Empresa D", "Panamá", "Inversiones", false, "Sin apps públicas", "NO_APLICA", null, null, null, null, false);

        var dto = new ImplementationReportsDto(
            ProcesoId: 1,
            CodigoProceso: "IMPL-2026-001",
            NombreProceso: "Despliegue WAAP",
            FamiliaNombre: "Web Security",
            TecnologiaEstandarNombre: "Cloudflare",
            VendorNombre: "Cloudflare Inc.",
            FechaInicioVigenciaContrato: new DateTime(2026, 1, 1),
            FechaFinVigenciaContrato: new DateTime(2027, 1, 1),
            NumeroContratoCorporativo: "CTR-01",
            LiderCorporativo: "Líder TSI",
            TotalEmpresasConvocadas: 4,
            TotalEmpresasAplicables: 3,
            TotalEmpresasAlineadas: 1,
            TotalEmpresasEnDespliegue: 1,
            TotalEmpresasPendientes: 1,
            TotalEmpresasNoAplica: 1,
            PorcentajeAlineamiento: 33.3,
            FilasAlineamiento: [filaAlineada, filaDespliegue, filaPendiente, filaNoAplica],
            PeriodosTimeline: [],
            FilasTimeline: [],
            EmpresasAcumuladasPorHito: new Dictionary<string, int>(),
            PorcentajeAcumuladoPorHito: new Dictionary<string, double>());

        Assert.Equal(4, dto.TotalEmpresasConvocadas);
        Assert.Equal(3, dto.TotalEmpresasAplicables);
        Assert.Equal(1, dto.TotalEmpresasAlineadas);
        Assert.Equal(1, dto.TotalEmpresasEnDespliegue);
        Assert.Equal(1, dto.TotalEmpresasPendientes);
        Assert.Equal(1, dto.TotalEmpresasNoAplica);
        Assert.Equal(33.3, dto.PorcentajeAlineamiento);
        Assert.True(dto.FilasAlineamiento[0].TieneArchivo);
        Assert.False(dto.FilasAlineamiento[1].TieneArchivo);
    }

    [Fact]
    public void ImplementationsIndexViewModel_CalculatesCountersCorrectly()
    {
        var proc1 = new ImplementationProcessSummaryViewModel
        {
            Id = 1,
            Codigo = "IMPL-01",
            TotalEmpresas = 5,
            EmpresasAlineadas = 5,
            PorcentajeAdopcion = 100.0,
            IsActivo = false,
            IsTerminada = true,
            IsCancelada = false
        };

        var proc2 = new ImplementationProcessSummaryViewModel
        {
            Id = 2,
            Codigo = "IMPL-02",
            TotalEmpresas = 10,
            EmpresasAlineadas = 5,
            PorcentajeAdopcion = 50.0,
            IsActivo = true,
            IsTerminada = false,
            IsCancelada = false
        };

        var model = new ImplementationsIndexViewModel
        {
            Processes = [proc1, proc2]
        };

        Assert.Equal(1, model.TotalActivas);
        Assert.Equal(1, model.TotalTerminadas);
        Assert.Equal(0, model.TotalCanceladas);
        Assert.Equal(15, model.TotalConvocadas);
        Assert.Equal(10, model.TotalAlineadas);
        Assert.Equal(75.0, model.PorcentajePromedioAdopcion);
    }

    [Fact]
    public void UpdateImplementationCompanyCommand_SupportsTecnologiaAsIs()
    {
        var cmd = new UpdateImplementationCompanyCommand(
            ProcesoId: 1,
            ProcesoEmpresaId: 10,
            Aplica: true,
            JustificacionNoAplica: null,
            FechaInicioAdopcion: new DateTime(2026, 6, 1),
            EspecialistaTecnico: "Juan Valenzuela",
            IdContactoEspecialista: null,
            UrlArquitectura: "https://wiki.corp/waf",
            RutaArchivoArquitectura: null,
            NombreArchivoArquitectura: null,
            EstadoImplementacion: "EN_DESPLIEGUE",
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "corr-test",
            TecnologiaAsIs: "AWS WAF Custom");

        Assert.Equal("AWS WAF Custom", cmd.TecnologiaAsIs);
        Assert.Equal("EN_DESPLIEGUE", cmd.EstadoImplementacion);
    }

    [Fact]
    public void ImplementationCompanyAlignmentRowDto_StoresTecnologiaAsIs()
    {
        var row = new ImplementationCompanyAlignmentRowDto(
            ProcesoEmpresaId: 1,
            EmpresaId: 10,
            EmpresaNombre: "Empresa X",
            Pais: "Perú",
            Rubro: "Financiero",
            Aplica: true,
            JustificacionNoAplica: null,
            EstadoImplementacion: "IMPLEMENTADO",
            FechaInicioAdopcion: new DateTime(2026, 1, 1),
            EspecialistaTecnico: "Carlos M.",
            UrlArquitectura: null,
            NombreArchivoArquitectura: null,
            TieneArchivo: false,
            TecnologiaAsIs: "Cloudflare Enterprise");

        Assert.Equal("Cloudflare Enterprise", row.TecnologiaAsIs);
    }

    [Fact]
    public void ImplementationDetailsViewModel_HandlesSubsidiaryAndPartnerSpecialists()
    {
        var model = new ImplementationDetailsViewModel
        {
            EspecialistasEmpresas = [
                new CompanySpecialistDto
                {
                    Id = 101,
                    EmpresaId = 1,
                    Nombre = "Ing. BCP Cloud",
                    Rol = "Arquitecto Cloud",
                    Plataformas = "AWS, Cloudflare",
                    Email = "cloud@bcp.com.pe"
                }
            ],
            EspecialistasPartners = [
                new PartnerSpecialistDto
                {
                    Id = 201,
                    PartnerId = 10,
                    PartnerNombre = "Cloudflare Partner LATAM",
                    Nombre = "Ing. Especialista Partner",
                    Rol = "Lead Security Architect",
                    Email = "partner@security.com",
                    TecnologiaId = 45
                }
            ]
        };

        Assert.Single(model.EspecialistasEmpresas);
        Assert.Equal(101, model.EspecialistasEmpresas[0].Id);
        Assert.Equal("Ing. BCP Cloud", model.EspecialistasEmpresas[0].Nombre);

        Assert.Single(model.EspecialistasPartners);
        Assert.Equal(201, model.EspecialistasPartners[0].Id);
        Assert.Equal("Cloudflare Partner LATAM", model.EspecialistasPartners[0].PartnerNombre);
        Assert.Equal(45, model.EspecialistasPartners[0].TecnologiaId);
    }

    [Fact]
    public void EditImplementationViewModel_BindsPropertiesProperly()
    {
        var model = new EditImplementationViewModel
        {
            Id = 5,
            Codigo = "IMPL-2026-005",
            Nombre = "Implementación EDR CrowdStrike",
            FamiliaId = 2,
            TecnologiaEstandarId = 15,
            BuildingBlockId = 4,
            FechaInicioVigenciaContrato = new DateTime(2026, 2, 1),
            FechaFinVigenciaContrato = new DateTime(2028, 2, 1),
            NumeroContratoCorporativo = "CTR-CS-2026",
            EstadoAdopcionId = 3,
            Objetivo = "Proteger endpoints",
            Alcance = "Todas las subsidiarias",
            LiderCorporativo = "Líder Ciberseguridad",
            FechaInicio = new DateTime(2026, 2, 15),
            FechaEstimadaCierre = new DateTime(2027, 2, 15)
        };

        Assert.Equal(5, model.Id);
        Assert.Equal("IMPL-2026-005", model.Codigo);
        Assert.Equal("Implementación EDR CrowdStrike", model.Nombre);
        Assert.Equal(2, model.FamiliaId);
        Assert.Equal(15, model.TecnologiaEstandarId);
        Assert.Equal(4, model.BuildingBlockId);
        Assert.Equal("CTR-CS-2026", model.NumeroContratoCorporativo);
        Assert.Equal(3, model.EstadoAdopcionId);
    }

    [Fact]
    public void ManageImplementationCompaniesFormModel_InitializesEmptyCompanyList()
    {
        var form = new ManageImplementationCompaniesFormModel();
        Assert.NotNull(form.SelectedCompanyIds);
        Assert.Empty(form.SelectedCompanyIds);
    }

    [Fact]
    public void CreateSpecialistFormModel_StoresInputFields()
    {
        var form = new CreateSpecialistFormModel
        {
            EmpresaId = 10,
            Nombre = "Ing. Martin Vizcarra",
            Rol = "DevSecOps",
            Email = "mvizcarra@subsidiaria.com",
            Telefono = "+51 987654321",
            Plataformas = "AWS, Kubernetes"
        };

        Assert.Equal(10, form.EmpresaId);
        Assert.Equal("Ing. Martin Vizcarra", form.Nombre);
        Assert.Equal("DevSecOps", form.Rol);
        Assert.Equal("mvizcarra@subsidiaria.com", form.Email);
        Assert.Equal("+51 987654321", form.Telefono);
        Assert.Equal("AWS, Kubernetes", form.Plataformas);
    }
}

