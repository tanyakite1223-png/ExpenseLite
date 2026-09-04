using ExpenseLite.Application.ExpenseCategories;
using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.ExpenseCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

[Authorize(Roles = ExpenseLiteRoles.Manager)]
public sealed class ExpenseCategoriesController : Controller
{
    private readonly ExpenseCategoryAppService _categories;

    public ExpenseCategoriesController(ExpenseCategoryAppService categories)
    {
        _categories = categories;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var page = await _categories.ListAsync(cancellationToken);
        return View(page);
    }

    public IActionResult Create()
    {
        return View(new CreateExpenseCategoryForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateExpenseCategoryForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _categories.CreateAsync(form.Name, cancellationToken);
            TempData["SuccessMessage"] = "類別已新增。";
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
    {
        try
        {
            await _categories.ActivateAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "類別已啟用。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _categories.DeactivateAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "類別已停用。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
