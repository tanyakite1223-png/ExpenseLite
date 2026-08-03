using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.CashAdvances;

namespace ExpenseLite.Application.CashAdvances;

/// <summary>
/// 只帶領款人的 UserId，不帶姓名——姓名快照由 Application 自己查 <see cref="IUserDirectory"/> 取得，
/// 不接受 Web 層送進來的字串，避免有人竄改表單讓紀錄上的名字對不上帳號。
/// </summary>
public sealed record CreateCashAdvanceCommand(
    Guid PayeeUserId,
    CashAdvanceUsage Usage,
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
