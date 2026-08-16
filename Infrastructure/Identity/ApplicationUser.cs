using ExpenseLite.Application.Identity;
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

    /// <summary>
    /// 帳號狀態。帳號只有主管建得出來，建立當下就要能用，所以預設是
    /// <see cref="UserAccountStatus.Active"/>；停用是之後才會發生的事（離職、停權）。
    /// </summary>
    public UserAccountStatus Status { get; set; } = UserAccountStatus.Active;

    /// <summary>
    /// 緊急存取帳號（break-glass）。這種帳號不能被停用、也不能降成員工，
    /// 為的是保證「唯一的主管忘記密碼」時還有路進得來。
    /// 只有 bootstrap 建立第一個帳號時會設成 true，UI 上沒有任何地方能開關它。
    /// </summary>
    public bool IsProtected { get; set; }

    /// <summary>
    /// 最後一次成功登入的時間。存在的理由是緊急帳號最該被回答的問題就是「它被用過嗎」，
    /// 而這套系統沒有寄信或外部告警的管道，只能把痕跡留在系統裡讓人看得到。
    /// null 代表從來沒登入成功過。
    /// </summary>
    public DateTime? LastSignedInAt { get; set; }

    /// <summary>
    /// 是否強制要求本人先改密碼才能繼續使用。
    /// 主管建帳號、主管替別人重設密碼、bootstrap 建立第一個帳號時會設 true——
    /// 這幾條路的預設密碼都是別人給的，本人第一次登入必須換掉。
    /// 本人自己在「修改密碼」頁改成功時會被清成 false。
    /// </summary>
    public bool RequirePasswordChange { get; set; }
}
