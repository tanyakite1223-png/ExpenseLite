using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Domain.Projects;

public sealed class Project
{
    private Project()
    {
        Name = string.Empty;
        CustomerName = string.Empty;
        Status = ProjectStatus.Active;
    }

    private Project(string name, string customerName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException("專案名稱不可空白。");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new DomainRuleViolationException("客戶名稱不可空白。");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        CustomerName = customerName.Trim();
        Status = ProjectStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string CustomerName { get; private set; }

    public ProjectStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Project Create(string name, string customerName)
        => new(name, customerName);
}
