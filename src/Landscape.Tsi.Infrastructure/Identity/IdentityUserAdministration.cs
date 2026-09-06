using System.Text.Json;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class IdentityUserAdministration(
    IdentityDbContext dbContext,
    UserManager<IamUsuario> userManager) : IIdentityUserAdministration
{
    public async Task<Guid> CreateAsync(CreateIdentityUserCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Justification);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);
        if (command.ValidUntilUtc is not null && command.ValidFromUtc is not null &&
            command.ValidUntilUtc <= command.ValidFromUtc)
        {
            throw new InvalidOperationException("La vigencia del usuario no es válida.");
        }

        var normalized = command.UserName.Trim().ToUpperInvariant();
        if (await dbContext.Users.AnyAsync(user => user.NormalizedUserName == normalized, cancellationToken))
        {
            throw new InvalidOperationException("El nombre de usuario ya existe.");
        }

        var user = new IamUsuario
        {
            UserName = command.UserName.Trim(),
            NormalizedUserName = normalized,
            IsActive = true,
            ValidFromUtc = command.ValidFromUtc,
            ValidUntilUtc = command.ValidUntilUtc,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        dbContext.Users.Add(user);
        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = command.ActorUserId,
            BeneficiaryUserId = user.Id,
            EventType = "UserCreated",
            Result = "Succeeded",
            ResourceType = nameof(IamUsuario),
            ResourceId = user.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { user.UserName, user.IsActive, user.ValidFromUtc, user.ValidUntilUtc }),
            Justification = command.Justification,
            CorrelationId = command.CorrelationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public Task<IdentityUserSummary?> FindAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Users.Where(user => user.Id == userId)
            .Select(user => new IdentityUserSummary(
                user.Id, user.UserName!, user.IsActive, user.ValidFromUtc, user.ValidUntilUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task SetActiveAsync(
        Guid userId,
        bool isActive,
        Guid actorUserId,
        string justification,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario no existe.");
        var before = user.IsActive;
        if (before == isActive)
        {
            return;
        }

        user.IsActive = isActive;
        user.SecurityStamp = Guid.NewGuid().ToString();
        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = actorUserId,
            BeneficiaryUserId = user.Id,
            EventType = isActive ? "UserReactivated" : "UserSuspended",
            Result = "Succeeded",
            ResourceType = nameof(IamUsuario),
            ResourceId = user.Id.ToString(),
            BeforeJson = JsonSerializer.Serialize(new { IsActive = before }),
            AfterJson = JsonSerializer.Serialize(new { user.IsActive }),
            Justification = justification,
            CorrelationId = correlationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ResetLocalPasswordAsync(
        ResetLocalPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.NewPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Justification);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CorrelationId);

        var user = await userManager.FindByNameAsync(command.UserName.Trim())
            ?? throw new KeyNotFoundException("El usuario local no existe.");
        if (!user.IsActive)
        {
            throw new InvalidOperationException("No se puede restablecer la contraseña de un usuario inactivo.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, command.NewPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Select(error => error.Code).Distinct(StringComparer.Ordinal).ToArray();
        }

        dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
        {
            ActorUserId = command.ActorUserId,
            BeneficiaryUserId = user.Id,
            EventType = "LocalPasswordReset",
            PermissionCode = Permissions.UserManage,
            Result = "Succeeded",
            ResourceType = nameof(IamUsuario),
            ResourceId = user.Id.ToString(),
            AfterJson = JsonSerializer.Serialize(new { CredentialUpdated = true }),
            Justification = command.Justification,
            CorrelationId = command.CorrelationId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Array.Empty<string>();
    }
}
