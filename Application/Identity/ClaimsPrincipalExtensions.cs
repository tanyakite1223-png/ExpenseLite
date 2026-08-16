using System.Security.Claims;

namespace ExpenseLite.Application.Identity;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>取顯示用姓名；沒有 DisplayName claim 時退回帳號名稱。</summary>
    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        var displayName = principal.FindFirstValue(ExpenseLiteClaimTypes.DisplayName);

        return string.IsNullOrWhiteSpace(displayName)
            ? principal.Identity?.Name ?? string.Empty
            : displayName;
    }

    public static bool IsManager(this ClaimsPrincipal principal)
        => principal.IsInRole(ExpenseLiteRoles.Manager);

    /// <summary>
    /// 目前登入的是不是緊急存取帳號。只拿來在畫面上掛警示橫幅，**不要拿來做權限判斷**——
    /// claim 是登入當下的快照，權限要看資料庫。
    /// </summary>
    public static bool IsProtectedAccount(this ClaimsPrincipal principal)
        => principal.HasClaim(ExpenseLiteClaimTypes.ProtectedAccount, "true");

    /// <summary>
    /// 是否被要求先改密碼。給攔截 middleware 用；本人改完密碼後 <c>RefreshSignInAsync</c>
    /// 會重新產 claim（此時值變成 false），middleware 下一次就會放行。
    /// </summary>
    public static bool MustChangePassword(this ClaimsPrincipal principal)
        => principal.HasClaim(ExpenseLiteClaimTypes.RequirePasswordChange, "true");

    /// <summary>
    /// 把登入 cookie 裡的身分轉成 Application 看得懂的 <see cref="CurrentUser"/>。
    /// 全站預設需要登入，所以走到這裡還沒有 UserId 代表程式接錯了，直接丟例外而不是靜靜吞掉。
    /// </summary>
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var userId = principal.GetUserId()
            ?? throw new InvalidOperationException("目前沒有登入者，無法取得操作人資訊。");

        return new CurrentUser(userId, principal.GetDisplayName(), principal.IsManager());
    }
}
