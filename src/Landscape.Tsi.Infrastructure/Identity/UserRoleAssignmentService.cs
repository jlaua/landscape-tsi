using System.Text.Json;

using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Identity;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Infrastructure.Identity;

internal sealed class UserRoleAssignmentService(IdentityDbContext dbContext) : IUserRoleAssignmentService
{
    public async Task AssignAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        ValidateText(command.Justification, command.CorrelationId);
        if (command.UserId == command.ApprovedByUserId)
        {
            throw new InvalidOperationException("El beneficiario no puede aprobar su propia elevación.");
        }

        if (command.ValidUntilUtc is not null && command.ValidUntilUtc <= command.ValidFromUtc)
        {
            throw new InvalidOperationException("La vigencia de la asignación no es válida.");
        }

        if (!await dbContext.Users.AnyAsync(user => user.Id == command.UserId, cancellationToken) ||
            !await dbContext.BusinessRoles.AnyAsync(role => role.Id == command.RoleId, cancellationToken))
        {
            throw new InvalidOperationException("El usuario o rol no existe.");
        }

        var assignment = await dbContext.UserRoles.SingleOrDefaultAsync(
            item => item.UserId == command.UserId && item.RoleId == command.RoleId, cancellationToken);
        if (assignment is null)
        {
            assignment = new IamUsuarioRol { UserId = command.UserId, RoleId = command.RoleId };
            dbContext.UserRoles.Add(assignment);
        }

        var before = assignment.ValidUntilUtc;
        assignment.RequestedByUserId = command.RequestedByUserId;
        assignment.ApprovedByUserId = command.ApprovedByUserId;
        assignment.ExecutedByUserId = command.ExecutedByUserId;
        assignment.Justification = command.Justification;
        assignment.ValidFromUtc = command.ValidFromUtc;
        assignment.ValidUntilUtc = command.ValidUntilUtc;
        AddAudit("RoleAssigned", command.UserId, command.RoleId, command.ExecutedByUserId,
            command.ApprovedByUserId, command.Justification, command.CorrelationId,
            JsonSerializer.Serialize(new { ValidUntilUtc = before }),
            JsonSerializer.Serialize(new { assignment.ValidFromUtc, assignment.ValidUntilUtc }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        Guid userId,
        Guid roleId,
        Guid executedByUserId,
        string justification,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ValidateText(justification, correlationId);
        var assignment = await dbContext.UserRoles.SingleOrDefaultAsync(
            item => item.UserId == userId && item.RoleId == roleId, cancellationToken)
            ?? throw new KeyNotFoundException("La asignación no existe.");
        var before = assignment.ValidUntilUtc;
        assignment.ValidUntilUtc = DateTime.UtcNow;
        assignment.ExecutedByUserId = executedByUserId;
        assignment.Justification = justification;
        AddAudit("RoleRevoked", userId, roleId, executedByUserId, assignment.ApprovedByUserId,
            justification, correlationId,
            JsonSerializer.Serialize(new { ValidUntilUtc = before }),
            JsonSerializer.Serialize(new { assignment.ValidUntilUtc }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAudit(
        string eventType,
        Guid userId,
        Guid roleId,
        Guid actorId,
        Guid? approverId,
        string justification,
        string correlationId,
        string before,
        string after) => dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
    {
        ActorUserId = actorId,
        BeneficiaryUserId = userId,
        EventType = eventType,
        Result = "Succeeded",
        ResourceType = nameof(IamRol),
        ResourceId = roleId.ToString(),
        BeforeJson = before,
        AfterJson = after,
        Justification = justification,
        ApprovedByUserId = approverId,
        CorrelationId = correlationId
    });

    private static void ValidateText(string justification, string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
    }
}
