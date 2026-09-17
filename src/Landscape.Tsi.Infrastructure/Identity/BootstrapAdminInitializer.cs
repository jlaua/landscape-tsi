using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Landscape.Tsi.Infrastructure.Identity;

public sealed class BootstrapAdminInitializer(
    UserManager<IamUsuario> userManager,
    IdentityDbContext dbContext,
    IOptions<BootstrapAdminOptions> options,
    IHostEnvironment environment,
    IAuthenticationAuditWriter auditWriter,
    ILogger<BootstrapAdminInitializer> logger)
{
    private static readonly string[] UserNames = ["jean", "administrador"];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled || !settings.AllowedEnvironments.Contains(environment.EnvironmentName, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation("El bootstrap administrativo está deshabilitado para este entorno.");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogWarning("Bootstrap administrativo omitido: configure BootstrapAdmin:Password mediante User Secrets o variable de entorno.");
            await auditWriter.WriteAsync("Bootstrap", "Local", "SkippedMissingSecret", null, null, cancellationToken);
            return;
        }

        await EnsureInitialRolesAsync(cancellationToken);
        foreach (var userName in UserNames)
        {
            await EnsureUserAsync(userName, settings.Password, cancellationToken);
        }
    }

    private async Task EnsureInitialRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in SystemRoles.InitialRoles)
        {
            if (!await dbContext.BusinessRoles.AnyAsync(x => x.Code == entry.Key, cancellationToken))
            {
                dbContext.BusinessRoles.Add(new IamRol { Code = entry.Key, Name = entry.Value });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var administratorRole = await dbContext.BusinessRoles
            .SingleAsync(x => x.Code == SystemRoles.AdministratorCode, cancellationToken);
        var securityArchitectRole = await dbContext.BusinessRoles
            .SingleAsync(x => x.Code == SystemRoles.SecurityArchitectCode, cancellationToken);
        var governmentSpocRole = await dbContext.BusinessRoles
            .SingleAsync(x => x.Code == SystemRoles.GovernmentSpocCode, cancellationToken);
        var tsiEngineerRole = await dbContext.BusinessRoles
            .SingleAsync(x => x.Code == SystemRoles.TsiEngineerCode, cancellationToken);

        foreach (var entry in Permissions.AdministratorPermissions)
        {
            var permission = await dbContext.Permissions.SingleOrDefaultAsync(x => x.Code == entry.Key, cancellationToken);
            if (permission is null)
            {
                permission = new IamPermiso { Code = entry.Key, Description = entry.Value };
                dbContext.Permissions.Add(permission);
            }

            if (!await dbContext.RolePermissions.AnyAsync(x => x.RoleId == administratorRole.Id && x.PermissionId == permission.Id, cancellationToken))
            {
                dbContext.RolePermissions.Add(new IamRolPermiso { Role = administratorRole, Permission = permission });
            }

            if (Permissions.SecurityArchitectPermissions.Contains(entry.Key) &&
                !await dbContext.RolePermissions.AnyAsync(
                    x => x.RoleId == securityArchitectRole.Id && x.PermissionId == permission.Id,
                    cancellationToken))
            {
                dbContext.RolePermissions.Add(new IamRolPermiso { Role = securityArchitectRole, Permission = permission });
            }

            if (Permissions.GovernmentSpocPermissions.Contains(entry.Key) &&
                !await dbContext.RolePermissions.AnyAsync(
                    x => x.RoleId == governmentSpocRole.Id && x.PermissionId == permission.Id,
                    cancellationToken))
            {
                dbContext.RolePermissions.Add(new IamRolPermiso { Role = governmentSpocRole, Permission = permission });
            }

            if (Permissions.TsiEngineerPermissions.Contains(entry.Key) &&
                !await dbContext.RolePermissions.AnyAsync(
                    x => x.RoleId == tsiEngineerRole.Id && x.PermissionId == permission.Id,
                    cancellationToken))
            {
                dbContext.RolePermissions.Add(new IamRolPermiso { Role = tsiEngineerRole, Permission = permission });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUserAsync(string userName, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new IamUsuario { UserName = userName, IsActive = true, BootstrapManaged = true };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var codes = string.Join(",", result.Errors.Select(x => x.Code));
                logger.LogError("No se pudo crear el usuario bootstrap {UserName}. Códigos: {Codes}", userName, codes);
                await auditWriter.WriteAsync("Bootstrap", "Local", "Failed", userName, null, cancellationToken);
                return;
            }

            await AssignRoleAsync(user, cancellationToken);
            await auditWriter.WriteAsync("Bootstrap", "Local", "Created", userName, null, cancellationToken);
            return;
        }

        if (!user.BootstrapManaged)
        {
            logger.LogWarning("El usuario {UserName} ya existe y no fue creado por bootstrap; no se modificó.", userName);
            await auditWriter.WriteAsync("Bootstrap", "Local", "SkippedManualUser", userName, null, cancellationToken);
            return;
        }

        await AssignRoleAsync(user, cancellationToken);
        await auditWriter.WriteAsync("Bootstrap", "Local", "AlreadyExists", userName, null, cancellationToken);
    }

    private async Task AssignRoleAsync(IamUsuario user, CancellationToken cancellationToken)
    {
        var roleId = await dbContext.BusinessRoles
            .Where(x => x.Code == SystemRoles.AdministratorCode)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        if (!await dbContext.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == roleId, cancellationToken))
        {
            dbContext.UserRoles.Add(new IamUsuarioRol { UserId = user.Id, RoleId = roleId });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}