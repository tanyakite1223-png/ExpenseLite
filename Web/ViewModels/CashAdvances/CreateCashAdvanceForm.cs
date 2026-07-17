using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class CreateCashAdvanceForm
{
    [Required(ErrorMessage = "請輸入領款人")]
    [StringLength(50, ErrorMessage = "領款人最多 50 個字")]
    public string PayeeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入預支用途")]
    [StringLength(200, ErrorMessage = "預支用途最多 200 個字")]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇預支日期")]
    public DateOnly AdvancedAt { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }
}
