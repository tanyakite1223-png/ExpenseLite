using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Projects;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.ExpenseReports;
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

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reports = await _expenseReports.ListAsync(cancellationToken);
        return View(reports);
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
                    form.ApplicantName,
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

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var report = await _expenseReports.GetDetailsAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return View(new ExpenseReportDetailsPage { Report = report });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var report = await _expenseReports.GetDetailsAsync(id, cancellationToken);
        if (report is null)
        {
            return NotFound();
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
                    form.Title,
                    form.ApplicantName,
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
            var report = await _expenseReports.GetDetailsAsync(id, cancellationToken);
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
    public async Task<IActionResult> RemoveDetail(Guid id, Guid detailId, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseReports.RemoveDetailAsync(id, detailId, cancellationToken);
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
            await _expenseReports.SubmitAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "報銷單已送審。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseReports.ReturnAsync(id, cancellationToken);
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
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseReports.ApproveAsync(id, cancellationToken);
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
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _expenseReports.RejectAsync(id, cancellationToken);
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
        form.CashAdvanceOptions = await _cashAdvances.ListOpenOptionsAsync(cancellationToken);
        return form;
    }

    private async Task<EditExpenseReportForm> BuildEditFormAsync(
        EditExpenseReportForm form,
        CancellationToken cancellationToken)
    {
        form.ProjectOptions = await _projects.ListActiveOptionsAsync(cancellationToken);
        form.CashAdvanceOptions = await _cashAdvances.ListOpenOptionsAsync(cancellationToken);
        return form;
    }

    private static bool CanEditReport(ExpenseReportStatus status)
        => status is ExpenseReportStatus.Draft or ExpenseReportStatus.Returned;
}
