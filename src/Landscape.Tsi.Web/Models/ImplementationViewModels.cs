using System.ComponentModel.DataAnnotations;
using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class ImplementationsIndexViewModel
{
    public IReadOnlyList<ImplementationProcessSummaryViewModel> Processes { get; set; } = [];
    public string? Search { get; set; }
    public int? FamiliaId { get; set; }
    public int? EstadoId { get; set; }
    public IReadOnlyList<CatalogOption> Familias { get; set; } = [];
    public IReadOnlyList<CatalogOption> EstadosAdopcion { get; set; } = [];

    public int TotalActivas => Processes.Count(p => p.IsActivo);
    public int TotalTerminadas => Processes.Count(p => p.IsTerminada);
    public int TotalCanceladas => Processes.Count(p => p.IsCancelada);
    public int TotalConvocadas => Processes.Sum(p => p.TotalEmpresas);
    public int TotalAlineadas => Processes.Sum(p => p.EmpresasAlineadas);
    public double PorcentajePromedioAdopcion => Processes.Count > 0 ? Math.Round(Processes.Average(p => p.PorcentajeAdopcion), 1) : 0.0;
}

public sealed class ImplementationProcessSummaryViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int? FamiliaId { get; set; }
    public string FamiliaNombre { get; set; } = string.Empty;
    public int? TecnologiaEstandarId { get; set; }
    public string TecnologiaEstandarNombre { get; set; } = string.Empty;
    public int? BuildingBlockId { get; set; }
    public string BuildingBlockNombre { get; set; } = string.Empty;
    public DateTime? FechaInicioVigenciaContrato { get; set; }
    public DateTime? FechaFinVigenciaContrato { get; set; }
    public string? NumeroContratoCorporativo { get; set; }
    public int EstadoId { get; set; }
    public string EstadoAdopcion { get; set; } = string.Empty;
    public string? LiderCorporativo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaEstimadaCierre { get; set; }
    public int TotalEmpresas { get; set; }
    public int EmpresasAlineadas { get; set; }
    public int EmpresasEnDespliegue { get; set; }
    public int EmpresasPendientes { get; set; }
    public int EmpresasNoAplica { get; set; }
    public double PorcentajeAdopcion { get; set; }
    public bool IsActivo { get; set; } = true;
    public bool IsTerminada { get; set; }
    public bool IsCancelada { get; set; }
}

public sealed class CreateImplementationViewModel
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la solicitud de implementación es obligatorio.")]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public int? FamiliaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar la tecnología estándar decretada.")]
    public int TecnologiaEstandarId { get; set; }

    public int? BuildingBlockId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaInicioVigenciaContrato { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaFinVigenciaContrato { get; set; }

    [StringLength(100)]
    public string? NumeroContratoCorporativo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un estado inicial.")]
    public int EstadoAdopcionId { get; set; } = 1;

    [StringLength(1000)]
    public string? Objetivo { get; set; }

    [StringLength(1000)]
    public string? Alcance { get; set; }

    [StringLength(150)]
    public string? LiderCorporativo { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime? FechaEstimadaCierre { get; set; }

    public int? ProcesoEvaluacionOrigenId { get; set; }

    // Catálogos
    public IReadOnlyList<CatalogOption> Familias { get; set; } = [];
    public IReadOnlyList<CatalogOption> TecnologiasDisponibles { get; set; } = [];
    public IReadOnlyList<StandardTechnologyDto> EstandaresDecretados { get; set; } = [];
    public IReadOnlyList<CatalogOption> BuildingBlocks { get; set; } = [];
    public IReadOnlyList<CatalogOption> EstadosAdopcion { get; set; } = [];
    public IReadOnlyList<CatalogOption> EvaluacionesPrevias { get; set; } = [];
    public IReadOnlyList<CatalogOption> EspecialistasPartnersDisponibles { get; set; } = [];

    // Convocatoria de empresas
    public List<ImplementationSubsidiaryCheckboxItem> Subsidiaries { get; set; } = [];
}

public sealed class ImplementationSubsidiaryCheckboxItem
{
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public string? Rubro { get; set; }
    public bool Selected { get; set; } = true;
    public bool Aplica { get; set; } = true;
    public string? JustificacionNoAplica { get; set; }
    public int? ContactoFocalId { get; set; }
    public DateTime? FechaInicioAdopcion { get; set; }
    public string? EspecialistaTecnico { get; set; }
    public string? UrlArquitectura { get; set; }
    public string EstadoImplementacion { get; set; } = "PENDIENTE";
    public string? TecnologiaAsIs { get; set; }
    public List<CatalogOption> EspecialistasDisponibles { get; set; } = [];
}

public sealed class CompanySpecialistDto
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Rol { get; set; }
    public string? Plataformas { get; set; }
    public string? Email { get; set; }
}

public sealed class PartnerSpecialistDto
{
    public int Id { get; set; }
    public int? PartnerId { get; set; }
    public string PartnerNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Rol { get; set; }
    public string? Email { get; set; }
    public int? TecnologiaId { get; set; }
}

public sealed class ImplementationDetailsViewModel
{
    public ImplementationProcessDetailDto Process { get; set; } = null!;
    public IReadOnlyList<CatalogOption> EstadosAdopcion { get; set; } = [];
    public IReadOnlyList<CatalogOption> Familias { get; set; } = [];
    public IReadOnlyList<CatalogOption> TecnologiasDisponibles { get; set; } = [];
    public IReadOnlyList<CompanySpecialistDto> EspecialistasEmpresas { get; set; } = [];
    public IReadOnlyList<PartnerSpecialistDto> EspecialistasPartners { get; set; } = [];
}

public sealed class UpdateImplementationCompanyFormModel
{
    public int ProcesoId { get; set; }
    public int ProcesoEmpresaId { get; set; }
    public bool Aplica { get; set; } = true;
    public string? JustificacionNoAplica { get; set; }
    public DateTime? FechaInicioAdopcion { get; set; }
    public string? EspecialistaTecnico { get; set; }
    public int? IdContactoEspecialista { get; set; }
    public string? UrlArquitectura { get; set; }
    public string EstadoImplementacion { get; set; } = "PENDIENTE";
    public string? TecnologiaAsIs { get; set; }
    public IFormFile? ArchivoArquitectura { get; set; }
}
