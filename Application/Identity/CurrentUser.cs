namespace ExpenseLite.Application.Identity;

/// <summary>
/// 正在執行動作的登入者。Application 只認識這個型別，不認識 ClaimsPrincipal 或 ApplicationUser，
/// 所以「登入者是誰」怎麼來的（cookie、claim）是 Web 層的事。
/// UserId 之後用來查「誰做的」，DisplayName 則會被寫進紀錄當「當時的姓名快照」。
/// </summary>
public sealed record CurrentUser(Guid UserId, string DisplayName);
