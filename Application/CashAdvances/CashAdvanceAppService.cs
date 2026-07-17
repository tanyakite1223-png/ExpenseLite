using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Application.CashAdvances;

public sealed class CashAdvanceAppService
{
    private readonly ICashAdvanceRepository _cashAdvances;
    private readonly IExpenseReportRepository _reports;

    public CashAdvanceAppService(
        ICashAdvanceRepository cashAdvances,
        IExpenseReportRepository reports)
    {
        _cashAdvances = cashAdvances;
        _reports = reports;
    }

    public async Task<IReadOnlyList<CashAdvanceListItemDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);

        return cashAdvances
            .OrderByDescending(x => x.AdvancedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => MapListItem(x, approvedAmounts.GetValueOrDefault(x.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<CashAdvanceOptionDto>> ListOpenOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);

        return cashAdvances
            .OrderByDescending(x => x.AdvancedAt)
            .ThenBy(x => x.PayeeName)
            .Select(x => MapOption(x, approvedAmounts.GetValueOrDefault(x.Id)))
            .Where(x => x.Amount != x.ApprovedReimbursedAmount)
            .ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateCashAdvanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = CashAdvance.Create(
            command.PayeeName,
            command.Purpose,
            command.AdvancedAt,
            Money.From(command.Amount));

        await _cashAdvances.AddAsync(cashAdvance, cancellationToken);
        await _cashAdvances.SaveChangesAsync(cancellationToken);

        return cashAdvance.Id;
    }

    private async Task<Dictionary<Guid, decimal>> GetApprovedAmountsByCashAdvanceAsync(
        CancellationToken cancellationToken)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        return reports
            .Where(x =>
                x.PaymentMethod == ExpensePaymentMethod.CashAdvance &&
                x.CashAdvanceId is not null &&
                x.Status == ExpenseReportStatus.Approved)
            .GroupBy(x => x.CashAdvanceId!.Value)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(report => report.TotalAmount.Amount));
    }

    private static CashAdvanceListItemDto MapListItem(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount)
    {
        var difference = approvedReimbursedAmount - cashAdvance.Amount.Amount;

        return new CashAdvanceListItemDto(
            cashAdvance.Id,
            cashAdvance.PayeeName,
            cashAdvance.Purpose,
            cashAdvance.AdvancedAt,
            cashAdvance.Amount.Amount,
            approvedReimbursedAmount,
            difference,
            difference == 0m);
    }

    private static CashAdvanceOptionDto MapOption(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount)
        => new(
            cashAdvance.Id,
            cashAdvance.PayeeName,
            cashAdvance.Purpose,
            cashAdvance.AdvancedAt,
            cashAdvance.Amount.Amount,
            approvedReimbursedAmount);
}
