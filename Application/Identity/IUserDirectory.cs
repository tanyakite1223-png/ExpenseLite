namespace ExpenseLite.Application.Identity;

/// <summary>下拉選單用的使用者選項。刻意只給 Id 與顯示姓名，不外洩 email、帳號狀態等資訊。</summary>
public sealed record UserOptionDto(Guid UserId, string DisplayName);

/// <summary>
/// 查詢「系統裡有哪些人」。介面定義在 Application、實作在 Infrastructure（走 ASP.NET Core Identity），
/// 跟 repository 是同一個依賴反轉的道理：Application 不該認識 UserManager 或 ApplicantUser。
/// 用途有兩個：畫下拉選單，以及在寫入資料時查出「當下的姓名」當快照。
/// </summary>
public interface IUserDirectory
{
    /// <summary>列出可以被指派的使用者（停用帳號不列）。</summary>
    Task<IReadOnlyList<UserOptionDto>> ListSelectableAsync(CancellationToken cancellationToken = default);

    /// <summary>依 UserId 查一個人；找不到回 null。</summary>
    Task<UserOptionDto?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
