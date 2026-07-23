using ExpenseLite.Application.CashAdvances;

namespace ExpenseLite.Web.ViewModels.CashAdvances;

public sealed class EditCashAdvancePage
{
    public CashAdvanceSettlementDetailDto CashAdvance { get; set; } = default!;

    public EditCashAdvanceForm CashAdvanceForm { get; set; } = new();
}
