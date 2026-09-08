using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class DeletionImpactCalculatorTests
{
    [Fact]
    public void NoDependents_DeletesOnlyRoot()
    {
        var result = DeletionImpactCalculator.Calculate("dominio", "TMDominio", 1, "Sin hijos", [], 50);

        Assert.Equal(0, result.TotalDependentRecords);
        Assert.Equal(1, result.TotalRecordsToDelete);
        Assert.False(result.RequiresTypedConfirmation);
        Assert.True(result.CanDelete);
    }

    [Fact]
    public void NestedDependents_CountsAllLevelsAndRequiresConfirmation()
    {
        var functionality = new DeletionDependencyNode("Funcionalidad", "TFuncionalidad", 5, 3, "capacidad", []);
        var capacity = new DeletionDependencyNode("Capacidad", "TCapacidadDeSeguridad", 2, 2, "building", [functionality]);
        var building = new DeletionDependencyNode("Building Block", "TBuildingBlock", 3, 1, "dominio", [capacity]);
        var result = DeletionImpactCalculator.Calculate("dominio", "TMDominio", 1, "Identity", [building], 5);

        Assert.Equal(10, result.TotalDependentRecords);
        Assert.Equal(11, result.TotalRecordsToDelete);
        Assert.True(result.RequiresTypedConfirmation);
        Assert.Equal(3, result.RecursiveDependents.Count);
    }

    [Fact]
    public void BridgeIsIncludedInImpact()
    {
        var bridge = new DeletionDependencyNode("Relaciones Building Block / Tecnología TSI", "TBuildingBlockVsTTecnologiaTSI", 4, 1, "bridge", []);
        var result = DeletionImpactCalculator.Calculate("dominio", "TMDominio", 1, "Identity", [bridge], 50);

        Assert.Contains(result.DirectDependents, node => node.PhysicalTableName == "TBuildingBlockVsTTecnologiaTSI");
        Assert.Equal(5, result.TotalRecordsToDelete);
    }

    [Fact]
    public void CircularDependency_BlocksWithoutInfiniteRecursion()
    {
        var cycle = new DeletionDependencyNode("Dominio", "TMDominio", 1, 1, "cycle", []);
        var cyclicChild = cycle with { Children = [cycle] };
        var result = DeletionImpactCalculator.Calculate("dominio", "TMDominio", 1, "Identity", [cyclicChild], 50);

        Assert.False(result.CanDelete);
        Assert.Contains("circular", result.BlockingReason, StringComparison.OrdinalIgnoreCase);
    }
}