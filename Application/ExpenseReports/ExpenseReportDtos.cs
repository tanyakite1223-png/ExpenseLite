using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record ExpenseReportListItemDto(
    Guid Id,
    string Title,
    string ApplicantName,
    ExpenseReportStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt);

public sealed record ExpenseReportDetailDto(
    Guid Id,
    string Title,
    string ApplicantName,
    ExpenseReportStatus Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    IReadOnlyList<ExpenseDetailDto> Details);

public sealed record ExpenseDetailDto(
    Guid Id,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    decimal Amount);
