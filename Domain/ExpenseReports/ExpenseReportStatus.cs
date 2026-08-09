namespace ExpenseLite.Domain.ExpenseReports;

public enum ExpenseReportStatus
{
    Draft = 0,
    Submitted = 1,
    Returned = 2,
    Approved = 3,
    Rejected = 4,

    /// <summary>主管作廢：已核准後才發現要撤回。不可撤銷，要救就重打一張新單。</summary>
    Voided = 5,

    /// <summary>申請人軟刪：退回單自認不成立而取消。可再由申請人復活回退回狀態。</summary>
    Cancelled = 6
}
