using ExpenseLite.Application.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ExpenseLite.Web.Filters;

/// <summary>
/// 把 Application 層丟出的 <see cref="ForbiddenOperationException"/> 轉成 HTTP 403。
/// 「怎麼回應瀏覽器」是 Web 層的事，所以 Application 只負責丟出「你沒權限」，
/// 不需要知道 403、AccessDenied 頁這些 HTTP 概念。
/// 用 filter 而不是在每個 action 寫 catch，是為了避免六個 action 重複同一段轉換。
/// </summary>
public sealed class ForbiddenOperationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ForbiddenOperationException)
        {
            return;
        }

        // ForbidResult 會交給登入 cookie 的 AccessDeniedPath 處理，導到 /Account/AccessDenied。
        context.Result = new ForbidResult();
        context.ExceptionHandled = true;
    }
}
