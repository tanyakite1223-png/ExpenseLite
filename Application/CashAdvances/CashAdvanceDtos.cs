using ExpenseLite.Domain.CashAdvances;

namespace ExpenseLite.Application.CashAdvances;

public enum CashAdvanceReconciliationStatus
{
    Unreimbursed = 0,
    Settled = 1,
    CompanyNeedsToPay = 2,
    EmployeeNeedsToReturn = 3
}

public sealed record CashAdvanceListItemDto(
    Guid Id,
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount,
    decimal ApprovedReimbursedAmount,
    decimal Difference,
    decimal RequiredSettlementAmount,
    decimal SettledAmount,
    decimal RemainingSettlementAmount,
    CashAdvanceSettlementType? RequiredSettlementType,
    bool IsSettled,
    CashAdvanceReconciliationStatus ReconciliationStatus);

public sealed record CashAdvanceListQuery(
    string? Keyword,
    CashAdvanceReconciliationStatus? ReconciliationStatus);

public sealed record CashAdvanceListPageDto(
    string Keyword,
    CashAdvanceReconciliationStatus? ReconciliationStatus,
    int TotalCashAdvanceCount,
    IReadOnlyList<CashAdvanceListItemDto> CashAdvances);

public sealed record CashAdvanceOptionDto(
    Guid Id,
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount,
    decimal ApprovedReimbursedAmount);

public sealed record CashAdvanceSettlementDetailDto(
    Guid Id,
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt,
    decimal Amount,
    decimal ApprovedReimbursedAmount,
    decimal Difference,
    decimal RequiredSettlementAmount,
    decimal SettledAmount,
    decimal RemainingSettlementAmount,
    CashAdvanceSettlementType? RequiredSettlementType,
    CashAdvanceReconciliationStatus ReconciliationStatus,
    IReadOnlyList<CashAdvanceSettlementRecordDto> SettlementRecords);

public sealed record CashAdvanceSettlementRecordDto(
    Guid Id,
    CashAdvanceSettlementType SettlementType,
    DateOnly SettledAt,
    decimal Amount,
    string HandledBy,
    string Note,
    DateTimeOffset CreatedAt);
