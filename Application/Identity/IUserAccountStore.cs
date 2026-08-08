namespace ExpenseLite.Application.Identity;

/// <summary>
/// 帳號的建立與維護。介面定義在 Application、實作在 Infrastructure（走 ASP.NET Core Identity），
/// 跟 <see cref="IUserDirectory"/>、repository 是同一個依賴反轉的形狀。
///
/// 為什麼不直接把這些方法加進 <see cref="IUserDirectory"/>：那個介面的用途是「查系統裡有哪些人」，
/// 唯讀、只回姓名，報銷單與預支款畫下拉選單時到處在用。
/// 這個介面會改帳號狀態、改角色、改密碼，兩者的權限範圍差太多，混在一起會讓
/// 「只是想畫個下拉」的地方也拿到一整套帳號管理能力。
/// </summary>
public interface IUserAccountStore
{
    /// <summary>列出所有帳號，含已停用的（管理頁要看得到全部）。</summary>
    Task<IReadOnlyList<UserAccountDto>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<UserAccountDto?> FindAccountByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>建立一個啟用中的帳號，角色由呼叫者指定（把關在 <c>UsersController</c> 的角色授權）。</summary>
    Task<UserAccountResult> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default);

    Task<UserAccountResult> SetStatusAsync(
        Guid userId,
        UserAccountStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>把使用者換成指定角色（本系統一人只有一個角色，所以是換不是加）。</summary>
    Task<UserAccountResult> SetRoleAsync(
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    /// <summary>本人改自己的密碼，要驗舊密碼。</summary>
    Task<UserAccountResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>主管替別人重設密碼，不需要舊密碼——因為忘記密碼的人本來就講不出舊的。</summary>
    Task<UserAccountResult> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>記下這次登入的時間。用來回答「這個帳號被用過嗎」，特別是緊急存取帳號。</summary>
    Task RecordSignInAsync(Guid userId, DateTime signedInAt, CancellationToken cancellationToken = default);
}
