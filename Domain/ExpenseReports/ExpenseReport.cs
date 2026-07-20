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
        ExpenseType = ExpenseType.General;
        PaymentMethod = ExpensePaymentMethod.EmployeePaid;
        TotalAmount = Money.Zero;
    }

    private ExpenseReport(
        string title,
        string applicantName,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
    {
        EnsureBasicInfoIsValid(
            title,
            applicantName,
            expenseType,
            projectId,
            paymentMethod,
            cashAdvanceId);

        Id = Guid.NewGuid();
        Title = title.Trim();
        ApplicantName = applicantName.Trim();
        ExpenseType = expenseType;
        ProjectId = projectId;
        PaymentMethod = paymentMethod;
        CashAdvanceId = cashAdvanceId;
        Status = ExpenseReportStatus.Draft;
        TotalAmount = Money.Zero;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string ApplicantName { get; private set; }

    public ExpenseReportStatus Status { get; private set; }

    public ExpenseType ExpenseType { get; private set; }

    public Guid? ProjectId { get; private set; }

    public ExpensePaymentMethod PaymentMethod { get; private set; }

    public Guid? CashAdvanceId { get; private set; }

    public Money TotalAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    public IReadOnlyCollection<ExpenseDetail> Details => _details.AsReadOnly();

    public static ExpenseReport Create(
        string title,
        string applicantName,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
        => new(title, applicantName, expenseType, projectId, paymentMethod, cashAdvanceId);

    public ExpenseDetail AddDetail(
        DateOnly expenseDate,
        string category,
        string description,
        ExpenseReceiptType receiptType,
        string? invoiceNumber,
        Money amount)
    {
        EnsureEditable("報銷單送審後不可修改明細。");

        var detail = new ExpenseDetail(
            expenseDate,
            category,
            description,
            receiptType,
            invoiceNumber,
            amount);
        _details.Add(detail);
        RecalculateTotal();

        return detail;
    }

    public void RemoveDetail(Guid detailId)
    {
        EnsureEditable("報銷單送審後不可修改明細。");

        var detail = _details.SingleOrDefault(x => x.Id == detailId);
        if (detail is null)
        {
            throw new DomainRuleViolationException("找不到要移除的報銷明細。");
        }

        _details.Remove(detail);
        RecalculateTotal();
    }

    public void UpdateBasicInfo(
        string title,
        string applicantName,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
    {
        EnsureEditable("只有草稿或退回的報銷單可以修改。");
        EnsureBasicInfoIsValid(
            title,
            applicantName,
            expenseType,
            projectId,
            paymentMethod,
            cashAdvanceId);

        Title = title.Trim();
        ApplicantName = applicantName.Trim();
        ExpenseType = expenseType;
        ProjectId = projectId;
        PaymentMethod = paymentMethod;
        CashAdvanceId = cashAdvanceId;
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

    private static void EnsureBasicInfoIsValid(
        string title,
        string applicantName,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleViolationException("報銷單標題不可空白。");
        }

        if (string.IsNullOrWhiteSpace(applicantName))
        {
            throw new DomainRuleViolationException("申請人不可空白。");
        }

        if (expenseType == ExpenseType.Project && projectId is null)
        {
            throw new DomainRuleViolationException("專案支出報銷單必須選擇專案。");
        }

        if (expenseType == ExpenseType.General && projectId is not null)
        {
            throw new DomainRuleViolationException("一般支出報銷單不可連到專案。");
        }

        if (paymentMethod == ExpensePaymentMethod.CashAdvance && cashAdvanceId is null)
        {
            throw new DomainRuleViolationException("預支費用報銷單必須選擇對應的預支款。");
        }

        if (paymentMethod == ExpensePaymentMethod.EmployeePaid && cashAdvanceId is not null)
        {
            throw new DomainRuleViolationException("員工墊款報銷單不可連到預支款。");
        }
    }

    private void EnsureEditable(string message)
    {
        if (Status is not ExpenseReportStatus.Draft and not ExpenseReportStatus.Returned)
        {
            throw new DomainRuleViolationException(message);
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
