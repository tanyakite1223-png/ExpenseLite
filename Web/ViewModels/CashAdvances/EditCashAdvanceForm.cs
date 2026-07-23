using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class EditCashAdvanceForm
{
    public Guid CashAdvanceId { get; set; }

    [Required(ErrorMessage = "請輸入預支用途")]
    [StringLength(200, ErrorMessage = "預支用途最多 200 個字")]
    public string Purpose { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }
}
