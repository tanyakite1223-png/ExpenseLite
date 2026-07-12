using ExpenseLite.Application.ExpenseReports;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class ExpenseReportDetailsPage
{
    public required ExpenseReportDetailDto Report { get; init; }

    public AddExpenseDetailForm NewDetail { get; init; } = new();
}
