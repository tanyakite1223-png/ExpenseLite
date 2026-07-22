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

    public async Task<IReadOnlyList<ExpenseReport>> ListByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.ExpenseType == ExpenseType.Project && x.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .Include(x => x.Details)
            .Include(x => x.ReviewRecords)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Dictionary<Guid, int>> CountUnfinishedProjectReportsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .AsNoTracking()
            .Where(x =>
                x.ExpenseType == ExpenseType.Project &&
                x.ProjectId != null &&
                x.Status != ExpenseReportStatus.Approved &&
                x.Status != ExpenseReportStatus.Rejected)
            .GroupBy(x => x.ProjectId!.Value)
            .Select(x => new { ProjectId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, cancellationToken);
    }

    public async Task<bool> HasUnfinishedProjectReportsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .AsNoTracking()
            .AnyAsync(x =>
                x.ExpenseType == ExpenseType.Project &&
                x.ProjectId == projectId &&
                x.Status != ExpenseReportStatus.Approved &&
                x.Status != ExpenseReportStatus.Rejected,
                cancellationToken);
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
