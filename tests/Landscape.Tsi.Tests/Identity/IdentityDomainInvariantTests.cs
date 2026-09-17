using Landscape.Tsi.Domain.Identity;

namespace Landscape.Tsi.Tests.Identity;

public sealed class IdentityDomainInvariantTests
{
    [Fact]
    public void User_CanAuthenticateOnlyWhileActiveAndEffective()
    {
        var now = DateTime.UtcNow;
        var active = new IamUsuario { IsActive = true, ValidFromUtc = now.AddMinutes(-1), ValidUntilUtc = now.AddMinutes(1) };
        var expired = new IamUsuario { IsActive = true, ValidUntilUtc = now };

        Assert.True(active.CanAuthenticateAt(now));
        Assert.False(expired.CanAuthenticateAt(now));
    }

    [Fact]
    public void OrganizationScope_RequiresExactlyOneScopeKind()
    {
        var scope = new IamUsuarioOrganizacion { IsCorporateScope = true, EmpresaSubsidiariaId = 10 };

        Assert.Throws<InvalidOperationException>(scope.Validate);
    }

    [Fact]
    public void EmergencyAccess_RejectsSelfApprovalAndInvalidPeriod()
    {
        var userId = Guid.NewGuid();
        var access = new IamAccesoEmergencia
        {
            UserId = userId,
            ApprovedByUserId = userId,
            IncidentReference = "INC-001",
            Justification = "Recuperación controlada",
            StartsAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        };

        Assert.Throws<InvalidOperationException>(access.Validate);
    }

    [Fact]
    public void ExternalLogin_RequiresStableIssuerAndSubject()
    {
        var login = new IamUsuarioLoginExterno { Issuer = " ", Subject = "subject" };

        Assert.Throws<ArgumentException>(login.Validate);
    }

    [Fact]
    public void RoleAssignment_IsNotEffectiveAfterExpiration()
    {
        var now = DateTime.UtcNow;
        var assignment = new IamUsuarioRol { ValidFromUtc = now.AddHours(-2), ValidUntilUtc = now.AddHours(-1) };

        Assert.False(assignment.IsEffectiveAt(now));
    }

    [Fact]
    public void AuthorizationAudit_RequiresCorrelation()
    {
        var audit = new IamEventoAuditoriaAutorizacion
        {
            EventType = "RoleAssigned",
            Result = "Succeeded",
            CorrelationId = " "
        };

        Assert.Throws<ArgumentException>(audit.Validate);
    }
}