using Landscape.Tsi.Application.Reporting;

namespace Landscape.Tsi.Web.Models;

public sealed record CompanyCisoReportViewModel(CompanyCisoReport Report)
{
    public int TotalPages => Report.TotalPages;
}
