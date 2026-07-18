using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.Projects;
using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Application.Projects;

public sealed class ProjectAppService
{
    private readonly IProjectRepository _projects;
    private readonly IExpenseReportRepository _expenseReports;

    public ProjectAppService(
        IProjectRepository projects,
        IExpenseReportRepository expenseReports)
    {
        _projects = projects;
        _expenseReports = expenseReports;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var projects = await _projects.ListAsync(cancellationToken);
        var unfinishedReportCounts = await _expenseReports.CountUnfinishedProjectReportsAsync(cancellationToken);

        return projects
            .OrderBy(x => x.Name)
            .Select(x => MapListItem(x, unfinishedReportCounts.GetValueOrDefault(x.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<ProjectOptionDto>> ListActiveOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var projects = await _projects.ListAsync(cancellationToken);

        return projects
            .Where(x => x.Status == ProjectStatus.Active)
            .OrderBy(x => x.Name)
            .Select(x => new ProjectOptionDto(x.Id, x.Name, x.CustomerName))
            .ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = Project.Create(command.Name, command.CustomerName);

        await _projects.AddAsync(project, cancellationToken);
        await _projects.SaveChangesAsync(cancellationToken);

        return project.Id;
    }

    public async Task CloseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            throw new DomainRuleViolationException("找不到指定的專案。");
        }

        if (await _expenseReports.HasUnfinishedProjectReportsAsync(id, cancellationToken))
        {
            throw new DomainRuleViolationException("這個專案仍有未完成的報銷單，需完成核准或拒絕後才能結案。");
        }

        project.Close();

        await _projects.SaveChangesAsync(cancellationToken);
    }

    private static ProjectListItemDto MapListItem(Project project, int unfinishedReportCount)
        => new(
            project.Id,
            project.Name,
            project.CustomerName,
            project.Status,
            unfinishedReportCount,
            project.CreatedAt);
}
