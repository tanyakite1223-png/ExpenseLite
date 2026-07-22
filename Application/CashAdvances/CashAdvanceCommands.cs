namespace ExpenseLite.Application.CashAdvances;

public sealed record CreateCashAdvanceCommand(
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount);

public sealed record RecordCashAdvanceSettlementCommand(
    Guid CashAdvanceId,
    DateOnly SettledAt,
    decimal Amount,
    string HandledBy,
    string? Note);
