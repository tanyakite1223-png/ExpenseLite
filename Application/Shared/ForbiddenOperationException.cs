namespace ExpenseLite.Application.Shared;

/// <summary>
/// 登入者沒有權限執行這個動作。
/// 刻意跟 <see cref="Domain.Shared.DomainRuleViolationException"/> 分開：
/// 那個是「這個動作在業務上不合法」（該顯示訊息讓人修正），這個是「你不該碰這筆資料」（該回 403）。
/// 放在 Application 而不是 Domain，因為「誰可以呼叫」是應用層的存取控制，不是 entity 的 invariant。
/// </summary>
public sealed class ForbiddenOperationException : InvalidOperationException
{
    public ForbiddenOperationException(string message)
        : base(message)
    {
    }
}
