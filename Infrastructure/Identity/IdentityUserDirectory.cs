using ExpenseLite.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// <see cref="IUserDirectory"/> 的實作，資料來源是 ASP.NET Core Identity 的使用者表。
/// 這層才認識 <see cref="ApplicationUser"/>；對 Application 只回傳 <see cref="UserOptionDto"/>。
/// </summary>
public sealed class IdentityUserDirectory : IUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserDirectory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserOptionDto>> ListSelectableAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .Where(x => x.Status == UserAccountStatus.Active)
            .OrderBy(x => x.DisplayName)
            .Select(x => new UserOptionDto(x.Id, x.DisplayName))
            .ToListAsync(cancellationToken);

        return users;
    }

    /// <summary>
    /// 刻意不過濾狀態：這是用來查姓名快照與顯示既有資料的，
    /// 帳號被停用之後，歷史紀錄上的人還是得查得到。
    /// </summary>
    public async Task<UserOptionDto?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _userManager.Users
            .Where(x => x.Id == userId)
            .Select(x => new UserOptionDto(x.Id, x.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
}
