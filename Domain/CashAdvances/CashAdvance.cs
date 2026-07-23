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

        ValidatePurposeAndAmount(purpose, amount);

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

    public void UpdateBasicInfo(string purpose, Money amount)
    {
        ValidatePurposeAndAmount(purpose, amount);

        if (Amount != amount && _settlementRecords.Any(x => !x.IsVoided))
        {
            throw new DomainRuleViolationException("已有已計入核對的結清紀錄時，不可修改預支金額。請先將相關結清紀錄標記為不採用後再修改。");
        }

        Purpose = purpose.Trim();
        Amount = amount;
    }

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

    public CashAdvanceSettlementRecord UpdateSettlementRecord(
        Guid settlementRecordId,
        DateOnly settledAt,
        Money amount,
        string handledBy,
        string? note)
    {
        var record = GetSettlementRecord(settlementRecordId);
        record.Update(settledAt, amount, handledBy, note);

        return record;
    }

    public void VoidSettlementRecord(
        Guid settlementRecordId,
        string voidedBy,
        string? voidReason)
    {
        var record = GetSettlementRecord(settlementRecordId);
        record.MarkAsVoided(voidedBy, voidReason);
    }

    private static void ValidatePurposeAndAmount(string purpose, Money amount)
    {
        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new DomainRuleViolationException("預支用途不可空白。");
        }

        if (amount.Amount <= 0m)
        {
            throw new DomainRuleViolationException("預支金額必須大於 0。");
        }
    }

    private CashAdvanceSettlementRecord GetSettlementRecord(Guid settlementRecordId)
    {
        var record = _settlementRecords.SingleOrDefault(x => x.Id == settlementRecordId);
        if (record is null)
        {
            throw new DomainRuleViolationException("找不到結清紀錄。");
        }

        return record;
    }
}
