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
    }

    internal CashAdvanceSettlementRecord(
        CashAdvanceSettlementType settlementType,
        DateOnly settledAt,
        Money amount,
        string handledBy,
        string? note)
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

        Id = Guid.NewGuid();
        SettlementType = settlementType;
        SettledAt = settledAt;
        Amount = amount;
        HandledBy = handledBy.Trim();
        Note = note?.Trim() ?? string.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public CashAdvanceSettlementType SettlementType { get; private set; }

    public DateOnly SettledAt { get; private set; }

    public Money Amount { get; private set; }

    public string HandledBy { get; private set; }

    public string Note { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
