using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record CreateExpenseReportCommand(
    string Title,
    string ApplicantName,
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
