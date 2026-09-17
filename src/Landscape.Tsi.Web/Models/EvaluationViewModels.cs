using System.ComponentModel.DataAnnotations;

using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class EvaluationsIndexViewModel
{
    public IReadOnlyList<EvaluationProcessSummaryViewModel> Processes { get; set; } = [];
    public string? Search { get; set; }
    public int? DominioId { get; set; }
    public int? EstadoId { get; set; }
    public IReadOnlyList<CatalogOption> Dominios { get; set; } = [];
    public IReadOnlyList<CatalogOption> BuildingBlocks { get; set; } = [];
    public IReadOnlyList<CatalogOption> EstadosAdopcion { get; set; } = [];
    public int TotalActivas => Processes.Count(p => p.IsActivo && !p.IsTerminada && !p.IsCancelada);
    public int TotalTerminadas => Processes.Count(p => p.IsTerminada);
    public int TotalCanceladas => Processes.Count(p => p.IsCancelada || !p.IsActivo);
    public int TotalConvocadas => Processes.Sum(p => p.TotalEmpresas);
    public int TotalConAdopcion => Processes.Sum(p => p.EmpresasConAdopcion);
    public int TotalNoAplica => Processes.Sum(p => p.EmpresasNoAplica);
}

public sealed class EvaluationProcessSummaryViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int BuildingBlockId { get; set; }
    public string BuildingBlockName { get; set; } = string.Empty;
    public string DominioName { get; set; } = string.Empty;
    public int EstadoId { get; set; }
    public string EstadoAdopcion { get; set; } = string.Empty;
    public string FaseAdopcion { get; set; } = "EVALUACION";
    public string? LiderCorporativo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaEstimadaCierre { get; set; }
    public int TotalEmpresas { get; set; }
    public int EmpresasConAdopcion { get; set; }
    public int EmpresasNoAplica { get; set; }
    public bool IsActivo { get; set; } = true;
    public bool IsCancelada { get; set; }
    public bool IsTerminada { get; set; }
    public string? EstandarPrincipal { get; set; }
    public string? PartnerPrincipal { get; set; }
}

public sealed class CreateEvaluationViewModel
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la evaluación es obligatorio.")]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un Building Block.")]
    public int BuildingBlockId { get; set; }

    public int DominioId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un estado de adopción TSI.")]
    public int EstadoAdopcionId { get; set; } = 1; // Default: SIN EVALUACION / PRE-CALIFICADA

    [StringLength(1000)]
    public string? Objetivo { get; set; }

    [StringLength(1000)]
    public string? Alcance { get; set; }

    [StringLength(150)]
    public string? LiderCorporativo { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime? FechaEstimadaCierre { get; set; }

    // Catálogos para el formulario
    public IReadOnlyList<CatalogOption> Dominios { get; set; } = [];
    public IReadOnlyList<CatalogOption> BuildingBlocks { get; set; } = [];
    public IReadOnlyList<CatalogOption> EstadosAdopcion { get; set; } = [];
    public IReadOnlyList<CatalogOption> TecnologiasDisponibles { get; set; } = [];
    public IReadOnlyList<TechnologyCatalogItem> TecnologiasCatalogo { get; set; } = [];
    public IReadOnlyList<CatalogOption> Familias { get; set; } = [];
    public IReadOnlyList<CatalogOption> TiposOperacion { get; set; } = [];
    public IReadOnlyList<CatalogOption> ModalidadesLaborales { get; set; } = [];
    public IReadOnlyList<CatalogOption> Vendors { get; set; } = [];
    public IReadOnlyList<CatalogOption> Partners { get; set; } = [];
    public IReadOnlyList<CatalogOption> VersionesDesplegadas { get; set; } = [];

    // Selección múltiple con checkboxes de empresas
    public List<SubsidiaryCheckboxItem> Subsidiaries { get; set; } = [];

    // Diagnóstico AS-IS por subsidiaria participante
    public List<SubsidiaryAsIsInputModel> SubsidiaryAsIsList { get; set; } = [];

    // Estándar Corporativo objetivo
    public int? TecnologiaEstandarId { get; set; }
    public string? TecnologiaEstandarNombre { get; set; }
    public string? NumeroContratoCorporativo { get; set; }
    public DateTime? FechaAdjudicacionCorporativa { get; set; }
    public string? VendorCorporativo { get; set; }
    public string? ContactoVendorCorporativo { get; set; }
    public string? PartnerCorporativo { get; set; }
    public string? ContactoPartnerCorporativo { get; set; }
}

public sealed class AsIsPartnerItemInputModel
{
    public int? PartnerId { get; set; }
    public string? PartnerNombre { get; set; }
    public int? PartnerContactoId { get; set; }
    public string? PartnerContacto { get; set; }
}

