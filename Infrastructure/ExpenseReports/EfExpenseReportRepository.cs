using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.ExpenseReports;

public sealed class EfExpenseReportRepository : IExpenseReportRepository
{
    private readonly ExpenseLiteDbContext _dbContext;

    public EfExpenseReportRepository(ExpenseLiteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ExpenseReport>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .AsNoTracking()
            .Include(x => x.Details)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.ExpenseReports.AddAsync(report, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
