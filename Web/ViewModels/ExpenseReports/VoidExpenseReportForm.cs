using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

/// <summary>
/// 作廢已核准報銷單的表單。
/// Reason 必填（跟退回 / 拒絕同一類）——作廢是重大動作，理由屬於審核紀錄的一部分，
/// 半年後回頭看得知道當時為什麼撤下這張已核准的單。domain 端也擋，這裡是為了 form 驗證即時提示。
/// </summary>
public sealed class VoidExpenseReportForm
{
    [Required(ErrorMessage = "作廢原因必填。")]
    [StringLength(500, ErrorMessage = "原因最多 500 個字。")]
    public string Reason { get; set; } = string.Empty;
}
