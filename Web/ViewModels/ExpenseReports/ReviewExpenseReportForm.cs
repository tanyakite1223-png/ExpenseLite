using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class ReviewExpenseReportForm
{
    [Required(ErrorMessage = "審核人不可空白。")]
    [StringLength(50, ErrorMessage = "審核人最多 50 個字。")]
    public string ReviewerName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "原因最多 500 個字。")]
    public string? Reason { get; set; }
}
