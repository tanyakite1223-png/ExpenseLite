using System.ComponentModel.DataAnnotations;

namespace ExpenseLite.Web.ViewModels.Projects;

public sealed class CreateProjectForm
{
    [Required(ErrorMessage = "請輸入專案名稱")]
    [StringLength(100, ErrorMessage = "專案名稱最多 100 個字")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入客戶名稱")]
    [StringLength(100, ErrorMessage = "客戶名稱最多 100 個字")]
    public string CustomerName { get; set; } = string.Empty;
}
