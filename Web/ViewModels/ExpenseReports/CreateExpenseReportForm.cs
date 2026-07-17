using System.ComponentModel.DataAnnotations;
using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class CreateExpenseReportForm
{
    [Required(ErrorMessage = "請輸入報銷單標題")]
    [StringLength(100, ErrorMessage = "標題最多 100 個字")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入申請人")]
    [StringLength(50, ErrorMessage = "申請人最多 50 個字")]
    public string ApplicantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇付款方式")]
    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.EmployeePaid;

    public Guid? CashAdvanceId { get; set; }

    public IReadOnlyList<CashAdvanceOptionDto> CashAdvanceOptions { get; set; } = [];
}
