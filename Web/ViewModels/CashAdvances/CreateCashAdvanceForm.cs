using System.ComponentModel.DataAnnotations;
using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.CashAdvances;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class CreateCashAdvanceForm
{
    /// <summary>
    /// 領款人是「當事人」不是「操作者」，所以不自動帶入登入者——主管很可能替員工建立預支款。
    /// 表單只送 UserId，姓名快照由 Application 自己查。
    /// </summary>
    [Required(ErrorMessage = "請選擇領款人")]
    public Guid? PayeeUserId { get; set; }

    /// <summary>
    /// 預設個人預支：預設選限制緊的一方，忘了改的後果比較小。
    /// 反過來預設零用金，忘了改就是「本該專屬某人的錢，全公司都能報」。
    /// </summary>
    [Required(ErrorMessage = "請選擇用途類型")]
    public CashAdvanceUsage Usage { get; set; } = CashAdvanceUsage.Personal;

    [Required(ErrorMessage = "請輸入預支用途")]
    [StringLength(200, ErrorMessage = "預支用途最多 200 個字")]
    public string Purpose { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇預支日期")]
    public DateOnly AdvancedAt { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(typeof(decimal), "0.01", "999999999999.99", ErrorMessage = "金額必須大於 0")]
    public decimal Amount { get; set; }

    public IReadOnlyList<UserOptionDto> PayeeOptions { get; set; } = [];
}
