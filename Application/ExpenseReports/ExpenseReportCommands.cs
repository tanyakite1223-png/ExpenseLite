using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record CreateExpenseReportCommand(
    string Title,
    CurrentUser Applicant,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId);

public sealed record UpdateExpenseReportCommand(
    Guid Id,
    string Title,
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
    CurrentUser Reviewer,
    string? Reason);
