using System.Security.Claims;

using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.AuditView)]
public sealed class AuditController(IAuthorizationAuditQuery query, IAuditRestoreService restoreService, ILogger<AuditController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search, [FromQuery(Name = "action")] string? actionType, string? entity,
        Guid? actorUserId, int? empresaSubsidiariaId, DateOnly? dateFrom, DateOnly? dateTo,
        int page = 1, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local));
        var effectiveFrom = dateFrom ?? today;
        var effectiveTo = dateTo ?? today;
        if (effectiveTo < effectiveFrom)
            return BadRequest("El rango de fechas no es válido.");
        if (!AuditEntityRegistry.TryGetPhysicalTable(entity, out var physicalEntity))
            return BadRequest("La entidad seleccionada no está disponible para auditoría.");

        var timeZone = TimeZoneInfo.Local;
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(effectiveFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(effectiveTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        try
        {
            var dashboard = await query.SearchAsync(userId,
                new AuditFilter(search, actionType, physicalEntity, actorUserId, empresaSubsidiariaId, fromUtc, toUtc, page), cancellationToken);
            var options = await query.GetFilterOptionsAsync(userId, cancellationToken);
            return View(new AuditQueryViewModel
            {
                Search = search,
                Action = actionType,
                Entity = entity,
                ActorUserId = actorUserId,
                EmpresaSubsidiariaId = empresaSubsidiariaId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = dashboard.Page,
                PageSize = dashboard.PageSize,
                TotalCount = dashboard.TotalCount,
                DashboardEvents = dashboard.Items.Select(item => item with { Entity = AuditEntityRegistry.DisplayName(item.Entity) }).ToArray(),
                EntityOptions = options.Entities,
                UserOptions = options.Users,
                SubsidiaryOptions = options.Subsidiaries,
                DateFrom = effectiveFrom,
                DateTo = effectiveTo
            });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (SqlException exception)
        {
            logger.LogError(exception, "Error SQL consultando auditoría. TraceIdentifier={TraceIdentifier}", HttpContext.TraceIdentifier);
            return View(new AuditQueryViewModel
            {
                Search = search,
                Action = actionType,
                Entity = entity,
                ActorUserId = actorUserId,
                EmpresaSubsidiariaId = empresaSubsidiariaId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                DateFrom = effectiveFrom,
                DateTo = effectiveTo,
                Page = Math.Max(1, page),
                PageSize = 25,
                QueryError = "No fue posible consultar la auditoría en este momento. Intente nuevamente."
            });
        }
    }

    [HttpGet("Operations/{operationId:long}")]
    public async Task<IActionResult> Operation(long operationId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await query.GetAsync(userId, operationId, cancellationToken);
        return item is null ? NotFound() : View(item);
    }

    [HttpGet("Operations/{operationId:guid}")]
    public async Task<IActionResult> Operation(Guid operationId, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await query.GetOperationAsync(userId, operationId, cancellationToken);
        if (item is null)
            return NotFound();

        var preview = await restoreService.PreviewAsync(userId, operationId, cancellationToken);
        return View("Operation", item with { CanRestore = preview?.CanUndo == true });
    }

    [HttpGet("Operations/{operationId:guid}/restore-preview")]
    [Authorize(Policy = Permissions.AuditoriaRestaurar)]
    public async Task<IActionResult> RestorePreview(Guid operationId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();
        var preview = await restoreService.PreviewAsync(actor.Value, operationId, cancellationToken);
        return preview is null ? NotFound() : Json(preview);
    }

    [HttpPost("Operations/{operationId:guid}/restore")]
    [Authorize(Policy = Permissions.AuditoriaRestaurar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(Guid operationId, CancellationToken cancellationToken)
    {
        var actor = ActorId();
        if (actor is null) return Forbid();
        var result = await restoreService.RestoreAsync(actor.Value, operationId, HttpContext.TraceIdentifier, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Operation), new { operationId });
    }

    private Guid? ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}