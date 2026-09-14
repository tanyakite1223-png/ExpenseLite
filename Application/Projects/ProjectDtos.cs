using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Shared;
using ExpenseLite.Domain.Projects;

namespace ExpenseLite.Application.Projects;

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    string CustomerName,
    ProjectStatus Status,
    int UnfinishedExpenseReportCount,
    DateTimeOffset CreatedAt);

public sealed record ProjectListPageDto(
    string Keyword,
    int TotalProjectCount,
    IReadOnlyList<ProjectListItemDto> Projects,
    PageInfo Paging);

public sealed record ProjectOptionDto(
    Guid Id,
    string Name,
    string CustomerName);

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string CustomerName,
    ProjectStatus Status,
    int UnfinishedExpenseReportCount,
    int TotalExpenseReportCount,
    /// <summary>相關報銷單中，狀態為 Approved 的總金額。獨立算好給 View，View 不該對分頁後的清單做加總。</summary>
    decimal ApprovedExpenseReportAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ExpenseReportListItemDto> ExpenseReports,
    PageInfo Paging);
