using Landscape.Tsi.Application.Reporting;

namespace Landscape.Tsi.Tests.Reporting;

public sealed class CompanyCisoReportTests
{
    [Fact]
    public void EmptyResult_UsesOneDisplayPage()
    {
        var report = new CompanyCisoReport(
            new CompanyCisoReportQuery(null, null, null, null, null, null, null, false, false, false, 1, 25),
            new(0, 0, 0, 0, 0), [], 0, [], [], [], [], []);

        Assert.Equal(1, report.TotalPages);
    }

    [Fact]
    public void MultipleRepresentatives_AreMarkedAsInconsistent()
    {
        var row = new CompanyCisoReportRow(1, "Empresa", null, null, null, null, null, 2,
            "CISO", null, null, true, true, true, true, null, null);

        Assert.True(row.HasMultipleRepresentatives);
        Assert.True(row.HasRepresentative);
    }

    [Fact]
    public void CompanyWithoutCiso_IsRepresentedWithoutTechnicalIdentifiers()
    {
        var row = new CompanyCisoReportRow(1, "Empresa", null, null, null, null, null, null,
            null, null, null, null, false, false, false, null, null);

        Assert.False(row.HasCiso);
        Assert.Null(row.CisoId);
        Assert.Null(row.CisoRoute);
    }

    [Fact]
    public void QueryCarriesOrganizationalScopeUser()
    {
        var userId = Guid.NewGuid();
        var query = new CompanyCisoReportQuery(null, null, null, null, null, null, null, false, false, false, 1, 25, userId);

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void LegacyComparisonCanExposeMismatchWithoutChangingSourceData()
    {
        var row = new CompanyCisoReportRow(1, "Empresa", null, null, null, null, null, 2,
            "CISO actual", null, null, true, true, true, false, null, null,
            "CISO legado", "DIFIERE_O_FALTA");

        Assert.Equal("DIFIERE_O_FALTA", row.LegacyComparison);
        Assert.Equal("CISO legado", row.LegacyContact);
    }
}