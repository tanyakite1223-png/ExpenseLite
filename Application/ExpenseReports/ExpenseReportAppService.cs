using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.Projects;
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

    public async Task<IReadOnlyList<ExpenseReportListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var reports = await _reports.ListAsync(cancellationToken);
        var projectNames = await GetProjectNamesAsync(cancellationToken);

        return reports
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => MapListItem(x, projectNames.GetValueOrDefault(x.ProjectId ?? Guid.Empty)))
            .ToList();
    }

    public async Task<ExpenseReportDetailDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return null;
        }

        var project = await GetProjectAsync(report.ProjectId, cancellationToken);
        return MapDetails(report, project?.Name, project?.Status);
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
            command.ApplicantName,
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
            command.ApplicantName,
            command.ExpenseType,
            command.ProjectId,
            command.PaymentMethod,
            command.CashAdvanceId);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDetailAsync(AddExpenseDetailCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.AddDetail(
            command.ExpenseDate,
            command.Category,
            command.Description,
            command.ReceiptType,
            command.InvoiceNumber,
            Money.From(command.Amount));

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDetailAsync(Guid reportId, Guid detailId, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(reportId, cancellationToken);

        report.RemoveDetail(detailId);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDetailAsync(UpdateExpenseDetailCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

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

    public async Task SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(id, cancellationToken);

        await EnsureProjectCanBeSubmittedAsync(report, cancellationToken);

        report.Submit();

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task ReturnAsync(ReviewExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Return(command.ReviewerName, command.Reason ?? string.Empty);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(ReviewExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Approve(command.ReviewerName);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(ReviewExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.Reject(command.ReviewerName, command.Reason ?? string.Empty);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExpenseReport> GetRequiredReportAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(id, cancellationToken);

        return report ?? throw new DomainRuleViolationException("找不到指定的報銷單。");
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
        if (paymentMethod != ExpensePaymentMethod.CashAdvance)
        {
            return;
        }

        if (cashAdvanceId is null)
        {
            throw new DomainRuleViolationException("預支費用報銷單必須選擇對應的預支款。");
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
            report.ApplicantName,
            report.Status,
            report.ExpenseType,
            report.ProjectId,
            projectName,
            report.PaymentMethod,
            report.CashAdvanceId,
            report.TotalAmount.Amount,
            report.CreatedAt);

    private static ExpenseReportDetailDto MapDetails(
        ExpenseReport report,
        string? projectName,
        ProjectStatus? projectStatus)
        => new(
            report.Id,
            report.Title,
            report.ApplicantName,
            report.Status,
            report.ExpenseType,
            report.ProjectId,
            projectName,
            projectStatus,
            report.PaymentMethod,
            report.CashAdvanceId,
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
