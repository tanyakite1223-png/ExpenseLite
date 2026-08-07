using ExpenseLite.Application.Identity;
using ExpenseLite.Infrastructure.Identity;
using ExpenseLite.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

[Authorize]
public sealed class AccountController : Controller
{
    // 帳號不存在與密碼錯誤回同一句話，避免讓人用登入頁反推哪些帳號真的存在。
    // 帳號狀態的訊息是例外，但那要通過密碼驗證才看得到（見 Login）。
    private const string InvalidCredentialsMessage = "帳號或密碼不正確。";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserAccountAppService _userAccounts;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        UserAccountAppService userAccounts,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userAccounts = userAccounts;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginForm());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginForm form,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var user = await _userManager.FindByNameAsync(form.UserName);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(form);
        }

        // 先驗密碼、再判帳號狀態，順序不能反過來。
        // 如果先擋狀態，不知道密碼的人就能靠訊息差異（「尚待啟用」vs「帳號或密碼不正確」）
        // 反推哪些帳號真的存在。CheckPasswordSignInAsync 只驗密碼、不發 cookie，
        // 但仍會累計失敗次數，所以登入失敗鎖定保留著。
        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(
            user,
            form.Password,
            lockoutOnFailure: true);

        if (passwordCheck.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "登入失敗次數過多，帳號已暫時鎖定，請稍後再試。");
            return View(form);
        }

        if (!passwordCheck.Succeeded)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(form);
        }

        // 密碼對了才輪到狀態，這時候把話講明白對本人才有幫助。
        if (user.Status != UserAccountStatus.Active)
        {
            ModelState.AddModelError(string.Empty, StatusMessage(user.Status));
            return View(form);
        }

        await _signInManager.SignInAsync(user, form.RememberMe);

        // 記下登入時間，讓使用者管理頁答得出「這個帳號被用過嗎」。
        await _userAccounts.RecordSignInAsync(user.Id, DateTime.UtcNow, cancellationToken);

        if (user.IsProtected)
        {
            // 緊急存取帳號被使用是需要有人知道的事。這套系統沒有寄信或外部告警的管道，
            // 所以告警分兩路：一路留在伺服器 log，一路做成畫面上的橫幅與使用者管理頁的登入時間。
            _logger.LogWarning(
                "緊急存取帳號 {UserName} 於 {SignedInAt:u} 登入。若不是預期中的維運操作，請立即更換它的密碼。",
                user.UserName,
                DateTime.UtcNow);
        }

        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterForm());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _userAccounts.RegisterAsync(
            new RegisterUserCommand(form.UserName, form.Email, form.DisplayName, form.Password),
            cancellationToken);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(form);
        }

        // 註冊完不自動登入——帳號還是 Pending，本來就登不進去。
        TempData["SuccessMessage"] = "註冊完成。帳號需要主管啟用後才能登入，請聯絡主管。";

        return RedirectToAction(nameof(Login));
    }

    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _userAccounts.ChangeOwnPasswordAsync(
            User.ToCurrentUser(),
            form.CurrentPassword,
            form.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(form);
        }

        // 改密碼會換掉 security stamp，手上這張 cookie 在下一次驗證時就會失效。
        // 這裡主動換一張新的，否則「改完自己的密碼」會變成把自己踢出去。
        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        TempData["SuccessMessage"] = "密碼已更新。";

        return RedirectToAction(nameof(ChangePassword));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// 把 Application 回傳的錯誤逐條放進 ModelState。
    /// 這類錯誤（帳號重複、密碼太短、舊密碼不對）是使用者可以自己修正的，
    /// 所以走驗證訊息而不是例外。
    /// </summary>
    private void AddErrors(UserAccountResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private static string StatusMessage(UserAccountStatus status) => status switch
    {
        UserAccountStatus.Pending => "此帳號尚待主管啟用，啟用後才能登入。",
        UserAccountStatus.Disabled => "此帳號已停用，如需恢復請洽主管。",
        _ => InvalidCredentialsMessage
    };

    /// <summary>
    /// 只接受站內網址，避免有人在 returnUrl 塞外部連結做開放式轉址（open redirect）。
    /// </summary>
    private IActionResult RedirectToLocal(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
