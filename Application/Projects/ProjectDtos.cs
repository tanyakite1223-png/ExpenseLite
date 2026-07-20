using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.Projects;

namespace ExpenseLite.Application.Projects;

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    string CustomerName,
    ProjectStatus Status,
    int UnfinishedExpenseReportCount,
    DateTimeOffset CreatedAt);

public sealed record ProjectListPageDto(
    string Keyword,
    int TotalProjectCount,
    IReadOnlyList<ProjectListItemDto> Projects);

public sealed record ProjectOptionDto(
    Guid Id,
    string Name,
    string CustomerName);

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string CustomerName,
    ProjectStatus Status,
    int UnfinishedExpenseReportCount,
    int TotalExpenseReportCount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ExpenseReportListItemDto> ExpenseReports);
