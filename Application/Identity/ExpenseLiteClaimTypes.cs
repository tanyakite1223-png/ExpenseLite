namespace ExpenseLite.Application.Identity;

public static class ExpenseLiteClaimTypes
{
    /// <summary>顯示用姓名。登入時寫進 cookie，之後每頁要顯示「誰登入了」就不用再查資料庫。</summary>
    public const string DisplayName = "display_name";
}
