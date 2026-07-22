using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record CreateExpenseReportCommand(
    string Title,
    string ApplicantName,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId);

public sealed record UpdateExpenseReportCommand(
    Guid Id,
    string Title,
    string ApplicantName,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId);

public sealed record AddExpenseDetailCommand(
    Guid ReportId,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    ExpenseReceiptType ReceiptType,
    string? InvoiceNumber,
    decimal Amount);

public sealed record UpdateExpenseDetailCommand(
    Guid ReportId,
    Guid DetailId,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    ExpenseReceiptType ReceiptType,
    string? InvoiceNumber,
    decimal Amount);

public sealed record ReviewExpenseReportCommand(
    Guid ReportId,
    string ReviewerName,
    string? Reason);