public sealed class SubsidiaryCheckboxItem
{
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public string? Pais { get; set; }
    public string? Rubro { get; set; }
    public bool Selected { get; set; } = true;
    public int? ContactoFocalId { get; set; }
    public string? ContactoFocalNombre { get; set; }
    public bool Aplica { get; set; } = true;
    public string? JustificacionNoAplica { get; set; }
    public List<CatalogOption> ContactosDisponibles { get; set; } = [];
}

public sealed class SubsidiaryAsIsInputModel
{
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public bool TieneTecnologia { get; set; } = true;
    public bool EsInstanciaCorporativa { get; set; } = false;
    public int? TecnologiaId { get; set; }
    public string? TecnologiaNombre { get; set; }
    public string? VersionDesplegada { get; set; }
    public int? VendorId { get; set; }
    public string? VendorNombre { get; set; }
    public int? VendorContactoId { get; set; }
    public string? VendorContacto { get; set; }
    public int? PartnerId { get; set; }
    public string? PartnerNombre { get; set; }
    public int? PartnerContactoId { get; set; }
    public string? PartnerContacto { get; set; }
    public List<AsIsPartnerItemInputModel> Partners { get; set; } = [];
    public string? NumeroContrato { get; set; }
    public bool EsPayg { get; set; }
    public DateTime? FechaInicioContrato { get; set; }
    public DateTime? FechaFinContrato { get; set; }
    public decimal? MontoContratado { get; set; }
    public decimal? MontoAnual { get; set; }
    public decimal? MontoTrianual { get; set; }
    public string MonedaContrato { get; set; } = "USD";
    public List<DriverInputModel> Drivers { get; set; } = [];
    public int? TipoOperacionId { get; set; }
    public int? ModalidadLaboralId { get; set; }
    public string? SlaObservaciones { get; set; }
}

public sealed class DriverInputModel
{
    public string? DescripcionDriver { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string Moneda { get; set; } = "USD";
}

public sealed class DeactivateEvaluationViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe ingresar el motivo de la baja.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "El motivo debe tener al menos 10 caracteres.")]
    public string MotivoBaja { get; set; } = string.Empty;
}

public sealed class BuildingBlockCapabilitiesPreviewDto
{
    public int BuildingBlockId { get; set; }
    public string BuildingBlockNombre { get; set; } = string.Empty;
    public string DominioNombre { get; set; } = string.Empty;
    public IReadOnlyList<CapabilityItemDto> Capacidades { get; set; } = [];
}

public sealed class CapabilityItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public IReadOnlyList<string> Funcionalidades { get; set; } = [];
}

public sealed class EditEvaluationViewModel
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de la evaluación es obligatorio.")]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    public int BuildingBlockId { get; set; }
    public string BuildingBlockNombre { get; set; } = string.Empty;
    public string DominioNombre { get; set; } = string.Empty;

    public int EstadoAdopcionId { get; set; }
    public IReadOnlyList<CatalogOption> EstadosAdopcion { get; set; } = [];

    [StringLength(1000)]
    public string? Objetivo { get; set; }

    [StringLength(1000)]
    public string? Alcance { get; set; }

    [StringLength(150)]
    public string? LiderCorporativo { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaEstimadaCierre { get; set; }

    // Estándar Corporativo
    public int? TecnologiaEstandarId { get; set; }
    public string? TecnologiaEstandarNombre { get; set; }
    public string? RolEstandar { get; set; }
    public string? MotivoCambioEstandar { get; set; }
    public string? SustentoArquitecturaEstandar { get; set; }
    public IReadOnlyList<CatalogOption> TecnologiasDisponibles { get; set; } = [];
}

public sealed class FinalizeEvaluationWithStandardViewModel
{
    public int ProcesoId { get; set; }
    public string CodigoProceso { get; set; } = string.Empty;
    public string NombreProceso { get; set; } = string.Empty;
    public int BuildingBlockId { get; set; }
    public string BuildingBlockNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar la tecnología adjudicada como estándar.")]
    public int TecnologiaId { get; set; }
    public string RolEstandar { get; set; } = "PRINCIPAL";
    public DateTime FechaInicioVigencia { get; set; } = DateTime.Today;

    public string? MotivoAdjudicacion { get; set; }
    public string? SustentoArquitectura { get; set; }

    // Contrato Corporativo Maestro (Opcional)
    public string? NumeroContratoCorporativo { get; set; }
    public decimal? MontoContratoCorporativo { get; set; }
    public string MonedaContratoCorporativo { get; set; } = "USD";
    public DateTime? FechaInicioContratoCorporativo { get; set; }
    public DateTime? FechaFinContratoCorporativo { get; set; }
    public DateTime? FechaAdjudicacionContratoCorporativo { get; set; }
    public bool EsPaygContratoCorporativo { get; set; }

    // Drivers Corporativos Negociados
    public List<DriverInputModel> DriversCorporativos { get; set; } = [];

    // Subsidiarias que adoptan de inmediato la instancia corporativa
    public List<int> SubsidiariasAlineadasIds { get; set; } = [];
}

public sealed record TechnologyCatalogItem(int Id, string Nombre, string? Familia = null);