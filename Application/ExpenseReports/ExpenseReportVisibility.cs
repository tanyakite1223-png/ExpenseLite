using ExpenseLite.Application.Identity;
using ExpenseLite.Application.Shared;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.ExpenseReports;

/// <summary>
/// 「誰看得到一張報銷單」的單一出處：主管（要審核，所以看得到全部），或申請人本人。
/// 員工彼此的花費不該互相看見——報銷單上有金額、用途、單據號碼，是別人的私事。
/// 這是 resource-based authorization：要先載入才知道申請人是誰，
/// 所以擋在 Application 而不是 Controller 的 [Authorize]。
/// 抽成獨立的靜態類別，是因為報銷單列表與專案詳情頁都要用同一條規則——
/// 各自複製一份的話，之後只會改到其中一邊。
/// ApplicantUserId 為 null 是登入功能之前的舊資料，沒有申請人可比對，只有主管看得到。
/// </summary>
public static class ExpenseReportVisibility
{
    public static bool CanBeViewedBy(ExpenseReport report, CurrentUser viewer)
        => viewer.IsManager || report.ApplicantUserId == viewer.UserId;

    public static void EnsureCanBeViewedBy(ExpenseReport report, CurrentUser viewer)
    {
        if (CanBeViewedBy(report, viewer))
        {
            return;
        }

        throw new ForbiddenOperationException("只有主管或這張報銷單的申請人可以查看。");
    }
}
