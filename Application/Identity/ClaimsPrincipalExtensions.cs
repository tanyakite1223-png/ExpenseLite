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
}
