using Landscape.Tsi.Infrastructure.Catalogs;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class AssignmentConcurrencyHelperTests
{
    [Fact]
    public void CapabilityToken_IsDeterministicAndDetectsStateChanges()
    {
        var token1 = AssignmentConcurrencyHelper.CreateCapabilityToken(10, 5, "Gestión de Identidades", 1);
        var token2 = AssignmentConcurrencyHelper.CreateCapabilityToken(10, 5, "Gestión de Identidades", 1);
        var changedParent = AssignmentConcurrencyHelper.CreateCapabilityToken(10, 6, "Gestión de Identidades", 1);
        var changedName = AssignmentConcurrencyHelper.CreateCapabilityToken(10, 5, "Gestión de Accesos", 1);
        var changedState = AssignmentConcurrencyHelper.CreateCapabilityToken(10, 5, "Gestión de Identidades", 2);

        Assert.Equal(token1, token2);
        Assert.NotEqual(token1, changedParent);
        Assert.NotEqual(token1, changedName);
        Assert.NotEqual(token1, changedState);

        Assert.True(AssignmentConcurrencyHelper.VerifyCapabilityToken(token1, 10, 5, "Gestión de Identidades", 1));
        Assert.False(AssignmentConcurrencyHelper.VerifyCapabilityToken(token1, 10, 6, "Gestión de Identidades", 1));
        Assert.False(AssignmentConcurrencyHelper.VerifyCapabilityToken(string.Empty, 10, 5, "Gestión de Identidades", 1));
        Assert.False(AssignmentConcurrencyHelper.VerifyCapabilityToken("invalid-hash", 10, 5, "Gestión de Identidades", 1));
    }

    [Fact]
    public void FunctionalityToken_IsDeterministicAndDetectsStateChanges()
    {
        var token1 = AssignmentConcurrencyHelper.CreateFunctionalityToken(100, 20, "Autenticación Multifactor", 1);
        var token2 = AssignmentConcurrencyHelper.CreateFunctionalityToken(100, 20, "Autenticación Multifactor", 1);
        var changedParent = AssignmentConcurrencyHelper.CreateFunctionalityToken(100, 25, "Autenticación Multifactor", 1);
        var changedName = AssignmentConcurrencyHelper.CreateFunctionalityToken(100, 20, "SSO Federado", 1);

        Assert.Equal(token1, token2);
        Assert.NotEqual(token1, changedParent);
        Assert.NotEqual(token1, changedName);

        Assert.True(AssignmentConcurrencyHelper.VerifyFunctionalityToken(token1, 100, 20, "Autenticación Multifactor", 1));
        Assert.False(AssignmentConcurrencyHelper.VerifyFunctionalityToken(token1, 100, 25, "Autenticación Multifactor", 1));
        Assert.False(AssignmentConcurrencyHelper.VerifyFunctionalityToken(null!, 100, 20, "Autenticación Multifactor", 1));
    }
}