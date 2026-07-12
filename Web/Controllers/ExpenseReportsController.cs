using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.ExpenseReports;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public sealed class ExpenseReportsController : Controller
{
    private readonly ExpenseReportAppService _expenseReports;

    public ExpenseReportsController(ExpenseReportAppService expenseReports)
    {
        _expenseReports = expenseReports;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var reports = await _expenseReports.ListAsync(cancellationToken);
        return View(reports);
    }

    public IActionResult Create()
    {
        return View(new CreateExpenseReportForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExpenseReportForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            var id = await _expenseReports.CreateAsync(
                new CreateExpenseReportCommand(form.Title, form.ApplicantName),
                cancellationToken);

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainRuleViolationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(form);
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
}
