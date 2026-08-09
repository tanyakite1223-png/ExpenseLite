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
        EnsureReviewerIsNotApplicant(reviewerUserId);
        Status = ExpenseReportStatus.Returned;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Returned, reviewerUserId, reviewerName, reason));
    }

    public void Approve(Guid reviewerUserId, string reviewerName)
    {
        EnsureSubmitted("只有送審中的報銷單可以核准。");
        EnsureReviewerIsNotApplicant(reviewerUserId);
        Status = ExpenseReportStatus.Approved;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Approved, reviewerUserId, reviewerName, null));
    }

    public void Reject(Guid reviewerUserId, string reviewerName, string reason)
    {
        EnsureSubmitted("只有送審中的報銷單可以拒絕。");
        EnsureReviewerIsNotApplicant(reviewerUserId);
        Status = ExpenseReportStatus.Rejected;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Rejected, reviewerUserId, reviewerName, reason));
    }

    /// <summary>
    /// 主管作廢已核准的報銷單。作廢不可撤銷，要救就重打一張新單。
    ///
    /// 已核准報銷金額是即時算的：作廢當下這張單就不再算進去，但已計入的結清紀錄
    /// 不會自己動——那筆錢現實中真的動過，怎麼補救留給主管手動判斷。
    /// 詳情頁與相關預支款詳情頁會顯示提示，避免遺忘。
    /// </summary>
    public void Void(Guid reviewerUserId, string reviewerName, string reason)
    {
        if (Status != ExpenseReportStatus.Approved)
        {
            throw new DomainRuleViolationException("只有已核准的報銷單可以作廢。");
        }
        EnsureReviewerIsNotApplicant(reviewerUserId);
        Status = ExpenseReportStatus.Voided;
        _reviewRecords.Add(new ExpenseReviewRecord(ExpenseReviewAction.Voided, reviewerUserId, reviewerName, reason));
    }

    /// <summary>
    /// 申請人取消退回的報銷單（軟刪）。單子仍留在系統中，主管與申請人都查得到，
    /// 可再用 <see cref="Restore"/> 復活回退回狀態。
    /// 「只有申請人可以」是資源授權，在 Application Service 擋，不在這裡。
    /// </summary>
    public void Cancel()
    {
        if (Status != ExpenseReportStatus.Returned)
        {
            throw new DomainRuleViolationException("只有退回的報銷單可以取消。");
        }
        Status = ExpenseReportStatus.Cancelled;
    }

    /// <summary>
    /// 申請人復活軟刪的報銷單，回到退回狀態，之後可再修改重送。
    /// 「軟刪的單本身不可修改，要改先復活」由 <see cref="EnsureEditable"/> 保障——
    /// Cancelled 不在可修改的 whitelist 裡。
    /// </summary>
    public void Restore()
    {
        if (Status != ExpenseReportStatus.Cancelled)
        {
            throw new DomainRuleViolationException("只有已取消的報銷單可以復活。");
        }
        Status = ExpenseReportStatus.Returned;
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

        // 只有個人預支要綁預支款；員工墊款與零用金支付的錢都不是預先撥給某個人的，所以都不綁。
        if (paymentMethod == ExpensePaymentMethod.PersonalAdvance && cashAdvanceId is null)
        {
            throw new DomainRuleViolationException("個人預支報銷單必須選擇對應的預支款。");
        }

        if (paymentMethod != ExpensePaymentMethod.PersonalAdvance && cashAdvanceId is not null)
        {
            throw new DomainRuleViolationException("只有個人預支報銷單可以連到預支款。");
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

    // 舊資料的 ApplicantUserId 可能是 null（登入功能之前建的),沒得比對就當作沒有這條限制。
    private void EnsureReviewerIsNotApplicant(Guid reviewerUserId)
    {
        if (ApplicantUserId.HasValue && ApplicantUserId.Value == reviewerUserId)
        {
            throw new DomainRuleViolationException("不能審核自己送出的報銷單。");
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = _details
            .Select(x => x.Amount)
            .Aggregate(Money.Zero, (current, next) => current.Add(next));
    }
}
