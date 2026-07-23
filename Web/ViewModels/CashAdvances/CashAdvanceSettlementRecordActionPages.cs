using ExpenseLite.Application.CashAdvances;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class EditCashAdvanceSettlementPage
{
    public CashAdvanceSettlementDetailDto CashAdvance { get; set; } = default!;

    public CashAdvanceSettlementRecordDto SettlementRecord { get; set; } = default!;

    public EditCashAdvanceSettlementForm Settlement { get; set; } = new();
}

public sealed class VoidCashAdvanceSettlementPage
{
    public CashAdvanceSettlementDetailDto CashAdvance { get; set; } = default!;

    public CashAdvanceSettlementRecordDto SettlementRecord { get; set; } = default!;

    public VoidCashAdvanceSettlementForm Settlement { get; set; } = new();
}
