using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Web.Models;

public static class CatalogTreemapBuilder
{
    public static async Task<CatalogTreemapHomeViewModel> BuildAsync(
        CatalogDbContext catalogDbContext,
        CancellationToken cancellationToken = default)
    {
        var domains = await catalogDbContext.Domains.AsNoTracking().ToListAsync(cancellationToken);
        var buildingBlocks = await catalogDbContext.BuildingBlocks.AsNoTracking().ToListAsync(cancellationToken);
        var capabilities = await catalogDbContext.Capabilities.AsNoTracking().ToListAsync(cancellationToken);
        var functionalities = await catalogDbContext.Functionalities.AsNoTracking().ToListAsync(cancellationToken);
        var phases = await catalogDbContext.AdoptionPhases.AsNoTracking().ToListAsync(cancellationToken);
        var implemented = await catalogDbContext.ImplementedTechnologies.AsNoTracking().ToListAsync(cancellationToken);

        var phaseMap = phases.ToDictionary(p => p.Id, p => p.Nombre ?? "—");

        var capToBb = capabilities.Where(c => c.IdBuildingBlock.HasValue)
            .ToDictionary(c => c.Id, c => c.IdBuildingBlock!.Value);

        var funcCountByBb = functionalities
            .Where(f => f.IdCapacidad.HasValue && capToBb.ContainsKey(f.IdCapacidad.Value))
            .GroupBy(f => capToBb[f.IdCapacidad!.Value])
            .ToDictionary(g => g.Key, g => g.Count());

        var empCountByBb = implemented
            .Where(i => i.IdBuildingBlock.HasValue)
            .GroupBy(i => i.IdBuildingBlock!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.IdEmpresaSubsidiaria).Distinct().Count());

        var totalBbs = buildingBlocks.Count;

        var domainCards = new List<DomainRectCardViewModel>();
        foreach (var d in domains.OrderByDescending(d => buildingBlocks.Count(b => b.IdDominio == d.Id)))
        {
            var bbsOfDomain = buildingBlocks.Where(b => b.IdDominio == d.Id).OrderBy(b => b.Nombre).ToList();
            var bbCards = bbsOfDomain.Select(b => new BuildingBlockCardViewModel(
                b.Id,
                b.Nombre ?? $"BB #{b.Id}",
                b.Definicion,
                b.Pilar,
                b.IdFase.HasValue && phaseMap.TryGetValue(b.IdFase.Value, out var ph) ? ph : "En Definición",
                funcCountByBb.TryGetValue(b.Id, out var fCount) ? fCount : 0,
                empCountByBb.TryGetValue(b.Id, out var eCount) ? eCount : 0
            )).ToList();

            var color = CatalogDomainColorPalette.GetColorForDomain(d.Dominio, d.Id);
            var pct = totalBbs > 0 ? Math.Round((double)bbCards.Count / totalBbs * 100, 1) : 0;

            domainCards.Add(new DomainRectCardViewModel(
                d.Id,
                d.Dominio ?? "Dominio Sin Nombre",
                d.DescripcionDominio,
                color.PastelHex,
                color.BorderHex,
                color.TextHex,
                pct,
                bbCards
            ));
        }

        return new CatalogTreemapHomeViewModel
        {
            Dominios = domainCards,
            TotalBuildingBlocks = totalBbs,
            TotalDominios = domains.Count,
            TotalFuncionalidades = functionalities.Count,
            TotalImplementaciones = implemented.Count
        };
    }
}