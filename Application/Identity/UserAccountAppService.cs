using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Application.Identity;

/// <summary>
/// 帳號管理的 use case 編排：註冊、啟用 / 停用、換角色、改密碼。
///
/// 為什麼「至少要有一位啟用中的主管」這條規則落在 Application Service 而不是 Domain：
/// 它跨多個使用者（要看整份名單才知道自己是不是最後一個），照 §4.2 的判斷順序本來會是 Domain Service，
/// 但本專案的使用者刻意不是 Domain 的一員——<c>ApplicationUser</c> 繼承 Identity 的型別、住在 Infrastructure，
/// Domain 只用 Guid 參照人。要把這條規則放進 Domain，就得先把使用者整個搬進 Domain，
/// 那個代價遠大於一條規則。所以它落在需要 <see cref="IUserAccountStore"/> 才判斷得出來的這一層。
/// </summary>
public sealed class UserAccountAppService
{
    private readonly IUserAccountStore _accounts;

    public UserAccountAppService(IUserAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<IReadOnlyList<UserAccountDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accounts.ListAllAsync(cancellationToken);

        // 尚待啟用的排最前面：那是主管進這一頁唯一「有事要做」的一群人。
        return accounts
            .OrderBy(StatusOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }

    public Task<UserAccountResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
        => _accounts.RegisterAsync(command, cancellationToken);

    public async Task<UserAccountResult> ActivateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await GetRequiredAccountAsync(userId, cancellationToken);

        return await _accounts.SetStatusAsync(userId, UserAccountStatus.Active, cancellationToken);
    }

    public async Task<UserAccountResult> DisableAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccountAsync(userId, cancellationToken);

        if (account.IsProtected)
        {
            throw new DomainRuleViolationException(
                "這是緊急存取帳號，不能停用。它的用途就是在沒有其他主管進得來時保留一條路；" +
                "若要換掉它的密碼，請用「重設密碼」。");
        }

        await EnsureNotLastActiveManagerAsync(
            userId,
            "系統至少要保留一位啟用中的主管，不能停用最後一位主管。請先指派另一位主管。",
            cancellationToken);

        return await _accounts.SetStatusAsync(userId, UserAccountStatus.Disabled, cancellationToken);
    }

    public async Task<UserAccountResult> SetRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!ExpenseLiteRoles.All.Contains(role))
        {
            throw new DomainRuleViolationException("指定的角色不存在。");
        }

        var account = await GetRequiredAccountAsync(userId, cancellationToken);

        // 只有「降成員工」會少一位主管；升成主管不會。
        if (role != ExpenseLiteRoles.Manager)
        {
            if (account.IsProtected)
            {
                throw new DomainRuleViolationException(
                    "這是緊急存取帳號，不能降成員工。它必須一直是主管，才能在其他人都進不來時救得了系統。");
            }

            await EnsureNotLastActiveManagerAsync(
                userId,
                "系統至少要保留一位啟用中的主管，不能把最後一位主管改成員工。請先指派另一位主管。",
                cancellationToken);
        }

        return await _accounts.SetRoleAsync(userId, role, cancellationToken);
    }

    public async Task<UserAccountResult> ChangeOwnPasswordAsync(
        CurrentUser actor,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        await GetRequiredAccountAsync(actor.UserId, cancellationToken);

        return await _accounts.ChangePasswordAsync(
            actor.UserId,
            currentPassword,
            newPassword,
            cancellationToken);
    }

    public async Task<UserAccountResult> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        await GetRequiredAccountAsync(userId, cancellationToken);

        return await _accounts.ResetPasswordAsync(userId, newPassword, cancellationToken);
    }

    public Task<UserAccountDto?> FindAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _accounts.FindAccountByIdAsync(userId, cancellationToken);

    public Task RecordSignInAsync(
        Guid userId,
        DateTime signedInAt,
        CancellationToken cancellationToken = default)
        => _accounts.RecordSignInAsync(userId, signedInAt, cancellationToken);

    private async Task<UserAccountDto> GetRequiredAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _accounts.FindAccountByIdAsync(userId, cancellationToken)
           ?? throw new DomainRuleViolationException("找不到指定的帳號。");

    /// <summary>
    /// 擋掉「把系統裡最後一位啟用中的主管拿掉」。
    /// 沒有這道防線的話，全公司會登得進來但沒有人能核准、也沒有人能啟用別人的帳號，而且沒有後門可以救。
    /// </summary>
    private async Task EnsureNotLastActiveManagerAsync(
        Guid userId,
        string message,
        CancellationToken cancellationToken)
    {
        var accounts = await _accounts.ListAllAsync(cancellationToken);

        var activeManagers = accounts
            .Where(x => x.Status == UserAccountStatus.Active && x.Role == ExpenseLiteRoles.Manager)
            .ToList();

        if (activeManagers.Count == 1 && activeManagers[0].UserId == userId)
        {
            throw new DomainRuleViolationException(message);
        }
    }

    private static int StatusOrder(UserAccountDto account) => account.Status switch
    {
        UserAccountStatus.Pending => 0,
        UserAccountStatus.Active => 1,
        UserAccountStatus.Disabled => 2,
        _ => 3
    };
}
