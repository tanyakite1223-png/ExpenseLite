using System.ComponentModel.DataAnnotations;
using ExpenseLite.Application.CashAdvances;
using ExpenseLite.Application.Projects;
using ExpenseLite.Domain.ExpenseReports;

namespace ExpenseLite.Web.ViewModels.ExpenseReports;

public sealed class EditExpenseReportForm
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "請輸入報銷單標題")]
    [StringLength(100, ErrorMessage = "標題最多 100 個字")]
    public string Title { get; set; } = string.Empty;

    /// <summary>只用來顯示，不會寫回報銷單——申請人是建立者，建立後就不再變動。</summary>
    public string ApplicantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇支出類型")]
    public ExpenseType ExpenseType { get; set; } = ExpenseType.General;

    public Guid? ProjectId { get; set; }

    [Required(ErrorMessage = "請選擇付款方式")]
    public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.EmployeePaid;

    public Guid? CashAdvanceId { get; set; }

    public IReadOnlyList<ProjectOptionDto> ProjectOptions { get; set; } = [];

    public IReadOnlyList<CashAdvanceOptionDto> CashAdvanceOptions { get; set; } = [];
}
