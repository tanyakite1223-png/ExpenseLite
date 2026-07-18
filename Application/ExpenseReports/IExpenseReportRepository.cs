using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public interface IExpenseReportRepository
{
    Task<IReadOnlyList<ExpenseReport>> ListAsync(CancellationToken cancellationToken = default);

    Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> CountUnfinishedProjectReportsAsync(CancellationToken cancellationToken = default);

    Task<bool> HasUnfinishedProjectReportsAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
