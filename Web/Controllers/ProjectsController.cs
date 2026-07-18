using ExpenseLite.Application.Projects;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Web.ViewModels.Projects;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseLite.Web.Controllers;

public sealed class ProjectsController : Controller
{
    private readonly ProjectAppService _projects;

    public ProjectsController(ProjectAppService projects)
    {
        _projects = projects;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(cancellationToken);
        return View(projects);
    }

    public IActionResult Create()
    {
        return View(new CreateProjectForm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
