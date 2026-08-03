namespace ExpenseLite.Application.Identity;

/// <summary>
/// 正在執行動作的登入者。Application 只認識這個型別，不認識 ClaimsPrincipal 或 ApplicationUser，
/// 所以「登入者是誰」怎麼來的（cookie、claim）是 Web 層的事。
/// UserId 之後用來查「誰做的」，DisplayName 則會被寫進紀錄當「當時的姓名快照」。
/// IsManager 是給「同一份資料，主管看得比較多」這種可見度判斷用的——
/// 純角色檢查（例如「只有主管能核准」）仍然擋在 Controller 的 [Authorize]，不需要走到這裡。
/// </summary>
public sealed record CurrentUser(Guid UserId, string DisplayName, bool IsManager);
