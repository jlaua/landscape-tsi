namespace Landscape.Tsi.Tests.Catalogs;

public sealed class FamilyBuildingBlockReportTests
{
    private static readonly (int? FamilyId, int BuildingBlockId, int TechnologyId)[] Links =
    [
        (10, 5, 100),
        (10, 5, 101),
        (10, 5, 102),
        (20, 5, 103),
        (10, 6, 104),
        (null, 7, 105)
    ];

    [Fact]
    public void RepeatedTechnologiesInSameFamily_CountBuildingBlockOnce()
    {
        var count = Links.Where(link => link.FamilyId == 10).Select(link => link.BuildingBlockId).Distinct().Count();

        Assert.Equal(2, count);
    }

    [Fact]
    public void BuildingBlockInTwoFamilies_AppearsOncePerFamily()
    {
        var result = Links.Where(link => link.FamilyId is not null)
            .Select(link => (link.FamilyId!.Value, link.BuildingBlockId))
            .Distinct()
            .GroupBy(pair => pair.Item1)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.Equal(2, result[10]);
        Assert.Equal(1, result[20]);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void TechnologyWithoutFamily_DoesNotCreateFamilyPair()
    {
        var familyPairs = Links.Where(link => link.FamilyId is not null)
            .Select(link => (link.FamilyId!.Value, link.BuildingBlockId));

        Assert.DoesNotContain((0, 7), familyPairs);
    }

    [Fact]
    public void BuildingBlockWithoutTechnology_IsCountedSeparately()
    {
        var allBuildingBlocks = new[] { 5, 6, 7, 8 };
        var mapped = Links.Select(link => link.BuildingBlockId).Distinct();

        Assert.Equal(8, Assert.Single(allBuildingBlocks.Except(mapped)));
    }

    [Fact]
    public void TechnologyWithoutBuildingBlock_IsCountedSeparately()
    {
        var allTechnologies = new[] { 100, 101, 102, 103, 104, 105, 106 };
        var mapped = Links.Select(link => link.TechnologyId).Distinct();

        Assert.Equal(106, Assert.Single(allTechnologies.Except(mapped)));
    }

    [Fact]
    public void DrillDown_OnlyReturnsTechnologiesFromSelectedFamily()
    {
        var selectedFamily = Links.Where(link => link.FamilyId == 10 && link.BuildingBlockId == 5)
            .Select(link => link.TechnologyId)
            .Distinct()
            .ToArray();

        Assert.Equal([100, 101, 102], selectedFamily);
        Assert.DoesNotContain(103, selectedFamily);
    }

    [Fact]
    public void BuildingBlockWithMultipleFamilies_IsDetected()
    {
        var multiFamilyCount = Links.Where(link => link.FamilyId is not null)
            .GroupBy(link => link.BuildingBlockId)
            .Count(group => group.Select(link => link.FamilyId).Distinct().Count() > 1);

        Assert.Equal(1, multiFamilyCount);
    }
}
