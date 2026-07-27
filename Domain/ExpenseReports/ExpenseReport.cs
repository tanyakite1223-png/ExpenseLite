using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Domain.ExpenseReports;

public sealed class ExpenseReport
{
    private readonly List<ExpenseDetail> _details = [];
    private readonly List<ExpenseReviewRecord> _reviewRecords = [];

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
        Guid applicantUserId,
        string applicantName,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
    {
        EnsureApplicantIsValid(applicantUserId, applicantName);
        EnsureBasicInfoIsValid(
            title,
            expenseType,
            projectId,
            paymentMethod,
            cashAdvanceId);

        Id = Guid.NewGuid();
        Title = title.Trim();
        ApplicantUserId = applicantUserId;
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

    /// <summary>申請人的登入帳號 Id。舊資料沒有登入者可對應，所以是 nullable。</summary>
    public Guid? ApplicantUserId { get; private set; }

    /// <summary>申請當下的姓名快照。使用者日後改名時，這裡刻意不跟著變。</summary>
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

    public IReadOnlyCollection<ExpenseReviewRecord> ReviewRecords => _reviewRecords.AsReadOnly();

    public static ExpenseReport Create(
        string title,
        Guid applicantUserId,
        string applicantName,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
        => new(title, applicantUserId, applicantName, expenseType, projectId, paymentMethod, cashAdvanceId);

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

    public void UpdateDetail(
        Guid detailId,
        DateOnly expenseDate,
        string category,
        string description,
        ExpenseReceiptType receiptType,
        string? invoiceNumber,
        Money amount)
    {
        EnsureEditable("只有草稿或退回的報銷單可以修改明細。");

        var detail = _details.SingleOrDefault(x => x.Id == detailId);
        if (detail is null)
        {
            throw new DomainRuleViolationException("找不到要修改的報銷明細。");
        }

        detail.Update(
            expenseDate,
            category,
            description,
            receiptType,
            invoiceNumber,
            amount);
        RecalculateTotal();
    }

    /// <summary>申請人是建立報銷單的登入者，建立後就不再變動，所以不在修改範圍內。</summary>
    public void UpdateBasicInfo(
        string title,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
    {
        EnsureEditable("只有草稿或退回的報銷單可以修改。");
        EnsureBasicInfoIsValid(
            title,
            expenseType,
            projectId,
            paymentMethod,
            cashAdvanceId);

        Title = title.Trim();
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

    public void Return(Guid reviewerUserId, string reviewerName, string reason)
    {
        EnsureSubmitted("只有送審中的報銷單可以退回。");
        Status = ExpenseReportStatus.Returned;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Returned, reviewerUserId, reviewerName, reason));
    }

    public void Approve(Guid reviewerUserId, string reviewerName)
    {
        EnsureSubmitted("只有送審中的報銷單可以核准。");
        Status = ExpenseReportStatus.Approved;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Approved, reviewerUserId, reviewerName, null));
    }

    public void Reject(Guid reviewerUserId, string reviewerName, string reason)
    {
        EnsureSubmitted("只有送審中的報銷單可以拒絕。");
        Status = ExpenseReportStatus.Rejected;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Rejected, reviewerUserId, reviewerName, reason));
    }

    private static void EnsureApplicantIsValid(Guid applicantUserId, string applicantName)
    {
        if (applicantUserId == Guid.Empty)
        {
            throw new DomainRuleViolationException("申請人必須對應到一個登入帳號。");
        }

        if (string.IsNullOrWhiteSpace(applicantName))
        {
            throw new DomainRuleViolationException("申請人不可空白。");
        }
    }

    private static void EnsureBasicInfoIsValid(
        string title,
        ExpenseType expenseType,
        Guid? projectId,
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleViolationException("報銷單標題不可空白。");
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
