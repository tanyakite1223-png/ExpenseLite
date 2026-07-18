using ExpenseLite.Domain.Projects;

namespace ExpenseLite.Application.Projects;

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    string CustomerName,
    ProjectStatus Status,
    DateTimeOffset CreatedAt);

public sealed record ProjectOptionDto(
    Guid Id,
    string Name,
    string CustomerName);
