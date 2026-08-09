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
        Guid? applicantUserId = null,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExpenseReports
            .AsNoTracking()
            .Where(x =>
                x.ExpenseType == ExpenseType.Project &&
                x.ProjectId != null &&
                IsUnfinished(x.Status) &&
                (applicantUserId == null || x.ApplicantUserId == applicantUserId))
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
                IsUnfinished(x.Status),
                cancellationToken);
    }

    /// <summary>
    /// 「未完成」= 還在流程裡（草稿、送審中、被退回）。
    /// 用 whitelist 而不是 blacklist：新增狀態時預設「不算未完成」比較安全，
    /// 例如 Voided（作廢）與 Cancelled（申請人軟刪）都不再流動、不該算未完成。
    /// </summary>
    private static bool IsUnfinished(ExpenseReportStatus status)
        => status == ExpenseReportStatus.Draft ||
           status == ExpenseReportStatus.Submitted ||
           status == ExpenseReportStatus.Returned;

    public async Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.ExpenseReports.AddAsync(report, cancellationToken);
    }

    public Task DeleteAsync(ExpenseReport report, CancellationToken cancellationToken = default)
    {
        _dbContext.ExpenseReports.Remove(report);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
