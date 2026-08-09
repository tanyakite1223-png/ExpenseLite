using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.Identity;
using ExpenseLite.Application.Projects;
using ExpenseLite.Application.Shared;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Projects;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Application.ExpenseReports;

public sealed class ExpenseReportAppService
{
    private readonly IExpenseReportRepository _reports;
    private readonly ICashAdvanceRepository _cashAdvances;
    private readonly IProjectRepository _projects;

    public ExpenseReportAppService(
        IExpenseReportRepository reports,
        ICashAdvanceRepository cashAdvances,
        IProjectRepository projects)
    {
        _reports = reports;
        _cashAdvances = cashAdvances;
        _projects = projects;
    }

    public async Task<ExpenseReportListPageDto> ListPageAsync(
        ExpenseReportListQuery query,
        CurrentUser viewer,
        CancellationToken cancellationToken = default)
    {
        var reports = await _reports.ListAsync(cancellationToken);
        var projectNames = await GetProjectNamesAsync(cancellationToken);
        var normalizedKeyword = NormalizeKeyword(query.Keyword);

        // 先過濾可見度再算總筆數，「還沒有報銷單」和「篩選條件沒中」才不會被別人的資料混淆。
        var visible = reports
            .Where(x => ExpenseReportVisibility.CanBeViewedBy(x, viewer))
            .ToList();

        var items = visible
            .Where(x => MatchesFilter(x, normalizedKeyword, query))
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => MapListItem(x, projectNames.GetValueOrDefault(x.ProjectId ?? Guid.Empty)))
            .ToList();

