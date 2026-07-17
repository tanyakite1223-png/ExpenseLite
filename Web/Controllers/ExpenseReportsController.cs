using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.ExpenseReports;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public sealed class ExpenseReportsController : Controller
{
    private readonly ExpenseReportAppService _expenseReports;
    private readonly CashAdvanceAppService _cashAdvances;

    public ExpenseReportsController(
        ExpenseReportAppService expenseReports,
        CashAdvanceAppService cashAdvances)
    {
        _expenseReports = expenseReports;
        _cashAdvances = cashAdvances;
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
        form.CashAdvanceOptions = await _cashAdvances.ListOpenOptionsAsync(cancellationToken);
        return form;
    }
}
