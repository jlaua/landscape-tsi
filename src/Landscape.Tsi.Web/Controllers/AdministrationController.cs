using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Web.Models;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Controllers;

[Authorize(Policy = Permissions.UserManage)]
public sealed class AdministrationController(
    IIdentityAdministrationOverview overview,
    IIdentityUserAdministration users,
    ILocalUserAdministration localUsers) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await overview.GetAsync(cancellationToken));

    [HttpGet("Usuarios")]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<IActionResult> Users(LocalUsersQueryModel query, CancellationToken cancellationToken)
    {
        var page = await localUsers.ListAsync(new LocalUserListQuery(query.Search, query.RoleCode, query.IsActive, query.Page, 10), cancellationToken);
        return View(new LocalUsersPageViewModel(page, await localUsers.GetRolesAsync(cancellationToken), query));
    }

    [HttpGet("Usuarios/Nuevo")]
    [Authorize(Policy = Permissions.UsersCreate)]
    public async Task<IActionResult> CreateUser(CancellationToken cancellationToken) => View(new LocalUserCreateViewModel());

    [HttpPost("Usuarios/Nuevo")]
    [Authorize(Policy = Permissions.UsersCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(LocalUserCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var actor = GetActorId(); if (actor is null) return Forbid();
        try
        {
            await localUsers.CreateAsync(new CreateLocalUserCommand(model.UserName, model.Password, model.ValidFromUtc, model.ValidUntilUtc, actor.Value, model.Justification, HttpContext.TraceIdentifier), cancellationToken);
            TempData["StatusMessage"] = "Usuario local creado correctamente.";
            return RedirectToAction(nameof(Users));
        }
        catch (IdentityValidationException) { ModelState.AddModelError(nameof(model.Password), "La contraseña o los datos no cumplen la política de Identity."); return View(model); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); return View(model); }
    }

    [HttpGet("Usuarios/{userId:guid}")]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<IActionResult> UserDetails(Guid userId, CancellationToken cancellationToken)
    {
        var user = await localUsers.FindAsync(userId, cancellationToken); if (user is null) return NotFound();
        return View(new LocalUserDetailViewModel(user, await localUsers.GetRolesAsync(cancellationToken), await localUsers.GetAuditAsync(userId, cancellationToken)));
    }

    [HttpGet("Usuarios/{userId:guid}/Editar")]
    [Authorize(Policy = Permissions.UsersEdit)]
    public async Task<IActionResult> EditUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await localUsers.FindAsync(userId, cancellationToken); if (user is null) return NotFound();
        return View(new LocalUserEditViewModel { UserId = user.Id, UserName = user.UserName, IsActive = user.IsActive, ValidFromUtc = user.ValidFromUtc, ValidUntilUtc = user.ValidUntilUtc });
    }

    [HttpPost("Usuarios/{userId:guid}/Editar")]
    [Authorize(Policy = Permissions.UsersEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(Guid userId, LocalUserEditViewModel model, CancellationToken cancellationToken)
    {
        model.UserId = userId; if (!ModelState.IsValid) return View(model); var actor = GetActorId(); if (actor is null) return Forbid();
        await localUsers.UpdateAsync(new UpdateLocalUserCommand(userId, model.UserName, model.ValidFromUtc, model.ValidUntilUtc, actor.Value, model.Justification, HttpContext.TraceIdentifier), cancellationToken);
        TempData["StatusMessage"] = "Usuario actualizado correctamente."; return RedirectToAction(nameof(UserDetails), new { userId });
    }

    [HttpPost("Usuarios/{userId:guid}/Estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetUserState(Guid userId, bool active, string justification, CancellationToken cancellationToken)
    {
        if (!User.HasClaim(CustomClaimTypes.Permission, active ? Permissions.UsersActivate : Permissions.UsersDeactivate)) return Forbid();
        var actor = GetActorId(); if (actor is null) return Forbid();
        await localUsers.SetActiveAsync(userId, active, actor.Value, justification, HttpContext.TraceIdentifier, cancellationToken);
        TempData["StatusMessage"] = active ? "Usuario activado correctamente." : "Usuario desactivado correctamente.";
        return RedirectToAction(nameof(UserDetails), new { userId });
    }

    [HttpPost("Usuarios/{userId:guid}/Roles")]
    [Authorize(Policy = Permissions.UsersAssignRoles)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUserRole(Guid userId, string roleCode, bool assign, string justification, CancellationToken cancellationToken)
    {
        var actor = GetActorId(); if (actor is null) return Forbid();
        await localUsers.ChangeRoleAsync(new ChangeLocalUserRoleCommand(userId, roleCode, assign, actor.Value, justification, HttpContext.TraceIdentifier), cancellationToken);
        TempData["StatusMessage"] = assign ? "Rol asignado correctamente." : "Rol retirado correctamente.";
        return RedirectToAction(nameof(UserDetails), new { userId });
    }

    [HttpGet("Usuarios/{userId:guid}/RestablecerContrasena")]
    [Authorize(Policy = Permissions.UsersResetPassword)]
    public async Task<IActionResult> ResetPassword(Guid userId, CancellationToken cancellationToken)
    {
        if (!IsSystemAdministrator()) return Forbid();
        var user = await users.FindAsync(userId, cancellationToken);
        return user is null ? NotFound() : View(new ResetPasswordViewModel { UserId = user.Id, UserName = user.UserName });
    }

    [HttpPost("Usuarios/{userId:guid}/RestablecerContrasena")]
    [Authorize(Policy = Permissions.UsersResetPassword)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid userId, ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!IsSystemAdministrator()) return Forbid();
        model.UserId = userId;
        var user = await users.FindAsync(userId, cancellationToken);
        if (user is null) return NotFound();
        model.UserName = user.UserName;
        if (!ModelState.IsValid) return View(model);

        var actorId = GetActorId();
        if (actorId is null) return Forbid();
        var errors = await localUsers.ResetPasswordAsync(new ResetLocalUserPasswordCommand(
            userId, model.NewPassword, actorId.Value, model.Justification, HttpContext.TraceIdentifier), cancellationToken);
        if (errors.Count > 0)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "La contraseña no cumple la política configurada de Identity.");
            return View(model);
        }

        TempData["StatusMessage"] = $"La contraseña de {user.UserName} fue restablecida correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsSystemAdministrator() => User.IsInRole(SystemRoles.AdministratorName);

    private Guid? GetActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
