using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Application.ExpenseReports;

public sealed class ExpenseReportAppService
{
    private readonly IExpenseReportRepository _reports;

    public ExpenseReportAppService(IExpenseReportRepository reports)
    {
        _reports = reports;
    }

    public async Task<IReadOnlyList<ExpenseReportListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        return reports
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapListItem)
            .ToList();
    }

    public async Task<ExpenseReportDetailDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await _reports.GetByIdAsync(id, cancellationToken);
        return report is null ? null : MapDetails(report);
    }

    public async Task<Guid> CreateAsync(CreateExpenseReportCommand command, CancellationToken cancellationToken = default)
    {
        var report = ExpenseReport.Create(command.Title, command.ApplicantName);

        await _reports.AddAsync(report, cancellationToken);
        await _reports.SaveChangesAsync(cancellationToken);

        return report.Id;
    }

    public async Task AddDetailAsync(AddExpenseDetailCommand command, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(command.ReportId, cancellationToken);

        report.AddDetail(
            command.ExpenseDate,
            command.Category,
            command.Description,
            Money.From(command.Amount));

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDetailAsync(Guid reportId, Guid detailId, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(reportId, cancellationToken);

        report.RemoveDetail(detailId);

        await _reports.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await GetRequiredReportAsync(id, cancellationToken);

        report.Submit();

        await _reports.SaveChangesAsync(cancellationToken);
    }

    private async Task<ExpenseReport> GetRequiredReportAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(id, cancellationToken);

        return report ?? throw new DomainRuleViolationException("找不到指定的報銷單。");
    }

    private static ExpenseReportListItemDto MapListItem(ExpenseReport report)
        => new(
            report.Id,
            report.Title,
            report.ApplicantName,
            report.Status,
            report.TotalAmount.Amount,
            report.CreatedAt);

    private static ExpenseReportDetailDto MapDetails(ExpenseReport report)
        => new(
            report.Id,
            report.Title,
            report.ApplicantName,
            report.Status,
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
                    x.Amount.Amount))
                .ToList());
}
