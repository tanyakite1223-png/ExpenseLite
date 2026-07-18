using ExpenseLite.Domain.Projects;

namespace ExpenseLite.Application.Projects;

public sealed class ProjectAppService
{
    private readonly IProjectRepository _projects;

    public ProjectAppService(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var projects = await _projects.ListAsync(cancellationToken);

        return projects
            .OrderBy(x => x.Name)
            .Select(MapListItem)
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

    private static ProjectListItemDto MapListItem(Project project)
        => new(
            project.Id,
            project.Name,
            project.CustomerName,
            project.Status,
            project.CreatedAt);
}
