namespace ExpenseLite.Application.ExpenseReports;

public sealed record CreateExpenseReportCommand(string Title, string ApplicantName);

public sealed record AddExpenseDetailCommand(
    Guid ReportId,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    decimal Amount);
