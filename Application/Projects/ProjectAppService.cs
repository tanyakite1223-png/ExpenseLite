using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.ExpenseReports;
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

    public async Task<ProjectListPageDto> ListAsync(
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var projects = await _projects.ListAsync(cancellationToken);
        var unfinishedReportCounts = await _expenseReports.CountUnfinishedProjectReportsAsync(cancellationToken);
        var normalizedKeyword = NormalizeKeyword(keyword);

        var items = projects
            .Where(x => MatchesKeyword(x, normalizedKeyword))
            .OrderBy(x => x.Name)
            .Select(x => MapListItem(x, unfinishedReportCounts.GetValueOrDefault(x.Id)))
            .ToList();

        return new ProjectListPageDto(normalizedKeyword, projects.Count, items);
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

    public async Task<ProjectDetailDto?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        var reports = await _expenseReports.ListByProjectIdAsync(id, cancellationToken);
        var reportItems = reports
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => MapExpenseReportListItem(x, project.Name))
            .ToList();

        return new ProjectDetailDto(
            project.Id,
            project.Name,
            project.CustomerName,
            project.Status,
            CountUnfinishedReports(reports),
            reports.Count,
            project.CreatedAt,
            reportItems);
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

    private static ExpenseReportListItemDto MapExpenseReportListItem(ExpenseReport report, string projectName)
        => new(
            report.Id,
            report.Title,
            report.ApplicantName,
            report.Status,
            report.ExpenseType,
            report.ProjectId,
            projectName,
            report.PaymentMethod,
            report.CashAdvanceId,
            report.TotalAmount.Amount,
            report.CreatedAt);

    private static int CountUnfinishedReports(IReadOnlyList<ExpenseReport> reports)
        => reports.Count(x =>
            x.Status != ExpenseReportStatus.Approved &&
            x.Status != ExpenseReportStatus.Rejected);

    private static string NormalizeKeyword(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? string.Empty : keyword.Trim();

    private static bool MatchesKeyword(Project project, string keyword)
    {
        if (keyword.Length == 0)
        {
            return true;
        }

        return ContainsKeyword(project.Name, keyword) ||
               ContainsKeyword(project.CustomerName, keyword) ||
               ContainsKeyword(ProjectStatusText(project.Status), keyword);
    }

    private static bool ContainsKeyword(string value, string keyword)
        => value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static string ProjectStatusText(ProjectStatus status) => status switch
    {
        ProjectStatus.Active => "進行中",
        ProjectStatus.Closed => "已結案",
        _ => status.ToString()
    };
}
