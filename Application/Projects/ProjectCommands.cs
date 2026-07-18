namespace ExpenseLite.Application.Projects;

public sealed record CreateProjectCommand(
    string Name,
    string CustomerName);
