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

    public async Task<IActionResult> EditSettlement(
        Guid id,
        Guid settlementRecordId,
        CancellationToken cancellationToken)
    {
        var page = await BuildEditSettlementPageAsync(
            id,
            settlementRecordId,
            null,
            cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        if (!page.SettlementRecord.CanEdit)
        {
            TempData["ErrorMessage"] = "這筆結清紀錄目前不可修改。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSettlement(
        Guid id,
        Guid settlementRecordId,
        [Bind(Prefix = "Settlement")]
        EditCashAdvanceSettlementForm form,
        CancellationToken cancellationToken)
    {
        form.CashAdvanceId = id;
        form.SettlementRecordId = settlementRecordId;

        if (!ModelState.IsValid)
        {
            var page = await BuildEditSettlementPageAsync(
                id,
                settlementRecordId,
                form,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(page);
        }

        try
        {
            await _cashAdvances.UpdateSettlementAsync(
                new UpdateCashAdvanceSettlementCommand(
                    id,
                    settlementRecordId,
                    form.SettledAt,
                    form.Amount,
                    form.HandledBy,
                    form.Note),
                cancellationToken);

            TempData["SuccessMessage"] = "結清紀錄已修改。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var page = await BuildEditSettlementPageAsync(
                id,
                settlementRecordId,
                form,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(page);
        }
    }

    public async Task<IActionResult> VoidSettlement(
        Guid id,
        Guid settlementRecordId,
        CancellationToken cancellationToken)
    {
        var page = await BuildVoidSettlementPageAsync(
            id,
            settlementRecordId,
            new VoidCashAdvanceSettlementForm(),
            cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        if (!page.SettlementRecord.CanVoid)
        {
            TempData["ErrorMessage"] = "這筆結清紀錄目前不可標記為不採用。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoidSettlement(
        Guid id,
        Guid settlementRecordId,
        [Bind(Prefix = "Settlement")]
        VoidCashAdvanceSettlementForm form,
        CancellationToken cancellationToken)
    {
        form.CashAdvanceId = id;
        form.SettlementRecordId = settlementRecordId;

        if (!ModelState.IsValid)
        {
            var page = await BuildVoidSettlementPageAsync(
                id,
                settlementRecordId,
                form,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(page);
        }

        try
        {
            await _cashAdvances.VoidSettlementAsync(
                new VoidCashAdvanceSettlementCommand(
                    id,
                    settlementRecordId,
                    form.VoidedBy,
                    form.VoidReason),
                cancellationToken);

            TempData["SuccessMessage"] = "結清紀錄已標記為不採用。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var page = await BuildVoidSettlementPageAsync(
                id,
                settlementRecordId,
                form,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(page);
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

    private async Task<EditCashAdvanceSettlementPage?> BuildEditSettlementPageAsync(
        Guid id,
        Guid settlementRecordId,
        EditCashAdvanceSettlementForm? form,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, cancellationToken);
        var record = FindSettlementRecord(cashAdvance, settlementRecordId);
        if (cashAdvance is null || record is null)
        {
            return null;
        }

        form ??= new EditCashAdvanceSettlementForm
        {
            SettledAt = record.SettledAt,
            Amount = record.Amount,
            HandledBy = record.HandledBy,
            Note = record.Note
        };
        form.CashAdvanceId = id;
        form.SettlementRecordId = settlementRecordId;

        return new EditCashAdvanceSettlementPage
        {
            CashAdvance = cashAdvance,
            SettlementRecord = record,
            Settlement = form
        };
    }

    private async Task<VoidCashAdvanceSettlementPage?> BuildVoidSettlementPageAsync(
        Guid id,
        Guid settlementRecordId,
        VoidCashAdvanceSettlementForm form,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, cancellationToken);
        var record = FindSettlementRecord(cashAdvance, settlementRecordId);
        if (cashAdvance is null || record is null)
        {
            return null;
        }

        form.CashAdvanceId = id;
        form.SettlementRecordId = settlementRecordId;

        return new VoidCashAdvanceSettlementPage
        {
            CashAdvance = cashAdvance,
            SettlementRecord = record,
            Settlement = form
        };
    }

    private static CashAdvanceSettlementRecordDto? FindSettlementRecord(
        CashAdvanceSettlementDetailDto? cashAdvance,
        Guid settlementRecordId)
        => cashAdvance?.SettlementRecords.SingleOrDefault(x => x.Id == settlementRecordId);
}
