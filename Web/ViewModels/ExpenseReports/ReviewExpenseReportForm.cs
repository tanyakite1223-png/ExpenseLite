using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class ReviewExpenseReportForm
{
    [StringLength(500, ErrorMessage = "原因最多 500 個字。")]
    public string? Reason { get; set; }
}
