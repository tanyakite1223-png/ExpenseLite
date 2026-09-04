using ExpenseLite.Domain.ExpenseCategories;
using ExpenseLite.Domain.Shared;

namespace ExpenseLite.Application.ExpenseCategories;

public sealed class ExpenseCategoryAppService
{
    private readonly IExpenseCategoryRepository _categories;

    public ExpenseCategoryAppService(IExpenseCategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<ExpenseCategoryListPageDto> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.ListAsync(cancellationToken);

        var items = categories
            .OrderBy(x => x.Name)
            .Select(x => new ExpenseCategoryListItemDto(x.Id, x.Name, x.IsActive, x.CreatedAt))
            .ToList();

        return new ExpenseCategoryListPageDto(items);
    }

    public async Task<IReadOnlyList<string>> ListActiveNamesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.ListAsync(cancellationToken);

        return categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToList();
    }

    public async Task CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await _categories.ListAsync(cancellationToken);

        if (existing.Any(x => x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainRuleViolationException("已有同名類別，請改用其他名稱。");
        }

        var category = ExpenseCategory.Create(name);

        await _categories.AddAsync(category, cancellationToken);
        await _categories.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetOrThrowAsync(id, cancellationToken);
        category.Activate();
        await _categories.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetOrThrowAsync(id, cancellationToken);
        category.Deactivate();
        await _categories.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExpenseCategory> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            throw new DomainRuleViolationException("找不到指定的類別。");
        }

        return category;
    }
}
