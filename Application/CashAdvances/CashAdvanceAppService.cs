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
        var list = await BuildListItemsAsync(cancellationToken);
        return list.Items;
    }

    public async Task<CashAdvanceListPageDto> ListPageAsync(
        CashAdvanceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var list = await BuildListItemsAsync(cancellationToken);
        var normalizedKeyword = NormalizeKeyword(query.Keyword);

        var items = list.Items
            .Where(x => MatchesFilter(x, normalizedKeyword, query.ReconciliationStatus))
            .ToList();

        return new CashAdvanceListPageDto(
            normalizedKeyword,
            query.ReconciliationStatus,
            list.TotalCashAdvanceCount,
            items);
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

    private async Task<(int TotalCashAdvanceCount, IReadOnlyList<CashAdvanceListItemDto> Items)> BuildListItemsAsync(
        CancellationToken cancellationToken)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);

        var items = cashAdvances
            .OrderByDescending(x => x.AdvancedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => MapListItem(x, approvedAmounts.GetValueOrDefault(x.Id)))
            .ToList();

        return (cashAdvances.Count, items);
    }

    private static CashAdvanceListItemDto MapListItem(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount)
    {
        var difference = approvedReimbursedAmount - cashAdvance.Amount.Amount;
        var reconciliationStatus = GetReconciliationStatus(approvedReimbursedAmount, difference);

        return new CashAdvanceListItemDto(
            cashAdvance.Id,
            cashAdvance.PayeeName,
            cashAdvance.Purpose,
            cashAdvance.AdvancedAt,
            cashAdvance.Amount.Amount,
            approvedReimbursedAmount,
            difference,
            difference == 0m,
            reconciliationStatus);
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

    private static bool MatchesFilter(
        CashAdvanceListItemDto cashAdvance,
        string keyword,
        CashAdvanceReconciliationStatus? reconciliationStatus)
    {
        if (reconciliationStatus is not null &&
            cashAdvance.ReconciliationStatus != reconciliationStatus)
        {
            return false;
        }

        return MatchesKeyword(cashAdvance, keyword);
    }

    private static bool MatchesKeyword(CashAdvanceListItemDto cashAdvance, string keyword)
    {
        if (keyword.Length == 0)
        {
            return true;
        }

        return ContainsKeyword(cashAdvance.PayeeName, keyword) ||
               ContainsKeyword(cashAdvance.Purpose, keyword);
    }

    private static CashAdvanceReconciliationStatus GetReconciliationStatus(
        decimal approvedReimbursedAmount,
        decimal difference)
    {
        if (difference == 0m)
        {
            return CashAdvanceReconciliationStatus.Settled;
        }

        if (approvedReimbursedAmount == 0m)
        {
            return CashAdvanceReconciliationStatus.Unreimbursed;
        }

        return difference > 0m
            ? CashAdvanceReconciliationStatus.CompanyNeedsToPay
            : CashAdvanceReconciliationStatus.EmployeeNeedsToReturn;
    }

    private static string NormalizeKeyword(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? string.Empty : keyword.Trim();

    private static bool ContainsKeyword(string value, string keyword)
        => value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

}
