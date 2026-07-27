using ExpenseLite.Application.Identity;

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
    CurrentUser HandledBy,
    string? Note);

/// <summary>修改結清紀錄不帶處理人：處理人固定是當初登記的人，不會因為別人來更正而換掉。</summary>
public sealed record UpdateCashAdvanceSettlementCommand(
    Guid CashAdvanceId,
    Guid SettlementRecordId,
    DateOnly SettledAt,
    decimal Amount,
    string? Note);

public sealed record VoidCashAdvanceSettlementCommand(
    Guid CashAdvanceId,
    Guid SettlementRecordId,
    CurrentUser VoidedBy,
    string? VoidReason);
