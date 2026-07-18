# 分層架構在 ExpenseLite 怎麼用

ExpenseLite 目前維持單一 `.csproj`，但用資料夾分出 `Web`、`Application`、`Domain`、`Infrastructure`。

- `Web`：Controller、ViewModel、Razor View。只接 HTTP 與表單，不直接操作 `DbContext`。
- `Application`：use case 編排，例如建立報銷單、加入明細、送審。它呼叫 domain 方法，不重寫業務規則。
- `Domain`：報銷單、明細、金額與狀態轉換規則。這層不依賴 EF Core。
- `Infrastructure`：EF Core `DbContext`、mapping、repository 實作，負責把 domain 存到 PostgreSQL。

目前的依賴方向是 Web 呼叫 Application，Application 依賴 Domain 與 repository 介面，Infrastructure 實作 repository 並被 DI 接上。

## Web 層為什麼集中在 `/Web`

ASP.NET Core MVC 預設會把 Controller 放在 `/Controllers`、Razor View 放在 `/Views`。本專案因為要在單一 `.csproj` 裡練習 DDD 分層，所以改成把 Web 層相關檔案集中在 `/Web`：

- `/Web/Controllers`：接 HTTP request，呼叫 Application Service。
- `/Web/ViewModels`：表單與頁面用的模型，不是 Domain Model。
- `/Web/Views`：Razor 畫面，只呈現資料與送出表單。

`/Web/Views` 不是 MVC 預設路徑，所以 `Program.cs` 有設定 Razor view location，讓 MVC 從 `/Web/Views/{Controller}/{Action}.cshtml` 與 `/Web/Views/Shared/{View}.cshtml` 找畫面。
