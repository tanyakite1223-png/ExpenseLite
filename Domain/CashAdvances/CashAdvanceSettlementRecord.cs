using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Domain.CashAdvances;

public sealed class CashAdvanceSettlementRecord
{
    private CashAdvanceSettlementRecord()
    {
        Amount = Money.Zero;
        HandledBy = string.Empty;
        Note = string.Empty;
        VoidedBy = string.Empty;
        VoidReason = string.Empty;
    }

    internal CashAdvanceSettlementRecord(
        CashAdvanceSettlementType settlementType,
        DateOnly settledAt,
        Money amount,
        Guid handledByUserId,
        string handledBy,
        string? note)
    {
        ValidateSettlementFields(settledAt, amount);
        ValidateHandler(handledByUserId, handledBy);

        Id = Guid.NewGuid();
        SettlementType = settlementType;
        SettledAt = settledAt;
        Amount = amount;
        HandledByUserId = handledByUserId;
        HandledBy = handledBy.Trim();
        Note = note?.Trim() ?? string.Empty;
        VoidedBy = string.Empty;
        VoidReason = string.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public CashAdvanceSettlementType SettlementType { get; private set; }

    public DateOnly SettledAt { get; private set; }

    public Money Amount { get; private set; }

    /// <summary>登記這筆結清的登入帳號 Id。舊資料沒有登入者可對應，所以是 nullable。</summary>
    public Guid? HandledByUserId { get; private set; }

    /// <summary>登記當下的姓名快照。使用者日後改名時，這裡刻意不跟著變。</summary>
    public string HandledBy { get; private set; }

    public string Note { get; private set; }

    public bool IsVoided { get; private set; }

    /// <summary>標記不採用的登入帳號 Id。尚未不採用或舊資料時為 null。</summary>
    public Guid? VoidedByUserId { get; private set; }

    public string VoidedBy { get; private set; }

    public string VoidReason { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>處理人是當初登記這筆結清的人，之後別人來更正內容也不會改掉，避免歷史失真。</summary>
    internal void Update(
        DateOnly settledAt,
        Money amount,
        string? note)
    {
        if (IsVoided)
        {
            throw new DomainRuleViolationException("已標記為不採用的結清紀錄不可修改。");
        }

        ValidateSettlementFields(settledAt, amount);

        SettledAt = settledAt;
        Amount = amount;
        Note = note?.Trim() ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void MarkAsVoided(Guid voidedByUserId, string voidedBy, string? voidReason)
    {
        if (IsVoided)
        {
            throw new DomainRuleViolationException("結清紀錄已經標記為不採用。");
        }

        ValidateHandler(voidedByUserId, voidedBy);

        if (string.IsNullOrWhiteSpace(voidReason))
        {
            throw new DomainRuleViolationException("不採用原因不可空白。");
        }

        IsVoided = true;
        VoidedByUserId = voidedByUserId;
        VoidedBy = voidedBy.Trim();
        VoidReason = voidReason.Trim();
        VoidedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateSettlementFields(DateOnly settledAt, Money amount)
    {
        if (settledAt == default)
        {
            throw new DomainRuleViolationException("結清日期不可空白。");
        }

        if (amount.Amount <= 0m)
        {
            throw new DomainRuleViolationException("結清金額必須大於 0。");
        }
    }

    private static void ValidateHandler(Guid userId, string name)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainRuleViolationException("處理人必須對應到一個登入帳號。");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException("處理人不可空白。");
        }
    }
}
