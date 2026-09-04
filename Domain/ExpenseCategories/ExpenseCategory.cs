using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Domain.ExpenseCategories;

public sealed class ExpenseCategory
{
    private ExpenseCategory()
    {
        Name = string.Empty;
    }

    private ExpenseCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException("類別名稱不可空白。");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ExpenseCategory Create(string name) => new(name);

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainRuleViolationException("類別已啟用。");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainRuleViolationException("類別已停用。");
        }

        IsActive = false;
    }
}
