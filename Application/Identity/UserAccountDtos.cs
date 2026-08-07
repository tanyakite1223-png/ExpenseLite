namespace ExpenseLite.Application.Identity;

/// <summary>
/// 使用者管理頁看到的一筆帳號。比 <see cref="UserOptionDto"/> 多帶了帳號、email、狀態與角色——
/// 那些是管理畫面才需要的，一般的下拉選單不該看到。
/// </summary>
/// <param name="Role">本系統一個人只有一個角色；查不到角色時是空字串。</param>
/// <param name="IsProtected">緊急存取帳號（break-glass），不能停用也不能降級。</param>
/// <param name="LastSignedInAt">最後一次成功登入的時間；null 代表從未登入成功過。</param>
public sealed record UserAccountDto(
    Guid UserId,
    string UserName,
    string DisplayName,
    string Email,
    UserAccountStatus Status,
    string Role,
    bool IsProtected,
    DateTime? LastSignedInAt);

/// <summary>
/// 帳號操作的結果。
/// 存在的理由是不讓 Identity 的 <c>IdentityResult</c> 漏進 Application——
/// 密碼太短、帳號重複這類錯誤是「使用者可以自己修正的」，要原封不動帶回畫面上，
/// 而且一次可能有好幾條，用例外表達會很彆扭。
/// 真正「不該發生」的狀況（找不到帳號、規則被違反）仍然丟例外。
/// </summary>
public sealed record UserAccountResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static UserAccountResult Success { get; } = new(true, []);

    public static UserAccountResult Failed(IReadOnlyList<string> errors) => new(false, errors);
}
