using ExpenseLite.Application.Identity;

namespace ExpenseLite.Web.Middleware;

/// <summary>
/// 已登入但被要求先改密碼的人，除了「修改密碼」與「登出」以外的請求一律導到修改密碼頁。
///
/// 用 middleware 而不是 MVC filter 的原因：這件事跟具體 controller / action 無關，
/// 是「全站的一道通道閘門」。放在 filter 就得每個 controller 標一次 attribute，
/// 漏掉一個就是缺口；middleware 是預設關起來，例外用允許路徑白名單維持。
/// </summary>
public sealed class RequirePasswordChangeMiddleware
{
    private static readonly string[] AllowedPaths =
    {
        "/Account/ChangePassword",
        "/Account/Logout"
    };

    private readonly RequestDelegate _next;

    public RequirePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // 未登入的請求交給既有的授權管線處理，這裡不插手。
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return _next(context);
        }

        if (!context.User.MustChangePassword())
        {
            return _next(context);
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsAllowed(path))
        {
            return _next(context);
        }

        // 走 302 而不是 rewrite：讓瀏覽器網址列直接變成 /Account/ChangePassword，
        // 使用者看得出來自己被強制導過來了。
        context.Response.Redirect("/Account/ChangePassword");
        return Task.CompletedTask;
    }

    private static bool IsAllowed(string path)
    {
        foreach (var allowed in AllowedPaths)
        {
            if (path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
