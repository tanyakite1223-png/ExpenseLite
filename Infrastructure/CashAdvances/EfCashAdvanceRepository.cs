using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseLite.Infrastructure.CashAdvances;

public sealed class EfCashAdvanceRepository : ICashAdvanceRepository
{
    private readonly ExpenseLiteDbContext _dbContext;

    public EfCashAdvanceRepository(ExpenseLiteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CashAdvance>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CashAdvances
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<CashAdvance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CashAdvances
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(CashAdvance cashAdvance, CancellationToken cancellationToken = default)
    {
        await _dbContext.CashAdvances.AddAsync(cashAdvance, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
