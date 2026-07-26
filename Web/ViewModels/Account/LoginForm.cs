using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.Account;

public sealed class LoginForm
{
    [Required(ErrorMessage = "請輸入帳號")]
    [StringLength(50, ErrorMessage = "帳號最多 50 個字")]
    [Display(Name = "帳號")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "在這台電腦上保持登入")]
    public bool RememberMe { get; set; }
}
