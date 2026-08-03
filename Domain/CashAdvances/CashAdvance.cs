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

    private CashAdvance(
        Guid payeeUserId,
        string payeeName,
        CashAdvanceUsage usage,
        string purpose,
        DateOnly advancedAt,
        Money amount)
    {
        EnsurePayeeIsValid(payeeUserId, payeeName);
        ValidatePurposeAndAmount(purpose, amount);

        Id = Guid.NewGuid();
        PayeeUserId = payeeUserId;
        PayeeName = payeeName.Trim();
        Usage = usage;
        Purpose = purpose.Trim();
        AdvancedAt = advancedAt;
        Amount = amount;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>領款人的登入帳號 Id。舊資料的領款人只是手打字串、沒有帳號可對應，所以是 nullable。</summary>
    public Guid? PayeeUserId { get; private set; }

    /// <summary>領款當下的姓名快照。使用者日後改名時，這裡刻意不跟著變。</summary>
    public string PayeeName { get; private set; }

    /// <summary>個人預支還是零用金（共用），決定誰的報銷單可以引用這筆錢。</summary>
    public CashAdvanceUsage Usage { get; private set; }

    public string Purpose { get; private set; }

    public DateOnly AdvancedAt { get; private set; }

    public Money Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<CashAdvanceSettlementRecord> SettlementRecords => _settlementRecords.AsReadOnly();

    public static CashAdvance Create(
        Guid payeeUserId,
        string payeeName,
        CashAdvanceUsage usage,
        string purpose,
        DateOnly advancedAt,
        Money amount)
        => new(payeeUserId, payeeName, usage, purpose, advancedAt, amount);

    /// <summary>
    /// 領款人與用途類型建立後不可修改，所以不在修改範圍內：
    /// 換掉領款人等於改寫「這筆錢當初給了誰」，換掉用途類型會讓已經引用它的報銷單變成不該存在。
    /// </summary>
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
        Guid handledByUserId,
        string handledByName,
        string? note)
    {
        var record = new CashAdvanceSettlementRecord(
            settlementType,
            settledAt,
            amount,
            handledByUserId,
            handledByName,
            note);
        _settlementRecords.Add(record);

        return record;
    }

    public CashAdvanceSettlementRecord UpdateSettlementRecord(
        Guid settlementRecordId,
        DateOnly settledAt,
        Money amount,
        string? note)
    {
        var record = GetSettlementRecord(settlementRecordId);
        record.Update(settledAt, amount, note);

        return record;
    }

    public void VoidSettlementRecord(
        Guid settlementRecordId,
        Guid voidedByUserId,
        string voidedByName,
        string? voidReason)
    {
        var record = GetSettlementRecord(settlementRecordId);
        record.MarkAsVoided(voidedByUserId, voidedByName, voidReason);
    }

    private static void EnsurePayeeIsValid(Guid payeeUserId, string payeeName)
    {
        if (payeeUserId == Guid.Empty)
        {
            throw new DomainRuleViolationException("領款人必須對應到一個登入帳號。");
        }

        if (string.IsNullOrWhiteSpace(payeeName))
        {
            throw new DomainRuleViolationException("領款人不可空白。");
        }
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
