using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

public interface IExpenseReportRepository
{
    Task<IReadOnlyList<ExpenseReport>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseReport>> ListByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 各專案未完成報銷單的筆數。<paramref name="applicantUserId"/> 給值時只算那個人的單，
    /// null 代表不分申請人全算——可見度的判斷在 Application，這裡只負責把條件下推到查詢。
    /// </summary>
    Task<Dictionary<Guid, int>> CountUnfinishedProjectReportsAsync(
        Guid? applicantUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasUnfinishedProjectReportsAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task AddAsync(ExpenseReport report, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
