using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Shared;
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
            .Select(x => new
            {
                CashAdvance = x,
                ApprovedReimbursedAmount = approvedAmounts.GetValueOrDefault(x.Id),
                Settlement = BuildSettlementSummary(x, approvedAmounts.GetValueOrDefault(x.Id))
            })
            .Where(x => x.Settlement.RemainingSettlementAmount > 0m)
            .Select(x => MapOption(x.CashAdvance, x.ApprovedReimbursedAmount))
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

    public async Task<CashAdvanceSettlementDetailDto?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await _cashAdvances.GetByIdAsync(id, cancellationToken);
        if (cashAdvance is null)
        {
            return null;
        }

        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);
        var inProgressCashAdvanceIds = await GetInProgressCashAdvanceIdsAsync(cancellationToken);

        return MapSettlementDetail(
            cashAdvance,
            approvedAmounts.GetValueOrDefault(cashAdvance.Id),
            inProgressCashAdvanceIds.Contains(cashAdvance.Id));
    }

    public async Task RecordSettlementAsync(
        RecordCashAdvanceSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await _cashAdvances.GetByIdAsync(command.CashAdvanceId, cancellationToken);
        if (cashAdvance is null)
        {
            throw new DomainRuleViolationException("找不到預支款。");
        }

        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);
        var inProgressCashAdvanceIds = await GetInProgressCashAdvanceIdsAsync(cancellationToken);
        var settlement = BuildSettlementSummary(
            cashAdvance,
            approvedAmounts.GetValueOrDefault(cashAdvance.Id));

        if (inProgressCashAdvanceIds.Contains(cashAdvance.Id))
        {
            throw new DomainRuleViolationException("這筆預支款仍有流程中的報銷單，請等相關報銷單核准或拒絕後再做最終結清。");
        }

        if (settlement.RequiredSettlementType is null ||
            settlement.RemainingSettlementAmount <= 0m)
        {
            throw new DomainRuleViolationException("這筆預支款目前沒有待結清金額。");
        }

        if (command.Amount > settlement.RemainingSettlementAmount)
        {
            throw new DomainRuleViolationException("結清金額不可超過尚待結清金額。");
        }

        cashAdvance.AddSettlementRecord(
            settlement.RequiredSettlementType.Value,
            command.SettledAt,
            Money.From(command.Amount),
            command.HandledBy,
            command.Note);

        await _cashAdvances.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSettlementAsync(
        UpdateCashAdvanceSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await GetExistingCashAdvanceAsync(command.CashAdvanceId, cancellationToken);
        var record = GetExistingSettlementRecord(cashAdvance, command.SettlementRecordId);

        EnsureSettlementUpdateAmountIsAllowed(
            cashAdvance,
            record,
            command.Amount,
            await GetApprovedAmountAsync(cashAdvance.Id, cancellationToken));

        cashAdvance.UpdateSettlementRecord(
            command.SettlementRecordId,
            command.SettledAt,
            Money.From(command.Amount),
            command.HandledBy,
            command.Note);

        await _cashAdvances.SaveChangesAsync(cancellationToken);
    }

    public async Task VoidSettlementAsync(
        VoidCashAdvanceSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await GetExistingCashAdvanceAsync(command.CashAdvanceId, cancellationToken);

        cashAdvance.VoidSettlementRecord(
            command.SettlementRecordId,
            command.VoidedBy,
            command.VoidReason);

        await _cashAdvances.SaveChangesAsync(cancellationToken);
    }

    private async Task<CashAdvance> GetExistingCashAdvanceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cashAdvance = await _cashAdvances.GetByIdAsync(id, cancellationToken);
        if (cashAdvance is null)
        {
            throw new DomainRuleViolationException("找不到預支款。");
        }

        return cashAdvance;
    }

    private static CashAdvanceSettlementRecord GetExistingSettlementRecord(
        CashAdvance cashAdvance,
        Guid settlementRecordId)
    {
        var record = cashAdvance.SettlementRecords.SingleOrDefault(x => x.Id == settlementRecordId);
        if (record is null)
        {
            throw new DomainRuleViolationException("找不到結清紀錄。");
        }

        return record;
    }

    private async Task<decimal> GetApprovedAmountAsync(
        Guid cashAdvanceId,
        CancellationToken cancellationToken)
    {
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);
        return approvedAmounts.GetValueOrDefault(cashAdvanceId);
    }

    private static void EnsureSettlementUpdateAmountIsAllowed(
        CashAdvance cashAdvance,
        CashAdvanceSettlementRecord record,
        decimal newAmount,
        decimal approvedReimbursedAmount)
    {
        if (record.IsVoided)
        {
            return;
        }

        var settlement = BuildSettlementSummary(cashAdvance, approvedReimbursedAmount);

        if (settlement.RequiredSettlementType is null ||
            record.SettlementType != settlement.RequiredSettlementType.Value)
        {
            throw new DomainRuleViolationException("這筆結清紀錄的方向已不符合目前應結清方向，請先標記為不採用後再重新結清。");
        }

        var currentContribution = record.Amount.Amount;
        var maxAllowedAmount = settlement.RemainingSettlementAmount + currentContribution;
        if (newAmount > maxAllowedAmount)
        {
            throw new DomainRuleViolationException("結清金額不可超過尚待結清金額。");
        }
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

    private async Task<HashSet<Guid>> GetInProgressCashAdvanceIdsAsync(
        CancellationToken cancellationToken)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        return reports
            .Where(x =>
                x.PaymentMethod == ExpensePaymentMethod.CashAdvance &&
                x.CashAdvanceId is not null &&
                IsInProgress(x.Status))
            .Select(x => x.CashAdvanceId!.Value)
            .ToHashSet();
    }

    private async Task<(int TotalCashAdvanceCount, IReadOnlyList<CashAdvanceListItemDto> Items)> BuildListItemsAsync(
        CancellationToken cancellationToken)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);
        var inProgressCashAdvanceIds = await GetInProgressCashAdvanceIdsAsync(cancellationToken);

        var items = cashAdvances
            .OrderByDescending(x => x.AdvancedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => MapListItem(
                x,
                approvedAmounts.GetValueOrDefault(x.Id),
                inProgressCashAdvanceIds.Contains(x.Id)))
            .ToList();

        return (cashAdvances.Count, items);
    }

    private static CashAdvanceListItemDto MapListItem(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount,
        bool hasInProgressReports)
    {
        var settlement = BuildSettlementSummary(cashAdvance, approvedReimbursedAmount);

        return new CashAdvanceListItemDto(
            cashAdvance.Id,
            cashAdvance.PayeeName,
            cashAdvance.Purpose,
            cashAdvance.AdvancedAt,
            cashAdvance.Amount.Amount,
            approvedReimbursedAmount,
            settlement.Difference,
            settlement.RequiredSettlementAmount,
            settlement.SettledAmount,
            settlement.RemainingSettlementAmount,
            settlement.RequiredSettlementType,
            hasInProgressReports,
            settlement.RemainingSettlementAmount == 0m,
            settlement.ReconciliationStatus);
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

    private static CashAdvanceSettlementDetailDto MapSettlementDetail(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount,
        bool hasInProgressReports)
    {
        var settlement = BuildSettlementSummary(cashAdvance, approvedReimbursedAmount);

        return new CashAdvanceSettlementDetailDto(
            cashAdvance.Id,
            cashAdvance.PayeeName,
            cashAdvance.Purpose,
            cashAdvance.AdvancedAt,
            cashAdvance.Amount.Amount,
            approvedReimbursedAmount,
            settlement.Difference,
            settlement.RequiredSettlementAmount,
            settlement.SettledAmount,
            settlement.RemainingSettlementAmount,
            settlement.RequiredSettlementType,
            hasInProgressReports,
            settlement.ReconciliationStatus,
            cashAdvance.SettlementRecords
                .OrderByDescending(x => x.SettledAt)
                .ThenByDescending(x => x.CreatedAt)
                .Select(MapSettlementRecord)
                .ToList());
    }

    private static CashAdvanceSettlementRecordDto MapSettlementRecord(
        CashAdvanceSettlementRecord record)
    {
        var canChange = !record.IsVoided;

        return new CashAdvanceSettlementRecordDto(
            record.Id,
            record.SettlementType,
            record.SettledAt,
            record.Amount.Amount,
            record.HandledBy,
            record.Note,
            record.IsVoided,
            record.VoidedBy,
            record.VoidReason,
            record.VoidedAt,
            record.UpdatedAt,
            record.CreatedAt,
            canChange,
            canChange);
    }

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

    private static CashAdvanceSettlementSummary BuildSettlementSummary(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount)
    {
        var difference = approvedReimbursedAmount - cashAdvance.Amount.Amount;
        CashAdvanceSettlementType? requiredSettlementType = null;
        if (difference > 0m)
        {
            requiredSettlementType = CashAdvanceSettlementType.CompanyPaid;
        }
        else if (difference < 0m)
        {
            requiredSettlementType = CashAdvanceSettlementType.EmployeeReturned;
        }

        var requiredSettlementAmount = Math.Abs(difference);
        var settledAmount = requiredSettlementType is null
            ? 0m
            : cashAdvance.SettlementRecords
                .Where(x => x.SettlementType == requiredSettlementType.Value && !x.IsVoided)
                .Sum(x => x.Amount.Amount);
        var remainingSettlementAmount = Math.Max(
            0m,
            requiredSettlementAmount - settledAmount);
        var reconciliationStatus = GetReconciliationStatus(
            approvedReimbursedAmount,
            difference,
            remainingSettlementAmount);

        return new CashAdvanceSettlementSummary(
            difference,
            requiredSettlementAmount,
            settledAmount,
            remainingSettlementAmount,
            requiredSettlementType,
            reconciliationStatus);
    }

    private static CashAdvanceReconciliationStatus GetReconciliationStatus(
        decimal approvedReimbursedAmount,
        decimal difference,
        decimal remainingSettlementAmount)
    {
        if (difference == 0m || remainingSettlementAmount == 0m)
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

    private static bool IsInProgress(ExpenseReportStatus status)
        => status is ExpenseReportStatus.Draft or
            ExpenseReportStatus.Submitted or
            ExpenseReportStatus.Returned;

    private sealed record CashAdvanceSettlementSummary(
        decimal Difference,
        decimal RequiredSettlementAmount,
        decimal SettledAmount,
        decimal RemainingSettlementAmount,
        CashAdvanceSettlementType? RequiredSettlementType,
        CashAdvanceReconciliationStatus ReconciliationStatus);
}
