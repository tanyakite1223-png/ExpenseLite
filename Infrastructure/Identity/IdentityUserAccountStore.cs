using ExpenseLite.Application.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.Identity;

/// <summary>
/// <see cref="IUserAccountStore"/> 的實作，底層是 ASP.NET Core Identity 的 <see cref="UserManager{TUser}"/>。
/// 只有這一層認識 <see cref="ApplicationUser"/> 與 <c>IdentityResult</c>；
/// 對 Application 一律回 <see cref="UserAccountDto"/> 與 <see cref="UserAccountResult"/>。
/// </summary>
public sealed class IdentityUserAccountStore : IUserAccountStore
{
    private const string AccountNotFoundMessage = "找不到指定的帳號。";

    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserAccountStore(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserAccountDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var accounts = new List<UserAccountDto>(users.Count);
        foreach (var user in users)
        {
            // 每個人多查一次角色（N+1）。十人內的公司這點成本無所謂，
            // 換來的是「這個人實際上掛了什麼角色」這個最直白的答案；
            // 人數真的變多再改成 join user_roles 一次撈完。
            accounts.Add(await MapAsync(user));
        }

        return accounts;
    }

    public async Task<UserAccountDto?> FindAccountByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);

        return user is null ? null : await MapAsync(user);
    }

    public async Task<UserAccountResult> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = command.UserName,
            Email = command.Email,
            // 本專案沒有寄信驗證的流程（也沒有 SMTP），所以直接視為已確認；
            // Identity 的 SignIn.RequireConfirmedAccount 本來就設成 false。
            EmailConfirmed = true,
            DisplayName = command.DisplayName,
            // 主管建好帳號就會當場把帳號與密碼交給本人，沒有「等待啟用」這一段。
            // 明寫出來而不是靠 enum 的預設值，免得日後有人調整 UserAccountStatus 的順序就默默改了行為。
            Status = UserAccountStatus.Active
        };

        var createResult = await _userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            return Failed(createResult);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, command.Role);

        return roleResult.Succeeded ? UserAccountResult.Success : Failed(roleResult);
    }

    public async Task<UserAccountResult> SetStatusAsync(
        Guid userId,
        UserAccountStatus status,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.Status = status;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Failed(updateResult);
        }

        return await InvalidateExistingSignInsAsync(user);
    }

    public async Task<UserAccountResult> SetRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        // 本系統一人只有一個角色，所以是「換」不是「加」：先清掉現有的再指派。
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return Failed(removeResult);
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            return Failed(addResult);
        }

        return await InvalidateExistingSignInsAsync(user);
    }

    public async Task<UserAccountResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.Succeeded ? UserAccountResult.Success : Failed(result);
    }

    public async Task<UserAccountResult> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        // 用 Identity 的 reset token 而不是直接寫入雜湊：token 由框架簽發與驗證，
        // 順帶處理密碼規則與 security stamp 更新。這裡沒有寄信流程，所以自己簽發、自己馬上用掉。
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        return result.Succeeded ? UserAccountResult.Success : Failed(result);
    }

    public async Task RecordSignInAsync(
        Guid userId,
        DateTime signedInAt,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.LastSignedInAt = signedInAt;

        // 刻意不理會失敗：登入紀錄寫不進去不該讓使用者登不進來。
        await _userManager.UpdateAsync(user);
    }

    private Task<ApplicationUser?> FindUserAsync(Guid userId, CancellationToken cancellationToken)
        => _userManager.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    private async Task<UserAccountDto> MapAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserAccountDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.Status,
            roles.FirstOrDefault() ?? string.Empty,
            user.IsProtected,
            user.LastSignedInAt);
    }

    /// <summary>
    /// 動過狀態或角色之後換掉 security stamp，讓對方目前那張登入 cookie 失效。
    /// 沒有這一步的話，剛被停用的人手上的 cookie 還能用到過期為止（本專案設 8 小時），
    /// 降成員工的人也會繼續帶著主管的角色 claim 到處走。
    ///
    /// 老實記著限制：這不是「立刻」生效。Identity 每隔一段時間才重新驗證一次 cookie
    /// （SecurityStampValidationInterval 預設 30 分鐘），所以最慢會延遲到那時候。
    /// </summary>
    private async Task<UserAccountResult> InvalidateExistingSignInsAsync(ApplicationUser user)
    {
        var stampResult = await _userManager.UpdateSecurityStampAsync(user);

        return stampResult.Succeeded ? UserAccountResult.Success : Failed(stampResult);
    }

    private static UserAccountResult NotFound()
        => UserAccountResult.Failed([AccountNotFoundMessage]);

    private static UserAccountResult Failed(IdentityResult result)
        => UserAccountResult.Failed(result.Errors.Select(x => x.Description).ToList());
}
