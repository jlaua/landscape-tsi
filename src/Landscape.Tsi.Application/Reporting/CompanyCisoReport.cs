namespace Landscape.Tsi.Application.Reporting;

public sealed record CompanyCisoReportQuery(
    string? Search,
    int? CompanyId,
    string? Country,
    string? Grouping,
    string? Industry,
    string? Ciso,
    bool? RepresentativeOnly,
    bool OnlyWithoutCiso,
    bool OnlyWithoutRepresentative,
    bool AllCiso,
    int Page = 1,
    int PageSize = 25,
    Guid? UserId = null);

public sealed record CompanyCisoReportKpis(
    int TotalCompanies,
    int CompaniesWithCiso,
    int CompaniesWithoutCiso,
    int CompaniesWithRepresentative,
    int CompaniesWithoutRepresentative);

public sealed record CompanyCisoReportRow(
    int CompanyId,
    string CompanyName,
    string? Alias,
    string? Grouping,
    string? Country,
    string? City,
    string? Industry,
    int? CisoId,
    string? CisoName,
    string? CisoEmail,
    string? CisoBusinessLine,
    bool? IsRepresentative,
    bool HasCiso,
    bool HasRepresentative,
    bool HasMultipleRepresentatives,
    string? CompanyRoute,
    string? CisoRoute,
    string? LegacyContact = null,
    string? LegacyComparison = null);

public sealed record CompanyCisoReport(
    CompanyCisoReportQuery Query,
    CompanyCisoReportKpis Kpis,
    IReadOnlyList<CompanyCisoReportRow> Rows,
    int TotalRows,
    IReadOnlyList<(int Id, string Name)> Companies,
    IReadOnlyList<string> Countries,
    IReadOnlyList<string> Groupings,
    IReadOnlyList<string> Industries,
    IReadOnlyList<string> CisoNames)
{
    public int TotalPages => TotalRows == 0 ? 1 : (int)Math.Ceiling(TotalRows / (double)Query.PageSize);
};
