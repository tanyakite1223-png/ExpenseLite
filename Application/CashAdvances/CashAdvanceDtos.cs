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
