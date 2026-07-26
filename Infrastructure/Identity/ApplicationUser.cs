using Microsoft.AspNetCore.Identity;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// 登入用的使用者身分，屬於 Infrastructure（依賴 ASP.NET Core Identity）。
/// Domain 不會參照這個型別，只透過 Guid 形式的 UserId 參照使用者。
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>顯示用姓名，例如「王小明」。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>停用的帳號保留歷史資料，但不能再登入。</summary>
    public bool IsActive { get; set; } = true;
}
