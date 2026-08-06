using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Identity;
using ExpenseLite.Application.Projects;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.ExpenseReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public sealed class ExpenseReportsController : Controller
{
    private readonly ExpenseReportAppService _expenseReports;
    private readonly CashAdvanceAppService _cashAdvances;
    private readonly ProjectAppService _projects;

    public ExpenseReportsController(
        ExpenseReportAppService expenseReports,
        CashAdvanceAppService cashAdvances,
        ProjectAppService projects)
    {
        _expenseReports = expenseReports;
        _cashAdvances = cashAdvances;
        _projects = projects;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        ExpenseReportStatus? status,
        ExpenseType? expenseType,
        ExpensePaymentMethod? paymentMethod,
        CancellationToken cancellationToken)
    {
        var page = await _expenseReports.ListPageAsync(
            new ExpenseReportListQuery(keyword, status, expenseType, paymentMethod),
            User.ToCurrentUser(),
            cancellationToken);

        return View(page);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await BuildCreateFormAsync(new CreateExpenseReportForm(), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseReportForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildCreateFormAsync(form, cancellationToken));
        }

        try
        {
            var id = await _expenseReports.CreateAsync(
                new CreateExpenseReportCommand(
                    form.Title,
                    User.ToCurrentUser(),
                    form.ExpenseType,
                    form.ProjectId,
                    form.PaymentMethod,
                    form.CashAdvanceId),
                cancellationToken);

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildCreateFormAsync(form, cancellationToken));
        }
    }

    public async Task<IActionResult> Details(Guid id, Guid? editDetailId, CancellationToken cancellationToken)
    {
        var report = await _expenseReports.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var editDetail = editDetailId is null
            ? null
            : BuildEditDetailForm(report, editDetailId.Value);
        if (editDetailId is not null && editDetail is null)
        {
            return NotFound();
        }

        return View(new ExpenseReportDetailsPage
        {
            Report = report,
            EditDetail = editDetail
        });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var report = await _expenseReports.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        // 真正的把關在 Application Service，這裡是為了讓非申請人連表單都看不到。
        if (report.ApplicantUserId != User.GetUserId())
        {
            return Forbid();
        }

        if (!CanEditReport(report.Status))
        {
            TempData["ErrorMessage"] = "只有草稿或退回的報銷單可以修改。";
            return RedirectToAction(nameof(Details), new { id });
        }

        var form = new EditExpenseReportForm
        {
            Id = report.Id,
            Title = report.Title,
            ApplicantName = report.ApplicantName,
            ExpenseType = report.ExpenseType,
            ProjectId = report.ProjectId,
            PaymentMethod = report.PaymentMethod,
            CashAdvanceId = report.CashAdvanceId
        };

        return View(await BuildEditFormAsync(form, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditExpenseReportForm form, CancellationToken cancellationToken)
    {
        form.Id = id;

        if (!ModelState.IsValid)
        {
            return View(await BuildEditFormAsync(form, cancellationToken));
        }

        try
        {
            await _expenseReports.UpdateAsync(
                new UpdateExpenseReportCommand(
                    id,
                    User.ToCurrentUser(),
                    form.Title,
                    form.ExpenseType,
                    form.ProjectId,
                    form.PaymentMethod,
                    form.CashAdvanceId),
                cancellationToken);

            TempData["SuccessMessage"] = "報銷單已更新。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildEditFormAsync(form, cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDetail(Guid id, AddExpenseDetailForm newDetail, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var report = await _expenseReports.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
            if (report is null)
            {
                return NotFound();
            }

            return View(nameof(Details), new ExpenseReportDetailsPage
            {
                Report = report,
                NewDetail = newDetail
            });
        }

        try
        {
            await _expenseReports.AddDetailAsync(
                new AddExpenseDetailCommand(
                    id,
                    User.ToCurrentUser(),
                    newDetail.ExpenseDate,
                    newDetail.Category,
                    newDetail.Description,
                    newDetail.ReceiptType,
                    newDetail.InvoiceNumber,
                    newDetail.Amount),
                cancellationToken);

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDetail(
        Guid id,
        Guid detailId,
        EditExpenseDetailForm form,
        CancellationToken cancellationToken)
    {
        form.Id = detailId;

        if (!ModelState.IsValid)
        {
            var report = await _expenseReports.GetDetailsAsync(id, User.ToCurrentUser(), cancellationToken);
            if (report is null)
            {
                return NotFound();
            }

            return View(nameof(Details), new ExpenseReportDetailsPage
            {
                Report = report,
                EditDetail = form
            });
        }

        try
        {
            await _expenseReports.UpdateDetailAsync(
                new UpdateExpenseDetailCommand(
                    id,
                    detailId,
                    User.ToCurrentUser(),
                    form.ExpenseDate,
                    form.Category,
                    form.Description,
                    form.ReceiptType,
                    form.InvoiceNumber,
                    form.Amount),
                cancellationToken);

            TempData["SuccessMessage"] = "明細已更新。";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveDetail(Guid id, Guid detailId, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseReports.RemoveDetailAsync(
                new RemoveExpenseDetailCommand(id, detailId, User.ToCurrentUser()),
                cancellationToken);
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseReports.SubmitAsync(
                new SubmitExpenseReportCommand(id, User.ToCurrentUser()),
                cancellationToken);
            TempData["SuccessMessage"] = "報銷單已送審。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // 核准 / 退回 / 拒絕是純角色檢查（「你是不是主管」），不需要載入報銷單就能判斷，
    // 所以擋在 Controller 的 [Authorize] 而不是 Application Service。
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Return(Guid id, ReviewExpenseReportForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _expenseReports.ReturnAsync(
                new ReviewExpenseReportCommand(id, User.ToCurrentUser(), form.Reason),
                cancellationToken);
            TempData["SuccessMessage"] = "報銷單已退回。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Approve(Guid id, ReviewExpenseReportForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _expenseReports.ApproveAsync(
                new ReviewExpenseReportCommand(id, User.ToCurrentUser(), form.Reason),
                cancellationToken);
            TempData["SuccessMessage"] = "報銷單已核准。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Reject(Guid id, ReviewExpenseReportForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _expenseReports.RejectAsync(
                new ReviewExpenseReportCommand(id, User.ToCurrentUser(), form.Reason),
                cancellationToken);
            TempData["SuccessMessage"] = "報銷單已拒絕。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<CreateExpenseReportForm> BuildCreateFormAsync(
        CreateExpenseReportForm form,
        CancellationToken cancellationToken)
    {
        form.ProjectOptions = await _projects.ListActiveOptionsAsync(cancellationToken);
        form.CashAdvanceOptions = await _cashAdvances.ListOpenOptionsAsync(User.ToCurrentUser(), cancellationToken);
        return form;
    }

    private async Task<EditExpenseReportForm> BuildEditFormAsync(
        EditExpenseReportForm form,
        CancellationToken cancellationToken)
    {
        form.ProjectOptions = await _projects.ListActiveOptionsAsync(cancellationToken);
        form.CashAdvanceOptions = await _cashAdvances.ListOpenOptionsAsync(User.ToCurrentUser(), cancellationToken);

        // 申請人在畫面上是唯讀文字、不會隨表單送回來，所以驗證失敗重新顯示時要自己補回去。
        if (string.IsNullOrWhiteSpace(form.ApplicantName))
        {
            var report = await _expenseReports.GetDetailsAsync(form.Id, User.ToCurrentUser(), cancellationToken);
            form.ApplicantName = report?.ApplicantName ?? string.Empty;
        }

        return form;
    }

    private static bool CanEditReport(ExpenseReportStatus status)
        => status is ExpenseReportStatus.Draft or ExpenseReportStatus.Returned;

    private static EditExpenseDetailForm? BuildEditDetailForm(ExpenseReportDetailDto report, Guid detailId)
    {
        var detail = report.Details.SingleOrDefault(x => x.Id == detailId);
        if (detail is null)
        {
            return null;
        }

        return new EditExpenseDetailForm
        {
            Id = detail.Id,
            ExpenseDate = detail.ExpenseDate,
            Category = detail.Category,
            Description = detail.Description,
            ReceiptType = detail.ReceiptType,
            InvoiceNumber = detail.InvoiceNumber,
            Amount = detail.Amount
        };
    }

    private string GetFirstModelError()
        => ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? "表單資料不完整。";
}
