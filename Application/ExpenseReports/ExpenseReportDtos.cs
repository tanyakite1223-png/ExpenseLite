using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Projects;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record ExpenseReportListItemDto(
    Guid Id,
    string Title,
    Guid? ApplicantUserId,
    string ApplicantName,
    ExpenseReportStatus Status,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    string? ProjectName,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId,
    decimal TotalAmount,
    DateTimeOffset CreatedAt);

public sealed record ExpenseReportListQuery(
    string? Keyword,
    ExpenseReportStatus? Status,
    ExpenseType? ExpenseType,
    ExpensePaymentMethod? PaymentMethod);

public sealed record ExpenseReportListPageDto(
    string Keyword,
    ExpenseReportStatus? Status,
    ExpenseType? ExpenseType,
    ExpensePaymentMethod? PaymentMethod,
    int TotalExpenseReportCount,
    IReadOnlyList<ExpenseReportListItemDto> Reports);

public sealed record ExpenseReportDetailDto(
    Guid Id,
    string Title,
    Guid? ApplicantUserId,
    string ApplicantName,
    ExpenseReportStatus Status,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    string? ProjectName,
    ProjectStatus? ProjectStatus,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId,
    ExpenseReportCashAdvanceDto? CashAdvance,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    IReadOnlyList<ExpenseDetailDto> Details,
    IReadOnlyList<ExpenseReviewRecordDto> ReviewRecords);

/// <summary>
/// 報銷單詳情頁要顯示「這張單對應哪一筆預支款」。
/// 只帶識別得出來的欄位，不帶核對金額——那是預支款自己的財務視角，見 CashAdvanceSettlementDetailDto。
/// 跨 aggregate 一律用 ID 參照，這裡是查詢時才組進 DTO，報銷單本身仍只存 CashAdvanceId。
/// </summary>
public sealed record ExpenseReportCashAdvanceDto(
    Guid Id,
    string PayeeName,
    string Purpose,
    DateOnly AdvancedAt);

public sealed record ExpenseDetailDto(
    Guid Id,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    ExpenseReceiptType ReceiptType,
    string InvoiceNumber,
    decimal Amount);

public sealed record ExpenseReviewRecordDto(
    Guid Id,
    ExpenseReviewAction Action,
    string ReviewerName,
    string Reason,
    DateTimeOffset ReviewedAt);
