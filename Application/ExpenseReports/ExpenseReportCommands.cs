using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public sealed record CreateExpenseReportCommand(
    string Title,
    CurrentUser Applicant,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId);

public sealed record UpdateExpenseReportCommand(
    Guid Id,
    CurrentUser Editor,
    string Title,
    ExpenseType ExpenseType,
    Guid? ProjectId,
    ExpensePaymentMethod PaymentMethod,
    Guid? CashAdvanceId);

public sealed record AddExpenseDetailCommand(
    Guid ReportId,
    CurrentUser Editor,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    ExpenseReceiptType ReceiptType,
    string? InvoiceNumber,
    decimal Amount);

public sealed record UpdateExpenseDetailCommand(
    Guid ReportId,
    Guid DetailId,
    CurrentUser Editor,
    DateOnly ExpenseDate,
    string Category,
    string Description,
    ExpenseReceiptType ReceiptType,
    string? InvoiceNumber,
    decimal Amount);

public sealed record RemoveExpenseDetailCommand(
    Guid ReportId,
    Guid DetailId,
    CurrentUser Editor);

public sealed record SubmitExpenseReportCommand(
    Guid ReportId,
    CurrentUser Editor);

public sealed record ReviewExpenseReportCommand(
    Guid ReportId,
    CurrentUser Reviewer,
    string? Reason);

/// <summary>申請人硬刪自己的草稿。草稿還沒進過主管視野，也沒綁結清紀錄，刪掉真的沒有後遺症。</summary>
public sealed record DeleteExpenseReportCommand(
    Guid ReportId,
    CurrentUser Applicant);

/// <summary>申請人取消退回的報銷單（軟刪）。單子留在系統中，可再由申請人復活。</summary>
public sealed record CancelExpenseReportCommand(
    Guid ReportId,
    CurrentUser Applicant);

/// <summary>申請人復活軟刪的報銷單，回到退回狀態。</summary>
public sealed record RestoreExpenseReportCommand(
    Guid ReportId,
    CurrentUser Applicant);

/// <summary>主管作廢已核准的報銷單。不可撤銷；結清紀錄不會自己動，由主管手動處理。</summary>
public sealed record VoidExpenseReportCommand(
    Guid ReportId,
    CurrentUser Reviewer,
    string Reason);
