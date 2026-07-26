using ExpenseLite.Infrastructure.Identity;
using ExpenseLite.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

[Authorize]
public sealed class AccountController : Controller
{
    // 不論是帳號不存在、密碼錯誤還是帳號已停用，都回同一句話，
    // 避免讓人用登入頁反推哪些帳號是有效的。
    private const string InvalidCredentialsMessage = "帳號或密碼不正確。";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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
        string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var user = await _userManager.FindByNameAsync(form.UserName);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(form);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            form.Password,
            form.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "登入失敗次數過多，帳號已暫時鎖定，請稍後再試。");
            return View(form);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, InvalidCredentialsMessage);
            return View(form);
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
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// 只接受站內網址，避免有人在 returnUrl 塞外部連結做開放式轉址（open redirect）。
    /// </summary>
    private IActionResult RedirectToLocal(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
