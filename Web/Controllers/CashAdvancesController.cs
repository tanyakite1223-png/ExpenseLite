using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.CashAdvances;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public sealed class CashAdvancesController : Controller
{
    private readonly CashAdvanceAppService _cashAdvances;

    public CashAdvancesController(CashAdvanceAppService cashAdvances)
    {
        _cashAdvances = cashAdvances;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        return View(cashAdvances);
    }

    public IActionResult Create()
    {
        return View(new CreateCashAdvanceForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCashAdvanceForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _cashAdvances.CreateAsync(
                new CreateCashAdvanceCommand(
                    form.PayeeName,
                    form.Purpose,
                    form.AdvancedAt,
                    form.Amount),
                cancellationToken);

            return RedirectToAction(nameof(Index));
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
        }
    }
}