        return new ExpenseReportListPageDto(
            normalizedKeyword,
            query.Status,
            query.ExpenseType,
            query.PaymentMethod,
            query.IncludeCancelled,
            visible.Count,
            items);
    }

    public async Task<ExpenseReportDetailDto?> GetDetailsAsync(
        Guid id,
        CurrentUser viewer,
        CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return null;
        }

        ExpenseReportVisibility.EnsureCanBeViewedBy(report, viewer);

        var project = await GetProjectAsync(report.ProjectId, cancellationToken);
        var cashAdvance = await GetCashAdvanceSummaryAsync(report.CashAdvanceId, cancellationToken);
        var adoptedSettlementCount = await GetAdoptedSettlementRecordCountAsync(
            report.CashAdvanceId,
            cancellationToken);

        return MapDetails(report, project?.Name, project?.Status, cashAdvance, adoptedSettlementCount);
    }

    /// <summary>
    /// 這張報銷單所綁的預支款上，仍被採用的結清紀錄筆數。
    /// 沒綁預支款直接回 0；作廢按鈕的確認框、Voided 後的持續提示都用這個判斷要不要提醒主管去處理。
    /// </summary>
    private async Task<int> GetAdoptedSettlementRecordCountAsync(
        Guid? cashAdvanceId,
        CancellationToken cancellationToken)
    {
        if (cashAdvanceId is null)
        {
            return 0;
        }

        var cashAdvance = await _cashAdvances.GetByIdAsync(cashAdvanceId.Value, cancellationToken);
        if (cashAdvance is null)
        {
            return 0;
        }

        return cashAdvance.SettlementRecords.Count(x => !x.IsVoided);
    }

    public async Task<Guid> CreateAsync(CreateExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        await EnsureProjectCanBeUsedAsync(
            command.ExpenseType,
            command.ProjectId,
            "已結案專案不可新增報銷單。",
            cancellationToken);
        await EnsureCashAdvanceExistsAsync(
            command.PaymentMethod,
            command.CashAdvanceId,
            cancellationToken);

        var report = ExpenseReport.Create(
            command.Title,
            command.Applicant.UserId,
            command.Applicant.DisplayName,
            command.ExpenseType,
            command.ProjectId,
            command.PaymentMethod,
            command.CashAdvanceId);

        await _reports.AddAsync(report, cancellationToken);
        await _reports.SaveChangesAsync(cancellationToken);

        return report.Id;
    }

    public async Task UpdateAsync(UpdateExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.Id, cancellationToken);
        EnsureCanBeEditedBy(report, command.Editor);

        await EnsureProjectCanBeUsedAsync(
            command.ExpenseType,
            command.ProjectId,
            "已結案專案不可套用到報銷單。",
            cancellationToken);
        await EnsureCashAdvanceExistsAsync(
            command.PaymentMethod,
            command.CashAdvanceId,
            cancellationToken);

        report.UpdateBasicInfo(
            command.Title,
            command.ExpenseType,
            command.ProjectId,
            command.PaymentMethod,
            command.CashAdvanceId);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDetailAsync(AddExpenseDetailCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeEditedBy(report, command.Editor);

        report.AddDetail(
            command.ExpenseDate,
            command.Category,
            command.Description,
            command.ReceiptType,
            command.InvoiceNumber,
            Money.From(command.Amount));

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDetailAsync(RemoveExpenseDetailCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeEditedBy(report, command.Editor);

        report.RemoveDetail(command.DetailId);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDetailAsync(UpdateExpenseDetailCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeEditedBy(report, command.Editor);

        report.UpdateDetail(
            command.DetailId,
            command.ExpenseDate,
            command.Category,
            command.Description,
            command.ReceiptType,
            command.InvoiceNumber,
            Money.From(command.Amount));

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(SubmitExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeEditedBy(report, command.Editor);

        await EnsureProjectCanBeSubmittedAsync(report, cancellationToken);

        report.Submit();

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(ReviewExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Return(command.Reviewer.UserId, command.Reviewer.DisplayName, command.Reason ?? string.Empty);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(ReviewExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Approve(command.Reviewer.UserId, command.Reviewer.DisplayName);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(ReviewExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Reject(command.Reviewer.UserId, command.Reviewer.DisplayName, command.Reason ?? string.Empty);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 主管作廢已核准的報銷單。不可撤銷，要救就重打一張新單。
    /// 「主管才能」是角色授權，擋在 Controller 的 [Authorize(Roles = Manager)]；
    /// 「不能作廢自己送的單」是報銷單的 invariant，Domain method 內擋。
    /// 已計入的結清紀錄由主管手動處理——這個方法不動它，只改報銷單狀態。
    /// </summary>
    public async Task VoidAsync(VoidExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Void(command.Reviewer.UserId, command.Reviewer.DisplayName, command.Reason);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    /// <summary>申請人硬刪自己還沒送出過的草稿。</summary>
    public async Task DeleteDraftAsync(DeleteExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeManagedByApplicant(report, command.Applicant);

        if (report.Status != ExpenseReportStatus.Draft)
        {
            throw new DomainRuleViolationException("只有草稿可以刪除。已進入審核流程的報銷單請改用取消或作廢。");
        }

        await _reports.DeleteAsync(report, cancellationToken);
        await _reports.SaveChangesAsync(cancellationToken);
    }

    /// <summary>申請人取消退回單（軟刪）。可再由 <see cref="RestoreAsync"/> 復活。</summary>
    public async Task CancelAsync(CancelExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeManagedByApplicant(report, command.Applicant);

        report.Cancel();

        await _reports.SaveChangesAsync(cancellationToken);
    }

    /// <summary>申請人復活軟刪的報銷單，回到退回狀態、之後可再修改重送。</summary>
    public async Task RestoreAsync(RestoreExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);
        EnsureCanBeManagedByApplicant(report, command.Applicant);

        report.Restore();

        await _reports.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 刪除 / 取消 / 復活都是「申請人自己動自己的單」，共用一條授權規則。
    /// 跟 <see cref="EnsureCanBeEditedBy"/> 檢查一樣，只是訊息更廣，涵蓋三個非修改的動作。
    /// </summary>
    private static void EnsureCanBeManagedByApplicant(ExpenseReport report, CurrentUser actor)
    {
        if (report.ApplicantUserId != actor.UserId)
        {
            throw new ForbiddenOperationException("只有申請人可以管理自己的報銷單。");
        }
    }

    private async Task<ExpenseReport> GetRequiredReportAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(id, cancellationToken);

        return report ?? throw new DomainRuleViolationException("找不到指定的報銷單。");
    }

    /// <summary>
    /// 只有申請人本人可以修改 / 送審自己的報銷單，主管沒有例外——主管的角色是審核，不是代改。
    /// 這是 resource-based authorization：要先載入報銷單才知道申請人是誰，所以必須放在 Application 層；
    /// 不像「主管才能核准」那種純角色檢查，可以在 Controller 用 [Authorize(Roles = ...)] 就擋掉。
    /// ApplicantUserId 為 null 是登入功能之前的舊資料，沒有申請人可比對，一律視為不可修改。
    /// </summary>
    private static void EnsureCanBeEditedBy(ExpenseReport report, CurrentUser editor)
    {
        if (report.ApplicantUserId != editor.UserId)
        {
            throw new ForbiddenOperationException("只有申請人可以修改或送審自己的報銷單。");
        }
    }

    private async Task EnsureProjectCanBeUsedAsync(
        ExpenseType expenseType,
        Guid? projectId,
        string closedProjectMessage,
        CancellationToken cancellationToken)
    {
        if (expenseType != ExpenseType.Project)
        {
            return;
        }

        if (projectId is null)
        {
            throw new DomainRuleViolationException("專案支出報銷單必須選擇專案。");
        }

        var project = await _projects.GetByIdAsync(projectId.Value, cancellationToken);
        if (project is null)
        {
            throw new DomainRuleViolationException("找不到指定的專案。");
        }

        if (project.Status != ProjectStatus.Active)
        {
            throw new DomainRuleViolationException(closedProjectMessage);
        }
    }

    private async Task EnsureCashAdvanceExistsAsync(
        ExpensePaymentMethod paymentMethod,
        Guid? cashAdvanceId,
        CancellationToken cancellationToken)
    {
        if (paymentMethod != ExpensePaymentMethod.PersonalAdvance)
        {
            return;
        }

        if (cashAdvanceId is null)
        {
            throw new DomainRuleViolationException("個人預支報銷單必須選擇對應的預支款。");
        }

        var cashAdvance = await _cashAdvances.GetByIdAsync(cashAdvanceId.Value, cancellationToken);
        if (cashAdvance is null)
        {
            throw new DomainRuleViolationException("找不到指定的預支款。");
        }
    }

    private async Task<Dictionary<Guid, string>> GetProjectNamesAsync(CancellationToken cancellationToken)
    {
        var projects = await _projects.ListAsync(cancellationToken);

        return projects.ToDictionary(x => x.Id, x => x.Name);
    }

    private async Task<string?> GetProjectNameAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(projectId, cancellationToken);
        return project?.Name;
    }

    private async Task<Project?> GetProjectAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        if (projectId is null)
        {
            return null;
        }

        return await _projects.GetByIdAsync(projectId.Value, cancellationToken);
    }

    private async Task<ExpenseReportCashAdvanceDto?> GetCashAdvanceSummaryAsync(
        Guid? cashAdvanceId,
        CancellationToken cancellationToken)
    {
        if (cashAdvanceId is null)
        {
            return null;
        }

        var cashAdvance = await _cashAdvances.GetByIdAsync(cashAdvanceId.Value, cancellationToken);
        if (cashAdvance is null)
        {
            return null;
        }

        return new ExpenseReportCashAdvanceDto(
            cashAdvance.Id,
            cashAdvance.PayeeName,
            cashAdvance.Purpose,
            cashAdvance.AdvancedAt);
    }

    private async Task EnsureProjectCanBeSubmittedAsync(
        ExpenseReport report,
        CancellationToken cancellationToken)
    {
        if (report.ExpenseType != ExpenseType.Project)
        {
            return;
        }

        if (report.ProjectId is null)
        {
            throw new DomainRuleViolationException("專案支出報銷單必須選擇專案。");
        }

        var project = await _projects.GetByIdAsync(report.ProjectId.Value, cancellationToken);
        if (project is null)
        {
            throw new DomainRuleViolationException("找不到指定的專案。");
        }

        if (project.Status != ProjectStatus.Active)
        {
            throw new DomainRuleViolationException("已結案專案的報銷單不可送審。");
        }
    }

    private static ExpenseReportListItemDto MapListItem(ExpenseReport report, string? projectName)
        => new(
            report.Id,
            report.Title,
            report.ApplicantUserId,
            report.ApplicantName,
            report.Status,
            report.ExpenseType,
            report.ProjectId,
            projectName,
            report.PaymentMethod,
            report.CashAdvanceId,
            report.TotalAmount.Amount,
            report.CreatedAt);

    private static bool MatchesFilter(
        ExpenseReport report,
        string keyword,
        ExpenseReportListQuery query)
    {
        if (query.Status is not null && report.Status != query.Status)
        {
            return false;
        }

        // 軟刪的 Cancelled 預設隱藏，避免退回單被取消後在列表上一直干擾申請人與主管。
        // 兩種情況會顯示：使用者勾了「顯示已取消」的篩選、或明確在狀態下拉選 Cancelled。
        if (report.Status == ExpenseReportStatus.Cancelled &&
            !query.IncludeCancelled &&
            query.Status != ExpenseReportStatus.Cancelled)
        {
            return false;
        }

        if (query.ExpenseType is not null && report.ExpenseType != query.ExpenseType)
        {
            return false;
        }

        if (query.PaymentMethod is not null && report.PaymentMethod != query.PaymentMethod)
        {
            return false;
        }

        return MatchesKeyword(report, keyword);
    }

    private static bool MatchesKeyword(ExpenseReport report, string keyword)
    {
        if (keyword.Length == 0)
        {
            return true;
        }

        return ContainsKeyword(report.Title, keyword) ||
               ContainsKeyword(report.ApplicantName, keyword);
    }

    private static string NormalizeKeyword(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? string.Empty : keyword.Trim();

    private static bool ContainsKeyword(string value, string keyword)
        => value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static ExpenseReportDetailDto MapDetails(
        ExpenseReport report,
        string? projectName,
        ProjectStatus? projectStatus,
        ExpenseReportCashAdvanceDto? cashAdvance,
        int adoptedSettlementRecordCount)
        => new(
            report.Id,
            report.Title,
            report.ApplicantUserId,
            report.ApplicantName,
            report.Status,
            report.ExpenseType,
            report.ProjectId,
            projectName,
            projectStatus,
            report.PaymentMethod,
            report.CashAdvanceId,
            cashAdvance,
            adoptedSettlementRecordCount,
            report.TotalAmount.Amount,
            report.CreatedAt,
            report.SubmittedAt,
            report.Details
                .OrderBy(x => x.ExpenseDate)
                .Select(x => new ExpenseDetailDto(
                    x.Id,
                    x.ExpenseDate,
                    x.Category,
                    x.Description,
                    x.ReceiptType,
                    x.InvoiceNumber,
                    x.Amount.Amount))
                .ToList(),
            report.ReviewRecords
                .OrderByDescending(x => x.ReviewedAt)
                .Select(x => new ExpenseReviewRecordDto(
                    x.Id,
                    x.Action,
                    x.ReviewerName,
                    x.Reason,
                    x.ReviewedAt))
                .ToList());
}
