namespace ExpenseLite.Domain.Shared;

public sealed class DomainRuleViolationException : InvalidOperationException
{
    public DomainRuleViolationException(string message)
        : base(message)
    {
    }
}
