namespace ExpenseLite.Application.Identity;

public static class ExpenseLiteClaimTypes
{
    /// <summary>顯示用姓名。登入時寫進 cookie，之後每頁要顯示「誰登入了」就不用再查資料庫。</summary>
    public const string DisplayName = "display_name";

    /// <summary>
    /// 這是不是緊急存取帳號（break-glass）。寫進 cookie 是為了讓每一頁都能掛上警示橫幅，
    /// 提醒使用者「你正在用不該日常使用的帳號」。**這不是權限判斷用的** ——
    /// 受保護帳號不能被停用 / 降級那條規則在 Application Service，讀的是資料庫而不是 claim。
    /// </summary>
    public const string ProtectedAccount = "protected_account";

    /// <summary>
    /// 是否必須先改密碼才能繼續使用。寫進 cookie 讓攔截 middleware 不用每個 request 查一次 users 表。
    /// 本人改完密碼後由 <c>RefreshSignInAsync</c> 重新產 claim，middleware 下一次就會放行。
    /// 主管替別人重設密碼時，對方 cookie 內的舊 claim 不會即時更新——但 <c>ResetPasswordAsync</c>
    /// 會換掉 security stamp，Identity 最長 30 分鐘內會強制對方重新登入並拿到 true 的新 claim。
    /// </summary>
    public const string RequirePasswordChange = "require_password_change";
}
