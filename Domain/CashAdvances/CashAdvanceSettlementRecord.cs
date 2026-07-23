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
        string handledBy,
        string? note)
    {
        ValidateSettlementFields(settledAt, amount, handledBy);

        Id = Guid.NewGuid();
        SettlementType = settlementType;
        SettledAt = settledAt;
        Amount = amount;
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

    public string HandledBy { get; private set; }

    public string Note { get; private set; }

    public bool IsVoided { get; private set; }

    public string VoidedBy { get; private set; }

    public string VoidReason { get; private set; }

    public DateTimeOffset? VoidedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    internal void Update(
        DateOnly settledAt,
        Money amount,
        string handledBy,
        string? note)
    {
        if (IsVoided)
        {
            throw new DomainRuleViolationException("已標記為不採用的結清紀錄不可修改。");
        }

        ValidateSettlementFields(settledAt, amount, handledBy);

        SettledAt = settledAt;
        Amount = amount;
        HandledBy = handledBy.Trim();
        Note = note?.Trim() ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void MarkAsVoided(string voidedBy, string? voidReason)
    {
        if (IsVoided)
        {
            throw new DomainRuleViolationException("結清紀錄已經標記為不採用。");
        }

        if (string.IsNullOrWhiteSpace(voidedBy))
        {
            throw new DomainRuleViolationException("處理人不可空白。");
        }

        if (string.IsNullOrWhiteSpace(voidReason))
        {
            throw new DomainRuleViolationException("不採用原因不可空白。");
        }

        IsVoided = true;
        VoidedBy = voidedBy.Trim();
        VoidReason = voidReason.Trim();
        VoidedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateSettlementFields(DateOnly settledAt, Money amount, string handledBy)
    {
        if (settledAt == default)
        {
            throw new DomainRuleViolationException("結清日期不可空白。");
        }

        if (amount.Amount <= 0m)
        {
            throw new DomainRuleViolationException("結清金額必須大於 0。");
        }

        if (string.IsNullOrWhiteSpace(handledBy))
        {
            throw new DomainRuleViolationException("處理人不可空白。");
        }
    }
}
