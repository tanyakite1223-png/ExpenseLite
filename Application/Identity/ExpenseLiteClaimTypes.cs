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
}
