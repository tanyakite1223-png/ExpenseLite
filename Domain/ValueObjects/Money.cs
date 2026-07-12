using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Domain.ValueObjects;

public sealed class Money : IEquatable<Money>
{
    private Money()
    {
    }

    public Money(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainRuleViolationException("金額不可為負數。");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainRuleViolationException("金額最多只能有兩位小數。");
        }

        Amount = amount;
    }

    public decimal Amount { get; private set; }

    public static Money Zero => new(0m);

    public static Money From(decimal amount) => new(amount);

    public Money Add(Money other) => new(Amount + other.Amount);

    public bool Equals(Money? other) => other is not null && Amount == other.Amount;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => Amount.GetHashCode();

    public override string ToString() => Amount.ToString("N2");

    public static bool operator ==(Money? left, Money? right) => Equals(left, right);

    public static bool operator !=(Money? left, Money? right) => !Equals(left, right);
}
