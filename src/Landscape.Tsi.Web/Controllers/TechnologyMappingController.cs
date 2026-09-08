using System.Security.Claims;
using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.CatalogView)]
[Route("Administration/TechnologyMapping")]
public sealed class TechnologyMappingController(IBuildingBlockTechnologyMappingService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(TechnologyMappingQuery query, int? reportFamilyId, int reportPage = 1, CancellationToken cancellationToken = default)
    {
        var page = await service.ListAsync(query, cancellationToken);
        var familyReport = await service.GetFamilyBuildingBlockReportAsync(reportFamilyId, reportPage, 10, cancellationToken);
        var rows = page.Items.Select(row => row with
        {
            DetailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "building-block", id = row.BuildingBlockId }) ?? "#",
            ManageRoute = Url.Action(nameof(BuildingBlockRelations), new { buildingBlockId = row.BuildingBlockId, returnUrl = Url.Action(nameof(Index)) }) ?? "#",
            Technologies = row.Technologies.Select(x => x with
            {
                DetailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "tecnologia-tsi", id = x.Id }) ?? "#"
            }).ToArray()
        }).ToArray();
        var reportRows = familyReport.Rows.Select(row => row with
        {
            DetailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "building-block", id = row.BuildingBlockId }) ?? "#"
        }).ToArray();
        return View(new TechnologyMappingViewModel
        {
            Query = query,
            Page = page with { Items = rows },
            FamilyReport = familyReport with { Rows = reportRows }
        });
    }

    [HttpGet("Unassigned")]
    public async Task<IActionResult> Unassigned(UnassignedTechnologyQuery query, CancellationToken cancellationToken)
    {
        var page = await service.ListUnassignedAsync(query, cancellationToken);
        return View(new UnassignedTechnologiesViewModel { Query = query, Page = page with
        {
            Items = page.Items.Select(item => item with { DetailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "tecnologia-tsi", id = item.Id }) ?? "#" }).ToArray()
        }});
    }

    [HttpGet("Technology/{technologyId:int}/Relations")]
    public async Task<IActionResult> TechnologyRelations(int technologyId, string? returnUrl, CancellationToken cancellationToken)
    {
        var relation = await service.GetTechnologyRelationsAsync(technologyId, cancellationToken);
        if (relation is null) return NotFound();
        return View(new TechnologyRelationsViewModel { Relation = relation with { BuildingBlocks = relation.BuildingBlocks.Select(x => x with { DetailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "building-block", id = x.Id }) ?? "#" }).ToArray() }, ReturnUrl = returnUrl });
    }

    [HttpGet("Family/{familyId:int}/BuildingBlocks")]
    public async Task<IActionResult> FamilyBuildingBlocks(int familyId, int page = 1, CancellationToken cancellationToken = default)
    {
        var report = await service.GetFamilyBuildingBlockReportAsync(familyId, page, 10, cancellationToken);
        return Json(new
        {
            familyId,
            familyName = report.SelectedFamilyName,
            page = report.Page,
            pageSize = report.PageSize,
            totalCount = report.TotalCount,
            totalPages = report.TotalCount == 0 ? 1 : (int)Math.Ceiling(report.TotalCount / (double)report.PageSize),
            rows = report.Rows.Select(row => new
            {
                row.BuildingBlockId,
                row.BuildingBlockName,
                row.TechnologyNames,
                row.TechnologyCount,
                detailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "building-block", id = row.BuildingBlockId })
            })
        });
    }

    [HttpPost("Technology/{technologyId:int}/Relations")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TechnologyRelations(int technologyId, int[] buildingBlockIds, string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId(); if (actor is null) return Forbid();
        try
        {
            await service.SaveTechnologyRelationsAsync(technologyId, buildingBlockIds, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        }
        catch (TechnologyMappingConflictException)
        {
            TempData["ErrorMessage"] = "No fue posible actualizar las relaciones. Verifique que no exista una relación duplicada e inténtelo nuevamente.";
            return RedirectLocal(returnUrl, Url.Action(nameof(TechnologyRelations), new { technologyId }) ?? "/Administration/TechnologyMapping");
        }
        TempData["SuccessMessage"] = "Las relaciones de la Tecnología TSI se actualizaron correctamente.";
        return RedirectLocal(returnUrl, Url.Action(nameof(Index))!);
    }

    [HttpGet("BuildingBlock/{buildingBlockId:int}/Relations")]
    public async Task<IActionResult> BuildingBlockRelations(int buildingBlockId, string? search, int? familyId, string? returnUrl, CancellationToken cancellationToken)
    {
        var relation = await service.GetBuildingBlockRelationsAsync(buildingBlockId, search, familyId, cancellationToken);
        if (relation is null) return NotFound();
        var families = (await service.ListAsync(new TechnologyMappingQuery(PageSize: 10), cancellationToken)).Families;
        return View(new BuildingTechnologyRelationsViewModel { Relation = relation with { Technologies = relation.Technologies.Select(x => x with { DetailRoute = Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "tecnologia-tsi", id = x.Id }) ?? "#" }).ToArray() }, Search = search, FamilyId = familyId, Families = families, ReturnUrl = returnUrl });
    }

    [HttpPost("BuildingBlock/{buildingBlockId:int}/Relations")]
    [Authorize(Policy = Permissions.CatalogEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuildingBlockRelations(int buildingBlockId, int[] technologyIds, string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId(); if (actor is null) return Forbid();
        try
        {
            await service.SaveBuildingBlockRelationsAsync(buildingBlockId, technologyIds, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        }
        catch (TechnologyMappingConflictException)
        {
            TempData["ErrorMessage"] = "No fue posible actualizar las relaciones. Verifique que no exista una relación duplicada e inténtelo nuevamente.";
            return RedirectLocal(returnUrl, Url.Action(nameof(BuildingBlockRelations), new { buildingBlockId }) ?? "/Administration/TechnologyMapping");
        }
        TempData["SuccessMessage"] = "Las relaciones del Building Block se actualizaron correctamente.";
        return RedirectLocal(returnUrl, Url.Action("CatalogDetails", "MasterTables", new { catalogRoute = "building-block", id = buildingBlockId })!);
    }

    [HttpPost("BuildingBlock/{buildingBlockId:int}/Technology/{technologyId:int}/Associate")]
    [Authorize(Policy = Permissions.CatalogEdit)] [ValidateAntiForgeryToken]
    public async Task<IActionResult> Associate(int buildingBlockId, int technologyId, string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId(); if (actor is null) return Forbid();
        await service.AssociateAsync(buildingBlockId, technologyId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        return RedirectLocal(returnUrl, Url.Action(nameof(BuildingBlockRelations), new { buildingBlockId })!);
    }

    [HttpPost("BuildingBlock/{buildingBlockId:int}/Technology/{technologyId:int}/Disassociate")]
    [Authorize(Policy = Permissions.CatalogEdit)] [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disassociate(int buildingBlockId, int technologyId, string? returnUrl, CancellationToken cancellationToken)
    {
        var actor = ActorId(); if (actor is null) return Forbid();
        await service.DisassociateAsync(buildingBlockId, technologyId, actor.Value, HttpContext.TraceIdentifier, cancellationToken);
        return RedirectLocal(returnUrl, Url.Action(nameof(BuildingBlockRelations), new { buildingBlockId })!);
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private IActionResult RedirectLocal(string? returnUrl, string fallback)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect(fallback);
}
