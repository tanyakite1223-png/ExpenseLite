namespace ExpenseLite.Application.Identity;

/// <summary>
/// 主管建立一個新帳號。
///
/// 這裡帶著 <paramref name="Role"/>，而前身 <c>RegisterUserCommand</c>（自助註冊）刻意寫死員工——
/// 差別在呼叫者是誰。自助註冊的入口對未登入的人開放，讓表單決定角色等於開放任何人自封主管；
/// 現在唯一的入口是 <c>UsersController</c>，整個 controller 已經被 <c>[Authorize(Roles = Manager)]</c> 擋住，
/// 能走到這裡的人本來就有指派角色的權限（列表上的「改為主管」按鈕做的是同一件事）。
/// 所以這不是把關變鬆，是把關的位置換到了門口。
///
/// 沒有狀態參數：建立出來的帳號一律是啟用中的（見 <see cref="UserAccountStatus"/>）。
/// </summary>
public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string DisplayName,
    string Role,
    string Password);
