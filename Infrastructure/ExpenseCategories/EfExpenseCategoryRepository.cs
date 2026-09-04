using ExpenseLite.Application.ExpenseCategories;
using ExpenseLite.Domain.ExpenseCategories;
using ExpenseLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.ExpenseCategories;

public sealed class EfExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly ExpenseLiteDbContext _db;

    public EfExpenseCategoryRepository(ExpenseLiteDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ExpenseCategory>> ListAsync(CancellationToken cancellationToken = default)
        => await _db.ExpenseCategories.ToListAsync(cancellationToken);

    public async Task<ExpenseCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.ExpenseCategories.FindAsync([id], cancellationToken);

    public async Task AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
        => await _db.ExpenseCategories.AddAsync(category, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
