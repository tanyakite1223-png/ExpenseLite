using ExpenseLite.Domain.ExpenseCategories;

namespace ExpenseLite.Application.ExpenseCategories;

public interface IExpenseCategoryRepository
{
    Task<IReadOnlyList<ExpenseCategory>> ListAsync(CancellationToken cancellationToken = default);

    Task<ExpenseCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
