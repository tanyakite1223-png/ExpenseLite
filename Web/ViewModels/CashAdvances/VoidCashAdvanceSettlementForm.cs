using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class VoidCashAdvanceSettlementForm
{
    public Guid CashAdvanceId { get; set; }

    public Guid SettlementRecordId { get; set; }

    [Required(ErrorMessage = "請輸入處理人")]
    [StringLength(50, ErrorMessage = "處理人最多 50 個字")]
    public string VoidedBy { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入不採用原因")]
    [StringLength(500, ErrorMessage = "不採用原因最多 500 個字")]
    public string VoidReason { get; set; } = string.Empty;
}
