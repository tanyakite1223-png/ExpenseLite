using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Identity;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Application.Home;

public sealed class HomeAppService
{
    private readonly IExpenseReportRepository _reports;
    private readonly CashAdvanceAppService _cashAdvances;

    public HomeAppService(
        IExpenseReportRepository reports,
        CashAdvanceAppService cashAdvances)
    {
        _reports = reports;
        _cashAdvances = cashAdvances;
    }

    public async Task<HomePageDto> GetPageAsync(
        CurrentUser viewer,
        CancellationToken cancellationToken = default)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        var cashAdvancePage = await _cashAdvances.ListPageAsync(
            new CashAdvanceListQuery(null, null),
            viewer,
            cancellationToken);
        var cashAdvances = cashAdvancePage.CashAdvances;

        var todos = viewer.IsManager
            ? BuildManagerTodos(reports, cashAdvances, viewer)
            : BuildEmployeeTodos(reports, cashAdvances, viewer);

        var nonEmpty = todos.Where(x => x.Count > 0).ToList();
        var lede = nonEmpty.Count > 0 ? nonEmpty[0] : null;
        var items = nonEmpty.Count > 1 ? (IReadOnlyList<HomeTodoDto>)nonEmpty.Skip(1).ToList() : [];

        return new HomePageDto(viewer.DisplayName, viewer.IsManager, lede, items);
    }

    private static IEnumerable<HomeTodoDto> BuildManagerTodos(
        IReadOnlyList<ExpenseReport> reports,
        IReadOnlyList<CashAdvanceListItemDto> cashAdvances,
        CurrentUser viewer)
    {
        // 等你審核：Submitted 且不是自己送的
        var awaitingReview = reports
            .Where(x => x.Status == ExpenseReportStatus.Submitted &&
                        x.ApplicantUserId != viewer.UserId)
            .ToList();

        if (awaitingReview.Count > 0)
        {
            var oldestWait = awaitingReview
                .Where(x => x.SubmittedAt.HasValue)
                .Min(x => x.SubmittedAt!.Value);
            var days = (int)(DateTimeOffset.UtcNow - oldestWait).TotalDays;
            var why = days > 0
                ? $"最久那張已等了 {days} 天。"
                : "今天剛送進來。";

            yield return new HomeTodoDto(
                awaitingReview.Count,
                "張報銷單等你審核",
                why,
                "/ExpenseReports/Index?status=Submitted",
                false);
        }

        // 作廢沒收拾：有作廢報銷單 且 預支款未結清
        var voidedNotCleaned = cashAdvances
            .Where(x => x.VoidedRelatedReportCount > 0 && !x.IsSettled)
            .ToList();

        if (voidedNotCleaned.Count > 0)
        {
            yield return new HomeTodoDto(
                voidedNotCleaned.Count,
                "筆預支款有作廢單未收拾",
                "已核准報銷已扣掉，但結清紀錄要手動確認。",
                "/CashAdvances/Index",
                true);
        }

        // 可最終結清：有待結清金額 且 無流程中報銷單
        var canSettle = cashAdvances
            .Where(x => x.RemainingSettlementAmount > 0 && !x.HasInProgressReports)
            .ToList();

        if (canSettle.Count > 0)
        {
            yield return new HomeTodoDto(
                canSettle.Count,
                "筆預支款可最終結清",
                "相關報銷單都已核准或拒絕，算式已定案。",
                canSettle.Count == 1
                    ? $"/CashAdvances/Details/{canSettle[0].Id}"
                    : "/CashAdvances/Index",
                false);
        }

        // 仍在流程中：有尚未結案的相關報銷單
        var inProgress = cashAdvances
            .Where(x => x.HasInProgressReports)
            .ToList();

        if (inProgress.Count > 0)
        {
            yield return new HomeTodoDto(
                inProgress.Count,
                "筆預支款仍在流程中",
                "核對金額只是暫估，等相關報銷單結案再結清。",
                "/CashAdvances/Index",
                false);
        }
    }

    private static IEnumerable<HomeTodoDto> BuildEmployeeTodos(
        IReadOnlyList<ExpenseReport> reports,
        IReadOnlyList<CashAdvanceListItemDto> cashAdvances,
        CurrentUser viewer)
    {
        // 被退回要改：自己的 Returned 單
        var returned = reports
            .Where(x => x.ApplicantUserId == viewer.UserId &&
                        x.Status == ExpenseReportStatus.Returned)
            .ToList();

        if (returned.Count > 0)
        {
            string why;
            string url;

            if (returned.Count == 1)
            {
                var latestReason = returned[0].ReviewRecords
                    .OrderByDescending(r => r.ReviewedAt)
                    .Select(r => r.Reason)
                    .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
                why = string.IsNullOrWhiteSpace(latestReason)
                    ? "請修改後重新送審。"
                    : latestReason;
                url = $"/ExpenseReports/Details/{returned[0].Id}";
            }
            else
            {
                why = $"共 {returned.Count} 張等待修改重送。";
                url = "/ExpenseReports/Index?status=Returned";
            }

            yield return new HomeTodoDto(
                returned.Count,
                "張報銷單被退回",
                why,
                url,
                true);
        }

        // 草稿還沒送審：自己的 Draft 單
        var drafts = reports
            .Where(x => x.ApplicantUserId == viewer.UserId &&
                        x.Status == ExpenseReportStatus.Draft)
            .ToList();

        if (drafts.Count > 0)
        {
            var emptyCount = drafts.Count(x => !x.Details.Any());
            var why = emptyCount > 0
                ? $"其中 {emptyCount} 張沒有明細，送不出去。"
                : "記得在截止前送審。";

            yield return new HomeTodoDto(
                drafts.Count,
                "張草稿還沒送審",
                why,
                "/ExpenseReports/Index?status=Draft",
                false);
        }

        // 預支款還沒用完：自己是領款人 且 仍有待結清金額
        var unspent = cashAdvances
            .Where(x => x.PayeeUserId == viewer.UserId &&
                        x.RemainingSettlementAmount > 0)
            .ToList();

        if (unspent.Count > 0)
        {
            string why;
            string url;

            if (unspent.Count == 1)
            {
                why = $"{unspent[0].Purpose}，尚可報銷 {unspent[0].RemainingSettlementAmount:N2} 元。";
                url = $"/CashAdvances/Details/{unspent[0].Id}";
            }
            else
            {
                var total = unspent.Sum(x => x.RemainingSettlementAmount);
                why = $"共 {total:N2} 元尚可報銷。";
                url = "/CashAdvances/Index";
            }

            yield return new HomeTodoDto(
                unspent.Count,
                "筆預支款還沒用完",
                why,
                url,
                false);
        }
    }
}
