namespace ExpenseLite.Application.CashAdvances;

public sealed record CreateCashAdvanceCommand(
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount);
