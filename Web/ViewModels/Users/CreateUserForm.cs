using System.ComponentModel.DataAnnotations;
using ExpenseLite.Application.Identity;

namespace ExpenseLite.Web.ViewModels.Users;

/// <summary>
/// 主管建立新帳號。這是系統裡唯一產生帳號的入口（除了空系統時的 bootstrap）。
///
/// 密碼由主管填、當面交給本人，跟「重設密碼」是同一套做法：
/// 沒有 SMTP 就沒有寄邀請信這個選項，總得有人把第一組密碼交出去。
/// </summary>
public sealed class CreateUserForm
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(50, ErrorMessage = "帳號最多 50 個字")]
    [Display(Name = "帳號")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入姓名")]
    [StringLength(50, ErrorMessage = "姓名最多 50 個字")]
    [Display(Name = "姓名")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [StringLength(256, ErrorMessage = "Email 最多 256 個字")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇角色")]
    [Display(Name = "角色")]
    public string Role { get; set; } = ExpenseLiteRoles.Employee;

    // 長度與複雜度的實際把關在 Identity（見 DependencyInjection 的 Password 設定），
    // 這裡只做「有沒有填、兩次一不一樣」這種表單層級的檢查，避免兩邊各寫一套規則卻對不起來。
    [Required(ErrorMessage = "請輸入預設密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "預設密碼")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "請再輸入一次預設密碼")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "兩次輸入的密碼不一致")]
    [Display(Name = "確認預設密碼")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
