namespace ExpenseLite.Application.Identity;

/// <summary>
/// 自助註冊。刻意不帶角色與狀態：註冊只能產生「員工 / 尚待啟用」，
/// 讓表單決定這兩者等於開放任何人自封主管並直接登入。
/// </summary>
public sealed record RegisterUserCommand(
    string UserName,
    string Email,
    string DisplayName,
    string Password);
