using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.Account;

public sealed class ChangePasswordForm
{
    [Required(ErrorMessage = "請輸入目前的密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "目前的密碼")]
    public string CurrentPassword { get; set; } = string.Empty;

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
