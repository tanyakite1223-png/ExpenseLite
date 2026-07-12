using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Domain.ExpenseReports;

public sealed class ExpenseDetail
{
    private ExpenseDetail()
    {
        Category = string.Empty;
        Description = string.Empty;
        Amount = Money.Zero;
    }

    internal ExpenseDetail(DateOnly expenseDate, string category, string description, Money amount)
    {
        if (expenseDate == default)
        {
            throw new DomainRuleViolationException("明細日期不可空白。");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new DomainRuleViolationException("明細類別不可空白。");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainRuleViolationException("明細說明不可空白。");
        }

        Id = Guid.NewGuid();
        ExpenseDate = expenseDate;
        Category = category.Trim();
        Description = description.Trim();
        Amount = amount;
    }

    public Guid Id { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    public string Category { get; private set; }

    public string Description { get; private set; }

    public Money Amount { get; private set; }
}
