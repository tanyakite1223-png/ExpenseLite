using System.ComponentModel.DataAnnotations;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class EditExpenseDetailForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "請選擇日期")]
    public DateOnly ExpenseDate { get; set; }

    [Required(ErrorMessage = "請輸入類別")]
    [StringLength(50, ErrorMessage = "類別最多 50 個字")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入說明")]
    [StringLength(200, ErrorMessage = "說明最多 200 個字")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇單據類型")]
    public ExpenseReceiptType ReceiptType { get; set; } = ExpenseReceiptType.Receipt;

    [StringLength(20, ErrorMessage = "發票號碼最多 20 個字")]
    public string? InvoiceNumber { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }
}
