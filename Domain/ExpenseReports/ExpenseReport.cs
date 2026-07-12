using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Domain.ExpenseReports;

public sealed class ExpenseReport
{
    private readonly List<ExpenseDetail> _details = [];

    private ExpenseReport()
    {
        Title = string.Empty;
        ApplicantName = string.Empty;
        TotalAmount = Money.Zero;
    }

    private ExpenseReport(string title, string applicantName)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleViolationException("報銷單標題不可空白。");
        }

        if (string.IsNullOrWhiteSpace(applicantName))
        {
            throw new DomainRuleViolationException("申請人不可空白。");
        }

        Id = Guid.NewGuid();
        Title = title.Trim();
        ApplicantName = applicantName.Trim();
        Status = ExpenseReportStatus.Draft;
        TotalAmount = Money.Zero;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string ApplicantName { get; private set; }

    public ExpenseReportStatus Status { get; private set; }

    public Money TotalAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public IReadOnlyCollection<ExpenseDetail> Details => _details.AsReadOnly();

    public static ExpenseReport Create(string title, string applicantName)
        => new(title, applicantName);

    public ExpenseDetail AddDetail(DateOnly expenseDate, string category, string description, Money amount)
    {
        EnsureEditable();

        var detail = new ExpenseDetail(expenseDate, category, description, amount);
        _details.Add(detail);
        RecalculateTotal();

        return detail;
    }

    public void RemoveDetail(Guid detailId)
    {
        EnsureEditable();

        var detail = _details.SingleOrDefault(x => x.Id == detailId);
        if (detail is null)
        {
            throw new DomainRuleViolationException("找不到要移除的報銷明細。");
        }

        _details.Remove(detail);
        RecalculateTotal();
    }

    public void Submit()
    {
        if (Status is not ExpenseReportStatus.Draft and not ExpenseReportStatus.Returned)
        {
            throw new DomainRuleViolationException("只有草稿或退回的報銷單可以送審。");
        }

        if (_details.Count == 0)
        {
            throw new DomainRuleViolationException("報銷單至少要有一筆明細才能送審。");
        }

        Status = ExpenseReportStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
    }

    public void Return()
    {
        EnsureSubmitted("只有送審中的報銷單可以退回。");
        Status = ExpenseReportStatus.Returned;
    }

    public void Approve()
    {
        EnsureSubmitted("只有送審中的報銷單可以核准。");
        Status = ExpenseReportStatus.Approved;
    }

    public void Reject()
    {
        EnsureSubmitted("只有送審中的報銷單可以拒絕。");
        Status = ExpenseReportStatus.Rejected;
    }

    private void EnsureEditable()
    {
        if (Status is not ExpenseReportStatus.Draft and not ExpenseReportStatus.Returned)
        {
            throw new DomainRuleViolationException("報銷單送審後不可修改明細。");
        }
    }

    private void EnsureSubmitted(string message)
    {
        if (Status != ExpenseReportStatus.Submitted)
        {
            throw new DomainRuleViolationException(message);
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = _details
            .Select(x => x.Amount)
            .Aggregate(Money.Zero, (current, next) => current.Add(next));
    }
}
