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

    public async Task<IActionResult> Index(
        string? keyword,
        CashAdvanceReconciliationStatus? reconciliationStatus,
        CancellationToken cancellationToken)
    {
        var page = await _cashAdvances.ListPageAsync(
            new CashAdvanceListQuery(keyword, reconciliationStatus),
            cancellationToken);

        return View(page);
    }

    public IActionResult Create()
    {
        return View(new CreateCashAdvanceForm());
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildSettlementPageAsync(
            id,
            new RecordCashAdvanceSettlementForm(),
            useDefaultAmount: true,
            cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        return View(page);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordSettlement(
        Guid id,
        [Bind(Prefix = "Settlement")]
        RecordCashAdvanceSettlementForm form,
        CancellationToken cancellationToken)
    {
        form.CashAdvanceId = id;

        if (!ModelState.IsValid)
        {
            var page = await BuildSettlementPageAsync(
                id,
                form,
                useDefaultAmount: false,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(nameof(Details), page);
        }

        try
        {
            await _cashAdvances.RecordSettlementAsync(
                new RecordCashAdvanceSettlementCommand(
                    id,
                    form.SettledAt,
                    form.Amount,
                    form.HandledBy,
                    form.Note),
                cancellationToken);

            TempData["SuccessMessage"] = "結清紀錄已新增。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var page = await BuildSettlementPageAsync(
                id,
                form,
                useDefaultAmount: false,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(nameof(Details), page);
        }
    }

    private async Task<CashAdvanceSettlementPage?> BuildSettlementPageAsync(
        Guid id,
        RecordCashAdvanceSettlementForm form,
        bool useDefaultAmount,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, cancellationToken);
        if (cashAdvance is null)
        {
            return null;
        }

        form.CashAdvanceId = id;
        if (useDefaultAmount && cashAdvance.RemainingSettlementAmount > 0m)
        {
            form.Amount = cashAdvance.RemainingSettlementAmount;
        }

        return new CashAdvanceSettlementPage
        {
            CashAdvance = cashAdvance,
            Settlement = form
        };
    }
}
