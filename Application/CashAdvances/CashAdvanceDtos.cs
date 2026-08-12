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
    Guid? PayeeUserId,
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
    bool HasInProgressReports,
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
    Guid? PayeeUserId,
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
    bool HasInProgressReports,
    bool HasRelatedReports,
    /// <summary>
    /// 這筆預支款上有幾張已被主管作廢的報銷單。核對數字（差額、應結清）是即時算的，
    /// 作廢單一離開 Approved 就自動不算，但已計入的結清紀錄不會自動動——主管要手動處理。
    /// &gt; 0 時預支款詳情頁會顯示提示：「已作廢報銷單相關的結清紀錄可能與現況不符」。
    /// </summary>
    int VoidedRelatedReportCount,
    bool CanEdit,
    bool CanEditAmount,
    CashAdvanceReconciliationStatus ReconciliationStatus,
    IReadOnlyList<CashAdvanceSettlementRecordDto> SettlementRecords);

public sealed record CashAdvanceSettlementRecordDto(
    Guid Id,
    CashAdvanceSettlementType SettlementType,
    DateOnly SettledAt,
    decimal Amount,
    string HandledBy,
    string Note,
    bool IsVoided,
    string VoidedBy,
    string VoidReason,
    DateTimeOffset? VoidedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset CreatedAt,
    bool CanEdit,
    bool CanVoid);
