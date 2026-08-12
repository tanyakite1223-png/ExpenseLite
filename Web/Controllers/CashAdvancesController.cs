using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.CashAdvances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

// 預支款的「動作」都是財務作業，仍然限主管：建立、修改、登記結清、標記不採用。
// 但「看」放寬了——領款人要看得到自己那筆錢的使用狀態，保管零用金的人尤其需要。
// 列表與詳情因此改成所有登入者可進入，由 Application Service 依領款人過濾內容，
// 這是資源授權（要載入資料才知道領款人是誰），不能用 [Authorize] 表達。
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
            User.ToCurrentUser(),
            cancellationToken);

        return View(page);
    }

    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await BuildCreateFormAsync(new CreateCashAdvanceForm(), cancellationToken));
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

    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var page = await BuildEditCashAdvancePageAsync(
            id,
            null,
            cancellationToken);
        if (page is null)
        {
            return NotFound();
        }

        if (!page.CashAdvance.CanEdit)
        {
            TempData["ErrorMessage"] = "這筆預支款已結清，不可修改。";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Create(
        CreateCashAdvanceForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateFormAsync(form, cancellationToken));
        }

        try
        {
            await _cashAdvances.CreateAsync(
                new CreateCashAdvanceCommand(
                    form.PayeeUserId!.Value,
                    form.Purpose,
                    form.AdvancedAt,
                    form.Amount),
                cancellationToken);

            TempData["SuccessMessage"] = "預支款已建立。";

            return RedirectToAction(nameof(Index));
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildCreateFormAsync(form, cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Edit(
        Guid id,
        [Bind(Prefix = "CashAdvanceForm")]
        EditCashAdvanceForm form,
        CancellationToken cancellationToken)
    {
        form.CashAdvanceId = id;

        if (!ModelState.IsValid)
        {
            var page = await BuildEditCashAdvancePageAsync(
                id,
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
            await _cashAdvances.UpdateAsync(
                new UpdateCashAdvanceCommand(
                    id,
                    form.Purpose,
                    form.Amount),
                cancellationToken);

            TempData["SuccessMessage"] = "預支款已修改。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var page = await BuildEditCashAdvancePageAsync(
                id,
                form,
                cancellationToken);
            if (page is null)
            {
                return NotFound();
            }

            return View(page);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
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
                    User.ToCurrentUser(),
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

    [Authorize(Roles = ExpenseLiteRoles.Manager)]
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
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
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

    [Authorize(Roles = ExpenseLiteRoles.Manager)]
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
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
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
                    User.ToCurrentUser(),
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

    private async Task<CreateCashAdvanceForm> BuildCreateFormAsync(
        CreateCashAdvanceForm form,
        CancellationToken cancellationToken)
    {
        form.PayeeOptions = await _cashAdvances.ListPayeeOptionsAsync(cancellationToken);
        return form;
    }

    private async Task<CashAdvanceSettlementPage?> BuildSettlementPageAsync(
        Guid id,
        RecordCashAdvanceSettlementForm form,
        bool useDefaultAmount,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
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

    private async Task<EditCashAdvancePage?> BuildEditCashAdvancePageAsync(
        Guid id,
        EditCashAdvanceForm? form,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
        if (cashAdvance is null)
        {
            return null;
        }

        form ??= new EditCashAdvanceForm
        {
            Purpose = cashAdvance.Purpose,
            Amount = cashAdvance.Amount
        };
        form.CashAdvanceId = id;

        return new EditCashAdvancePage
        {
            CashAdvance = cashAdvance,
            CashAdvanceForm = form
        };
    }

    private async Task<EditCashAdvanceSettlementPage?> BuildEditSettlementPageAsync(
        Guid id,
        Guid settlementRecordId,
        EditCashAdvanceSettlementForm? form,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
        var record = FindSettlementRecord(cashAdvance, settlementRecordId);
        if (cashAdvance is null || record is null)
        {
            return null;
        }

        form ??= new EditCashAdvanceSettlementForm
        {
            SettledAt = record.SettledAt,
            Amount = record.Amount,
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
        var cashAdvance = await _cashAdvances.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
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
