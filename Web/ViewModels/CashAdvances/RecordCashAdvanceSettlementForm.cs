using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class RecordCashAdvanceSettlementForm
{
    public Guid CashAdvanceId { get; set; }

    [Required(ErrorMessage = "請選擇結清日期")]
    public DateOnly SettledAt { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "結清金額必須大於 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "請輸入處理人")]
    [StringLength(50, ErrorMessage = "處理人最多 50 個字")]
    public string HandledBy { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "備註最多 500 個字")]
    public string? Note { get; set; }
}
