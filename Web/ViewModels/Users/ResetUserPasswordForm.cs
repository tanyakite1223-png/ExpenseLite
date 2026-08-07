using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.Users;

/// <summary>
/// 主管替別人重設密碼。刻意不問舊密碼——忘記密碼的人本來就講不出舊的，
/// 主管也不該知道別人的密碼。把關靠的是「只有主管進得來這一頁」。
/// </summary>
public sealed class ResetUserPasswordForm
{
    public Guid UserId { get; set; }

    /// <summary>只是畫面上讓主管確認自己在改誰，不會被寫進資料。</summary>
    public string DisplayName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入新密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "新密碼")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "請再輸入一次新密碼")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "兩次輸入的新密碼不一致")]
    [Display(Name = "確認新密碼")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
