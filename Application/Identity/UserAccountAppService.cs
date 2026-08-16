using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Application.Identity;

/// <summary>
/// 帳號管理的 use case 編排：建立、啟用 / 停用、換角色、改密碼。
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

        // 已停用的沉到最後：他們多半已經離職，平常要找的是還在職的人。
        return accounts
            .OrderBy(StatusOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }

    public Task<UserAccountResult> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        EnsureRoleExists(command.Role);

        return _accounts.CreateAsync(command, cancellationToken);
    }

    public async Task<UserAccountResult> ActivateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await GetRequiredAccountAsync(userId, cancellationToken);

        return await _accounts.SetStatusAsync(userId, UserAccountStatus.Active, cancellationToken);
    }

    public async Task<UserAccountResult> DisableAsync(
        Guid userId,
        CurrentUser actor,
        CancellationToken cancellationToken = default)
    {
        var account = await GetRequiredAccountAsync(userId, cancellationToken);

        if (account.IsProtected)
        {
            throw new DomainRuleViolationException(
                "這是緊急存取帳號，不能停用。它的用途就是在沒有其他主管進得來時保留一條路；" +
                "若要換掉它的密碼，請用「重設密碼」。");
        }

        // 停用自己沒有正當使用情境（離職通常由另一位主管處理），誤按代價高——
        // 停完自己 cookie 30 分鐘內失效，得請另一位主管救。跟業界 IAM 慣例一致（Google Cloud、Azure）。
        if (userId == actor.UserId)
        {
            throw new DomainRuleViolationException(
                "不能停用自己的帳號。如需離職或暫停使用，請由另一位主管操作。");
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
        CurrentUser actor,
        CancellationToken cancellationToken = default)
    {
        EnsureRoleExists(role);

        var account = await GetRequiredAccountAsync(userId, cancellationToken);

        // 只有「降成員工」會少一位主管；升成主管不會。
        if (role != ExpenseLiteRoles.Manager)
        {
            if (account.IsProtected)
            {
                throw new DomainRuleViolationException(
                    "這是緊急存取帳號，不能降成員工。它必須一直是主管，才能在其他人都進不來時救得了系統。");
            }

            // 跟停用自己同一類：主管把自己降成員工會馬上失去管理權限，實務上得請另一位主管救。
            // 這條的正當情境比較少（接班要交出主管職的場景），但誤按代價高，先由另一位主管處理更穩。
            if (userId == actor.UserId)
            {
                throw new DomainRuleViolationException(
                    "不能把自己降成員工。若要交出主管職，請由另一位主管操作。");
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

    /// <summary>
    /// 算「日常主管」的人數——啟用中的主管，扣掉緊急存取帳號。
    /// 用途是使用者管理頁的提示：只剩一位日常主管時，他自己送出的報銷單就沒人能審或作廢
    /// （2026-08-08 加了「審核人 ≠ 申請人」之後才變得急迫）。
    ///
    /// 為什麼扣掉緊急存取帳號：那個帳號的整個設計理念就是「日常不用」，
    /// 拿它當日常審核人違反該帳號定位；如果算它，只要 Admin 存在就永遠不會觸發提示，
    /// 這個提示等於沒用。定義集中在這裡，畫面只用來決定要不要顯示，不重寫規則。
    /// </summary>
    public async Task<int> CountActiveDailyManagersAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accounts.ListAllAsync(cancellationToken);

        return accounts.Count(x =>
            x.Status == UserAccountStatus.Active
            && x.Role == ExpenseLiteRoles.Manager
            && !x.IsProtected);
    }

    public Task RecordSignInAsync(
        Guid userId,
        DateTime signedInAt,
        CancellationToken cancellationToken = default)
        => _accounts.RecordSignInAsync(userId, signedInAt, cancellationToken);

    /// <summary>
    /// 角色字串是從表單來的，不能直接信。建立帳號與換角色兩個入口都要擋，
    /// 否則有人改掉表單的 value 就能把帳號掛到一個不存在的角色上——
    /// 那個帳號登得進來，但每一頁的 <c>[Authorize(Roles = ...)]</c> 都會把他擋在外面。
    /// </summary>
    private static void EnsureRoleExists(string role)
    {
        if (!ExpenseLiteRoles.All.Contains(role))
        {
            throw new DomainRuleViolationException("指定的角色不存在。");
        }
    }

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
        UserAccountStatus.Active => 0,
        UserAccountStatus.Disabled => 1,
        _ => 2
    };
}
