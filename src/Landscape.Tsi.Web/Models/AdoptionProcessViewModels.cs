using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public sealed class AdoptionProcessIndexViewModel
{
    public required IReadOnlyList<AdoptionProcessSummaryDto> Processes { get; init; }
    public int TotalProcesses => Processes.Count;
    public int TotalEmpresasConvocadas => Processes.Sum(p => p.TotalEmpresasConvocadas);
    public int TotalEmpresasImplementadas => Processes.Sum(p => p.TotalEmpresasImplementadas);
    public int TotalEmpresasNoAplica => Processes.Sum(p => p.TotalEmpresasNoAplica);
}

public sealed class AdoptionProcessDetailViewModel
{
    public required AdoptionProcessDetailDto Process { get; init; }
    public required IReadOnlyList<CatalogOption> Technologies { get; init; }
    public required IReadOnlyList<CatalogOption> Companies { get; init; }
    public required IReadOnlyList<CatalogOption> OperationTypes { get; init; }
    public required IReadOnlyList<CatalogOption> WorkModes { get; init; }
    public required IReadOnlyList<CatalogOption> AdoptionStates { get; init; }
    public string? ReturnUrl { get; init; }
}

public sealed class CreateAdoptionProcessViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int BuildingBlockId { get; set; }
    public int EstadoAdopcionId { get; set; }
    public string? Objetivo { get; set; }
    public string? Alcance { get; set; }
    public string? LiderCorporativo { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime? FechaEstimadaCierre { get; set; }
    public IReadOnlyList<CatalogOption> BuildingBlocks { get; set; } = [];
    public IReadOnlyList<CatalogOption> AdoptionStates { get; set; } = [];
}