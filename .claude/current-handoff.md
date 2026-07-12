# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-12 — v0.1 報銷單基礎流程完成

---

## 上次 session 摘要

- 已建立 ASP.NET Core MVC 專案骨架,使用 .NET 10。
- 已接上 PostgreSQL + EF Core + Npgsql。
- 已完成第一版報銷單基礎流程:建立報銷單、檢視報銷單、加入明細、總金額加總、送審。
- 已修正 EF Core 對 domain-assigned Guid 的誤判問題,避免新增明細時被當成 UPDATE。
- 已 commit 並 push:`739bdce feat: 建立報銷單基礎流程`。

## 專案現況

- 已完成功能:
  - 報銷單列表
  - 新增報銷單
  - 報銷單詳細頁
  - 新增 / 移除明細
  - 明細金額加總到報銷單總額
  - 草稿報銷單送審
- 進行中 / 未完成:
  - 核准 / 退回 / 拒絕流程尚未做 UI
  - 尚未有使用者 / 角色 / 權限
  - 尚未有附件或發票照片上傳
  - UI 仍是基礎 Bootstrap 版
- build & 跑得起來嗎:
  - `dotnet build` 成功,0 errors / 0 warnings。
  - 本機 PostgreSQL 已可用。
  - 本機連線字串已透過 user secrets 設定,不寫入 repo。
  - 收工時已停止 ASP.NET Core dev server。
- 開發 DB:
  - portable PostgreSQL 18.4 已放在 `.devtools/postgresql-18.4/`(git ignored)。
  - 資料目錄在 `.devdata/postgres/`(git ignored)。
  - database:`expenselite_dev`;application user:`expenselite_app`。
- 開發環境筆記:
  - `docs/development/postgresql-windows-portable.md`
  - `docs/development/aspnet-core-local-run.md`

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:
  - Controller → Application Service → Domain → Infrastructure
  - Rich Domain Model
  - `ExpenseReport` Aggregate Root
  - `ExpenseDetail` 作為 aggregate 內部 entity
  - `Money` Value Object
  - `IExpenseReportRepository`
  - EF Core owned type mapping
- `/docs/architecture/` 已有的篇章:
  - `layered-architecture.md`
  - `expense-report-aggregate.md`
  - `money-value-object.md`
  - `repository-and-ef-core.md`
- 有無偏離 CLAUDE.md 規範(技術債):
  - 無明顯偏離。
  - EF Core tool 版本 `10.0.7` 比 runtime `10.0.9` 舊,只有提示,未阻擋 migration / build。
  - git 會顯示使用者層級 global ignore 權限 warning,不影響 repo 操作。

## 待 Amber 決定 / 待辦

- 下一個功能方向建議從審核流程開始:
  - 送審中的報銷單可以核准
  - 可以退回
  - 可以拒絕
- 也可以先做列表篩選、刪除草稿、編輯基本資料或 UI 改善。

## 下一步建議

- 優先做「審核流程 UI」:
  - 在詳細頁依狀態顯示核准 / 退回 / 拒絕按鈕
  - Application Service 呼叫 `report.Approve()` / `report.Return()` / `report.Reject()`
  - 補必要的頁面訊息與錯誤處理

## git 狀態

- 最後 commit:`739bdce feat: 建立報銷單基礎流程`
- 已 push 到 `origin/main`
- 有無未 commit 的變更:目前只有本次 handoff 更新待 commit
