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
    ExpensePaymentMethod? PaymentMethod,
    /// <summary>true 才會列出申請人軟刪的 Cancelled 單；預設隱藏。</summary>
    bool IncludeCancelled = false);

public sealed record ExpenseReportListPageDto(
    string Keyword,
    ExpenseReportStatus? Status,
    ExpenseType? ExpenseType,
    ExpensePaymentMethod? PaymentMethod,
    bool IncludeCancelled,
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
    /// <summary>
    /// 這張報銷單所綁的預支款上，仍被採用（未標記不採用）的結清紀錄筆數。
    /// 沒綁預支款的（一般支出 / 員工墊款 / 零用金）永遠為 0。
    /// Approved 詳情頁作廢按鈕的確認框、以及 Voided 詳情頁的持續提示，都會用到這個數字。
    /// </summary>
    int AdoptedSettlementRecordCount,
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

/// <summary>
/// 列印報表的一組分組小計，例如「按申請人分組」下的一列。
/// <paramref name="Label"/> 是顯示用（申請人姓名 / 支出類型中文 / 專案名 / …），
/// 沒有 key（例如非專案支出）時仍會給一個顯示字串（例如「非專案支出」）。
/// </summary>
public sealed record PrintReportGroupItem(string Label, int Count, decimal TotalAmount);

public sealed record PrintReportSummaryDto(
    int TotalCount,
    decimal TotalAmount,
    IReadOnlyList<PrintReportGroupItem> ByApplicant,
    IReadOnlyList<PrintReportGroupItem> ByExpenseType,
    IReadOnlyList<PrintReportGroupItem> ByPaymentMethod,
    IReadOnlyList<PrintReportGroupItem> ByProject);

/// <summary>
/// 列印頁的頁面 DTO。內容是「送審時間落在 [From, To] 區間內、狀態為 Approved」的報銷單。
/// 用「送審時間」而非「核准時間」的理由：對員工整理送出納最直觀（「這個月我報了什麼」）；
/// 對主管月度支出視角也對得上（本月送進來的錢就是本月要處理的量）。
/// 可見度照 <see cref="ExpenseReportVisibility"/>——員工只看自己、主管看全部。
/// </summary>
public sealed record PrintReportDto(
    DateOnly From,
    DateOnly To,
    PrintReportSummaryDto Summary,
    IReadOnlyList<ExpenseReportDetailDto> Reports);
