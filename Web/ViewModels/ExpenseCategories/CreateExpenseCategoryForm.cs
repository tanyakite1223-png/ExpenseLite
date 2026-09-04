using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.ExpenseCategories;

public sealed class CreateExpenseCategoryForm
{
    [Required(ErrorMessage = "請輸入類別名稱")]
    [StringLength(50, ErrorMessage = "類別名稱最多 50 個字")]
    public string Name { get; set; } = string.Empty;
}
