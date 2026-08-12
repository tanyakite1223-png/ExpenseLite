using ExpenseLite.Application.ExpenseReports;
using ExpenseLite.Application.Identity;
using ExpenseLite.Application.Shared;
using ExpenseLite.Domain.CashAdvances;
using ExpenseLite.Domain.ExpenseReports;
using ExpenseLite.Domain.Shared;
using ExpenseLite.Domain.ValueObjects;

namespace ExpenseLite.Application.CashAdvances;

public sealed class CashAdvanceAppService
{
    private readonly ICashAdvanceRepository _cashAdvances;
    private readonly IExpenseReportRepository _reports;
    private readonly IUserDirectory _users;

    public CashAdvanceAppService(
        ICashAdvanceRepository cashAdvances,
        IExpenseReportRepository reports,
        IUserDirectory users)
    {
        _cashAdvances = cashAdvances;
        _reports = reports;
        _users = users;
    }

    public async Task<CashAdvanceListPageDto> ListPageAsync(
        CashAdvanceListQuery query,
        CurrentUser viewer,
        CancellationToken cancellationToken = default)
    {
        var list = await BuildListItemsAsync(viewer, cancellationToken);
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

    /// <summary>
    /// 報銷單「對應預支款」下拉的選項。零用金是共用的，所有人都能引用；
    /// 個人預支只有領款人本人能引用，否則別人的錢會被報掉。
    /// 這是模型決定的，不是角色決定的——主管也不能拿別人的個人預支來報自己的單。
    /// </summary>
    public async Task<IReadOnlyList<CashAdvanceOptionDto>> ListOpenOptionsAsync(
        CurrentUser viewer,
        CancellationToken cancellationToken = default)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);

        return cashAdvances
            .Where(x => CanBeReferencedBy(x, viewer))
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

    /// <summary>建立預支款時可以指派的領款人。</summary>
    public Task<IReadOnlyList<UserOptionDto>> ListPayeeOptionsAsync(
        CancellationToken cancellationToken = default)
        => _users.ListSelectableAsync(cancellationToken);

    public async Task<Guid> CreateAsync(
        CreateCashAdvanceCommand command,
        CancellationToken cancellationToken = default)
    {
        // 姓名快照由這裡查出來，不從表單接——表單只給 UserId，名字以系統當下的資料為準。
        var payee = await _users.FindByIdAsync(command.PayeeUserId, cancellationToken)
            ?? throw new DomainRuleViolationException("找不到指定的領款人。");

        var cashAdvance = CashAdvance.Create(
            payee.UserId,
            payee.DisplayName,
            command.Purpose,
            command.AdvancedAt,
            Money.From(command.Amount));

        await _cashAdvances.AddAsync(cashAdvance, cancellationToken);
        await _cashAdvances.SaveChangesAsync(cancellationToken);

        return cashAdvance.Id;
    }

    public async Task UpdateAsync(
        UpdateCashAdvanceCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await GetExistingCashAdvanceAsync(command.CashAdvanceId, cancellationToken);
        var approvedReimbursedAmount = await GetApprovedAmountAsync(cashAdvance.Id, cancellationToken);
        EnsureCashAdvanceIsNotSettled(cashAdvance, approvedReimbursedAmount, "修改預支款");

        var amount = Money.From(command.Amount);

        if (cashAdvance.Amount != amount &&
            await HasExpenseReportReferencesAsync(cashAdvance.Id, cancellationToken))
        {
            throw new DomainRuleViolationException("已有報銷單使用這筆預支款時，不可修改預支金額。");
        }

        cashAdvance.UpdateBasicInfo(command.Purpose, amount);

        await _cashAdvances.SaveChangesAsync(cancellationToken);
    }

    public async Task<CashAdvanceSettlementDetailDto?> GetDetailsAsync(
        Guid id,
        CurrentUser viewer,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await _cashAdvances.GetByIdAsync(id, cancellationToken);
        if (cashAdvance is null)
        {
            return null;
        }

        EnsureCanBeViewedBy(cashAdvance, viewer);

        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);
        var inProgressCashAdvanceIds = await GetInProgressCashAdvanceIdsAsync(cancellationToken);
        var relatedCashAdvanceIds = await GetRelatedCashAdvanceIdsAsync(cancellationToken);
        var voidedRelatedCounts = await GetVoidedRelatedReportCountsAsync(cancellationToken);

