namespace ExpenseLite.Application.CashAdvances;

public sealed record CreateCashAdvanceCommand(
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount);

public sealed record UpdateCashAdvanceCommand(
    Guid CashAdvanceId,
    string Purpose,
    decimal Amount);

public sealed record RecordCashAdvanceSettlementCommand(
    Guid CashAdvanceId,
    DateOnly SettledAt,
    decimal Amount,
    string HandledBy,
    string? Note);

public sealed record UpdateCashAdvanceSettlementCommand(
    Guid CashAdvanceId,
    Guid SettlementRecordId,
    DateOnly SettledAt,
    decimal Amount,
    string HandledBy,
    string? Note);

public sealed record VoidCashAdvanceSettlementCommand(
    Guid CashAdvanceId,
    Guid SettlementRecordId,
    string VoidedBy,
    string? VoidReason);
