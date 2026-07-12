# Repository 與 EF Core

本專案目前只有一個 aggregate root repository：`IExpenseReportRepository`。

Controller 不會直接碰 `DbContext`，Application Service 也只依賴 repository 介面。真正的 EF Core 實作放在 `EfExpenseReportRepository`，載入報銷單時會一起載入明細，因為明細隸屬於報銷單 aggregate。

這裡要誠實看待 tradeoff：EF Core 的 `DbContext` 本身已經有 Unit of Work 與 Repository 的味道。本專案再包一層 repository，是為了讓 Application 層不要直接依賴 EF Core，也讓持久化抽象和 DI 配置更清楚。
