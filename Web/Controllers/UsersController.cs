using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

/// <summary>
/// 使用者管理。整個 controller 都是主管專用，所以 <c>[Authorize]</c> 標在類別上——
/// 這裡沒有任何「同一份資料，不同人看到不一樣」的情況，用角色就表達得完，
/// 不需要把判斷往 Application Service 下沉。
/// </summary>
[Authorize(Roles = ExpenseLiteRoles.Manager)]
public sealed class UsersController : Controller
{
    private readonly UserAccountAppService _userAccounts;

    public UsersController(UserAccountAppService userAccounts)
    {
        _userAccounts = userAccounts;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var accounts = await _userAccounts.ListAsync(cancellationToken);
        return View(accounts);
    }

    public IActionResult Create()
    {
        return View(new CreateUserForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateUserForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var result = await _userAccounts.CreateAsync(
                new CreateUserCommand(
                    form.UserName,
                    form.Email,
                    form.DisplayName,
                    form.Role,
                    form.Password),
                cancellationToken);

            if (!result.Succeeded)
            {
                // 帳號或 Email 重複、密碼太短這類錯誤走 ModelState：
                // 主管留在原本的表單上改掉那一欄就好，不必重打整張表。
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(form);
            }

            TempData["SuccessMessage"] =
                $"已建立 {form.DisplayName}（{form.UserName}）的帳號，" +
                "請當面或用其他管道把帳號與預設密碼給本人，並提醒他登入後自行修改密碼。";

            return RedirectToAction(nameof(Index));
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => await RunAsync(
            () => _userAccounts.ActivateAsync(id, cancellationToken),
            "帳號已啟用。");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
        => await RunAsync(
            () => _userAccounts.DisableAsync(id, cancellationToken),
            "帳號已停用。");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(Guid id, string role, CancellationToken cancellationToken)
        => await RunAsync(
            () => _userAccounts.SetRoleAsync(id, role, cancellationToken),
            $"角色已改為{ExpenseLiteRoles.GetDisplayName(role)}。");

    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var account = await _userAccounts.FindAsync(id, cancellationToken);
        if (account is null)
        {
            return NotFound();
        }

        return View(new ResetUserPasswordForm
        {
            UserId = account.UserId,
            UserName = account.UserName,
            DisplayName = account.DisplayName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetUserPasswordForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var result = await _userAccounts.ResetPasswordAsync(
                form.UserId,
                form.NewPassword,
                cancellationToken);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(form);
            }

            TempData["SuccessMessage"] =
                $"已重設 {form.DisplayName} 的密碼，請當面或用其他管道把新密碼給本人，並提醒他登入後自行修改。";

            return RedirectToAction(nameof(Index));
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }

    /// <summary>
    /// 列表上的按鈕動作長得都一樣：跑一個回 <see cref="UserAccountResult"/> 的操作，
    /// 成功就報喜、失敗就把訊息放進 TempData，最後都回列表。
    /// </summary>
    private async Task<IActionResult> RunAsync(
        Func<Task<UserAccountResult>> operation,
        string successMessage)
    {
        try
        {
            var result = await operation();

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = successMessage;
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("；", result.Errors);
            }
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
