namespace ExpenseLite.Application.Identity;

/// <summary>
/// 系統角色。放在 Application 層而不是 Infrastructure，因為「員工 / 主管」是業務概念，
/// Web 的 [Authorize] 與 Infrastructure 的種子資料都會用到，不該綁在 Identity 的實作細節上。
/// </summary>
public static class ExpenseLiteRoles
{
    public const string Employee = "Employee";

    public const string Manager = "Manager";

    public static IReadOnlyList<string> All { get; } = [Employee, Manager];

    public static string GetDisplayName(string role)
        => role switch
        {
            Employee => "員工",
            Manager => "主管",
            _ => role
        };
}
