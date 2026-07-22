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
        string reviewerName,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(reviewerName))
        {
            throw new DomainRuleViolationException("審核人不可空白。");
        }

        if (action is ExpenseReviewAction.Returned or ExpenseReviewAction.Rejected &&
            string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("退回或拒絕時必須填寫原因。");
        }

        Id = Guid.NewGuid();
        Action = action;
        ReviewerName = reviewerName.Trim();
        Reason = reason?.Trim() ?? string.Empty;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public ExpenseReviewAction Action { get; private set; }

    public string ReviewerName { get; private set; }

    public string Reason { get; private set; }

    public DateTimeOffset ReviewedAt { get; private set; }
}
