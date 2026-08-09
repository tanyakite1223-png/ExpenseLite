using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Domain.ExpenseReports;

public sealed class ExpenseReviewRecord
{
    private ExpenseReviewRecord()
    {
        ReviewerName = string.Empty;
        Reason = string.Empty;
    }

    internal ExpenseReviewRecord(
        ExpenseReviewAction action,
        Guid reviewerUserId,
        string reviewerName,
        string? reason)
    {
        if (reviewerUserId == Guid.Empty)
        {
            throw new DomainRuleViolationException("審核人必須對應到一個登入帳號。");
        }

        if (string.IsNullOrWhiteSpace(reviewerName))
        {
            throw new DomainRuleViolationException("審核人不可空白。");
        }

        if (action is ExpenseReviewAction.Returned or ExpenseReviewAction.Rejected or ExpenseReviewAction.Voided &&
            string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("退回、拒絕或作廢時必須填寫原因。");
        }

        Id = Guid.NewGuid();
        Action = action;
        ReviewerUserId = reviewerUserId;
        ReviewerName = reviewerName.Trim();
        Reason = reason?.Trim() ?? string.Empty;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public ExpenseReviewAction Action { get; private set; }

    /// <summary>審核人的登入帳號 Id。舊資料沒有登入者可對應，所以是 nullable。</summary>
    public Guid? ReviewerUserId { get; private set; }

    /// <summary>審核當下的姓名快照。使用者日後改名時，這裡刻意不跟著變。</summary>
    public string ReviewerName { get; private set; }

    public string Reason { get; private set; }

    public DateTimeOffset ReviewedAt { get; private set; }
}
