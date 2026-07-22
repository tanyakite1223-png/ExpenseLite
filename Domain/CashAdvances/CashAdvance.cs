using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Domain.CashAdvances;

public sealed class CashAdvance
{
    private readonly List<CashAdvanceSettlementRecord> _settlementRecords = [];

    private CashAdvance()
    {
        PayeeName = string.Empty;
        Purpose = string.Empty;
        Amount = Money.Zero;
    }

    private CashAdvance(string payeeName, string purpose, DateOnly advancedAt, Money amount)
    {
        if (string.IsNullOrWhiteSpace(payeeName))
        {
            throw new DomainRuleViolationException("領款人不可空白。");
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new DomainRuleViolationException("預支用途不可空白。");
        }

        Id = Guid.NewGuid();
        PayeeName = payeeName.Trim();
        Purpose = purpose.Trim();
        AdvancedAt = advancedAt;
        Amount = amount;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string PayeeName { get; private set; }

    public string Purpose { get; private set; }

    public DateOnly AdvancedAt { get; private set; }

    public Money Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<CashAdvanceSettlementRecord> SettlementRecords => _settlementRecords.AsReadOnly();

    public static CashAdvance Create(string payeeName, string purpose, DateOnly advancedAt, Money amount)
        => new(payeeName, purpose, advancedAt, amount);

    public CashAdvanceSettlementRecord AddSettlementRecord(
        CashAdvanceSettlementType settlementType,
        DateOnly settledAt,
        Money amount,
        string handledBy,
        string? note)
    {
        var record = new CashAdvanceSettlementRecord(
            settlementType,
            settledAt,
            amount,
            handledBy,
            note);
        _settlementRecords.Add(record);

        return record;
    }
}
