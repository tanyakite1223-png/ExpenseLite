using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class AddExpenseDetailForm
{
    [Required(ErrorMessage = "請選擇日期")]
    public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "請輸入類別")]
    [StringLength(50, ErrorMessage = "類別最多 50 個字")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入說明")]
    [StringLength(200, ErrorMessage = "說明最多 200 個字")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }
}
