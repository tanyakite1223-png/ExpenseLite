using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Projects;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record ExpenseReportListItemDto(
    Guid Id,
    string Title,
    string ApplicantName,
    ExpenseReportStatus Status,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    string? ProjectName,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId,
    decimal TotalAmount,
    DateTimeOffset CreatedAt);

public sealed record ExpenseReportDetailDto(
    Guid Id,
    string Title,
    string ApplicantName,
    ExpenseReportStatus Status,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    string? ProjectName,
    ProjectStatus? ProjectStatus,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    IReadOnlyList<ExpenseDetailDto> Details,
    IReadOnlyList<ExpenseReviewRecordDto> ReviewRecords);

public sealed record ExpenseDetailDto(
    Guid Id,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    ExpenseReceiptType ReceiptType,
    string InvoiceNumber,
    decimal Amount);

public sealed record ExpenseReviewRecordDto(
    Guid Id,
    ExpenseReviewAction Action,
    string ReviewerName,
    string Reason,
    DateTimeOffset ReviewedAt);
