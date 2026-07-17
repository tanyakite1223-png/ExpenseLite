using ExpenseLite.Domain.CashAdvances;

namespace ExpenseLite.Application.CashAdvances;

public interface ICashAdvanceRepository
{
    Task<IReadOnlyList<CashAdvance>> ListAsync(CancellationToken cancellationToken = default);

    Task<CashAdvance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(CashAdvance cashAdvance, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
