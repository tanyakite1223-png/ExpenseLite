using ExpenseLite.Application.CashAdvances;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class CashAdvanceSettlementPage
{
    public CashAdvanceSettlementDetailDto CashAdvance { get; set; } = default!;

    public RecordCashAdvanceSettlementForm Settlement { get; set; } = new();
}
