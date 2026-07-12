# 分層架構在 ExpenseLite 怎麼用

ExpenseLite 目前維持單一 `.csproj`，但用資料夾分出 `Web`、`Application`、`Domain`、`Infrastructure`。

- `Web`：Controller、ViewModel、Razor View。只接 HTTP 與表單，不直接操作 `DbContext`。
- `Application`：use case 編排，例如建立報銷單、加入明細、送審。它呼叫 domain 方法，不重寫業務規則。
- `Domain`：報銷單、明細、金額與狀態轉換規則。這層不依賴 EF Core。
- `Infrastructure`：EF Core `DbContext`、mapping、repository 實作，負責把 domain 存到 PostgreSQL。

目前的依賴方向是 Web 呼叫 Application，Application 依賴 Domain 與 repository 介面，Infrastructure 實作 repository 並被 DI 接上。