        return MapSettlementDetail(
            cashAdvance,
            approvedAmounts.GetValueOrDefault(cashAdvance.Id),
            inProgressCashAdvanceIds.Contains(cashAdvance.Id),
            relatedCashAdvanceIds.Contains(cashAdvance.Id),
            voidedRelatedCounts.GetValueOrDefault(cashAdvance.Id));
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
            command.HandledBy.UserId,
            command.HandledBy.DisplayName,
            command.Note);

        await _cashAdvances.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSettlementAsync(
        UpdateCashAdvanceSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await GetExistingCashAdvanceAsync(command.CashAdvanceId, cancellationToken);
        var record = GetExistingSettlementRecord(cashAdvance, command.SettlementRecordId);
        var approvedReimbursedAmount = await GetApprovedAmountAsync(cashAdvance.Id, cancellationToken);

        EnsureCashAdvanceIsNotSettled(cashAdvance, approvedReimbursedAmount, "修改結清紀錄");
        EnsureSettlementUpdateAmountIsAllowed(
            cashAdvance,
            record,
            command.Amount,
            approvedReimbursedAmount);

        cashAdvance.UpdateSettlementRecord(
            command.SettlementRecordId,
            command.SettledAt,
            Money.From(command.Amount),
            command.Note);

        await _cashAdvances.SaveChangesAsync(cancellationToken);
    }

    public async Task VoidSettlementAsync(
        VoidCashAdvanceSettlementCommand command,
        CancellationToken cancellationToken = default)
    {
        var cashAdvance = await GetExistingCashAdvanceAsync(command.CashAdvanceId, cancellationToken);
        GetExistingSettlementRecord(cashAdvance, command.SettlementRecordId);

        var approvedReimbursedAmount = await GetApprovedAmountAsync(cashAdvance.Id, cancellationToken);
        EnsureCashAdvanceIsNotSettled(cashAdvance, approvedReimbursedAmount, "標記結清紀錄為不採用");

        cashAdvance.VoidSettlementRecord(
            command.SettlementRecordId,
            command.VoidedBy.UserId,
            command.VoidedBy.DisplayName,
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

    /// <summary>
    /// 誰看得到一筆預支款的財務核對視角（差額、應結清、結清紀錄）：主管，或這筆的領款人本人。
    /// 領款人要看得到自己那筆錢的使用狀態，保管零用金的人尤其需要；
    /// 但零用金的其他使用者不需要看核對頁——他們只要開自己的報銷單，想知道還剩多少就問保管人。
    /// 這是 resource-based authorization：要先載入才知道領款人是誰，所以擋在 Application 而不是 Controller。
    /// PayeeUserId 為 null 是登入功能之前的舊資料，沒有領款人可比對，只有主管看得到。
    /// </summary>
    private static void EnsureCanBeViewedBy(CashAdvance cashAdvance, CurrentUser viewer)
    {
        if (viewer.IsManager || cashAdvance.PayeeUserId == viewer.UserId)
        {
            return;
        }

        throw new ForbiddenOperationException("只有主管或這筆預支款的領款人可以查看。");
    }

    private static bool CanBeViewedBy(CashAdvance cashAdvance, CurrentUser viewer)
        => viewer.IsManager || cashAdvance.PayeeUserId == viewer.UserId;

    /// <summary>預支款是公司先把錢給某個人，所以只有領款人本人的報銷單能引用——主管也不例外。</summary>
    private static bool CanBeReferencedBy(CashAdvance cashAdvance, CurrentUser viewer)
        => cashAdvance.PayeeUserId == viewer.UserId;

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

    private static void EnsureCashAdvanceIsNotSettled(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount,
        string actionDescription)
    {
        var settlement = BuildSettlementSummary(cashAdvance, approvedReimbursedAmount);
        if (settlement.ReconciliationStatus == CashAdvanceReconciliationStatus.Settled)
        {
            throw new DomainRuleViolationException($"這筆預支款已結清，不可{actionDescription}。");
        }
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
                x.PaymentMethod == ExpensePaymentMethod.PersonalAdvance &&
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
                x.PaymentMethod == ExpensePaymentMethod.PersonalAdvance &&
                x.CashAdvanceId is not null &&
                IsInProgress(x.Status))
            .Select(x => x.CashAdvanceId!.Value)
            .ToHashSet();
    }

    private async Task<HashSet<Guid>> GetRelatedCashAdvanceIdsAsync(
        CancellationToken cancellationToken)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        return reports
            .Where(x =>
                x.PaymentMethod == ExpensePaymentMethod.PersonalAdvance &&
                x.CashAdvanceId is not null)
            .Select(x => x.CashAdvanceId!.Value)
            .ToHashSet();
    }

    /// <summary>
    /// 每筆預支款上有幾張已被主管作廢的報銷單。作廢的單子已不再算進「已核准報銷」金額，
    /// 但已計入的結清紀錄不會自動動——預支款詳情頁 &gt; 0 時要顯示提示，讓主管去手動處理。
    /// </summary>
    private async Task<Dictionary<Guid, int>> GetVoidedRelatedReportCountsAsync(
        CancellationToken cancellationToken)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        return reports
            .Where(x =>
                x.PaymentMethod == ExpensePaymentMethod.PersonalAdvance &&
                x.CashAdvanceId is not null &&
                x.Status == ExpenseReportStatus.Voided)
            .GroupBy(x => x.CashAdvanceId!.Value)
            .ToDictionary(x => x.Key, x => x.Count());
    }

    private async Task<bool> HasExpenseReportReferencesAsync(
        Guid cashAdvanceId,
        CancellationToken cancellationToken)
    {
        var reports = await _reports.ListAsync(cancellationToken);

        return reports.Any(x =>
            x.PaymentMethod == ExpensePaymentMethod.PersonalAdvance &&
            x.CashAdvanceId == cashAdvanceId);
    }

    private async Task<(int TotalCashAdvanceCount, IReadOnlyList<CashAdvanceListItemDto> Items)> BuildListItemsAsync(
        CurrentUser viewer,
        CancellationToken cancellationToken)
    {
        var cashAdvances = await _cashAdvances.ListAsync(cancellationToken);
        var approvedAmounts = await GetApprovedAmountsByCashAdvanceAsync(cancellationToken);
        var inProgressCashAdvanceIds = await GetInProgressCashAdvanceIdsAsync(cancellationToken);

        // 先過濾可見度再算總筆數，「還沒有預支款」和「篩選條件沒中」才不會被別人的資料混淆。
        var visible = cashAdvances
            .Where(x => CanBeViewedBy(x, viewer))
            .ToList();

        var items = visible
            .OrderByDescending(x => x.AdvancedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => MapListItem(
                x,
                approvedAmounts.GetValueOrDefault(x.Id),
                inProgressCashAdvanceIds.Contains(x.Id)))
            .ToList();

        return (visible.Count, items);
    }

    private static CashAdvanceListItemDto MapListItem(
        CashAdvance cashAdvance,
        decimal approvedReimbursedAmount,
        bool hasInProgressReports)
    {
        var settlement = BuildSettlementSummary(cashAdvance, approvedReimbursedAmount);

        return new CashAdvanceListItemDto(
            cashAdvance.Id,
            cashAdvance.PayeeUserId,
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
        bool hasInProgressReports,
        bool hasRelatedReports,
        int voidedRelatedReportCount)
    {
        var settlement = BuildSettlementSummary(cashAdvance, approvedReimbursedAmount);
        var isSettled = settlement.ReconciliationStatus == CashAdvanceReconciliationStatus.Settled;
        var canEdit = !isSettled;
        var hasAdoptedSettlementRecords = cashAdvance.SettlementRecords.Any(x => !x.IsVoided);

        return new CashAdvanceSettlementDetailDto(
            cashAdvance.Id,
            cashAdvance.PayeeUserId,
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
            hasRelatedReports,
            voidedRelatedReportCount,
            canEdit,
            canEdit && !hasRelatedReports && !hasAdoptedSettlementRecords,
            settlement.ReconciliationStatus,
            cashAdvance.SettlementRecords
                .OrderByDescending(x => x.SettledAt)
                .ThenByDescending(x => x.CreatedAt)
                .Select(x => MapSettlementRecord(x, canEdit))
                .ToList());
    }

    private static CashAdvanceSettlementRecordDto MapSettlementRecord(
        CashAdvanceSettlementRecord record,
        bool canEditCashAdvance)
    {
        var canChange = canEditCashAdvance && !record.IsVoided;

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
