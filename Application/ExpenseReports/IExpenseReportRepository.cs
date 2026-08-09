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

    /// <summary>
    /// 硬刪除。目前只用在申請人刪除自己還沒送出過的草稿——沒有審核紀錄、沒有結清紀錄綁著，
    /// 刪掉真的沒有後遺症。已進入審核流程的單改用軟刪除（Cancelled 狀態），這裡不動。
    /// </summary>
    Task DeleteAsync(ExpenseReport report, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
