using ExpenseLite.Application.Identity;
using ExpenseLite.Application.Projects;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public sealed class ProjectsController : Controller
{
    private readonly ProjectAppService _projects;

    public ProjectsController(ProjectAppService projects)
    {
        _projects = projects;
    }

    public async Task<IActionResult> Index(string? keyword, CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(keyword, cancellationToken);
        return View(projects);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projects.GetDetailsAsync(id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        return View(project);
    }

    // 列表與詳情所有人都看得到（員工建報銷單要選專案），但建立與結案限主管。
    // 結案會擋掉其他人的報銷單送審，不該讓員工做。
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public IActionResult Create()
    {
        return View(new CreateProjectForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Create(
        CreateProjectForm form,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await _projects.CreateAsync(
                new CreateProjectCommand(form.Name, form.CustomerName),
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
    [Authorize(Roles = ExpenseLiteRoles.Manager)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _projects.CloseAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "專案已結案。";
        }
        catch (DomainRuleViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
