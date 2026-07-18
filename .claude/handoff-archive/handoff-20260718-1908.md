# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-18 — 專案支出與專案資料

---

## 本次 session 摘要

- 先確認開發環境與 handoff 狀態:
  - 重新讀到 2026-07-17 版本 handoff,內容包含預支款核對與明細單據欄位。
  - 確認桌機 PostgreSQL 目前使用 scoop,DB data 不在專案內。
  - 實際 data 位置:`C:\Users\Pinecone\scoop\persist\postgresql\data`。
  - repo 內只剩舊 portable 備份 `.devtools.bak/`、`.devdata.bak/`,沒有被目前 PostgreSQL 腳本使用。
- 新增「一般支出 / 專案支出」最小功能:
  - 新增 `Project` aggregate root,目前欄位為專案名稱、客戶名稱、狀態、建立時間。
  - 新增 `ProjectStatus`:目前有 `Active`、`Closed`,但 UI 尚未提供結案動作。
  - 新增專案列表與新增專案頁面:`/Projects`。
  - 報銷單新增 `ExpenseType`: `General`(一般支出)、`Project`(專案支出)。
  - 報銷單新增 `ProjectId`;專案支出必須選專案,一般支出不可連到專案。
  - Application Service 會檢查使用者選到的 `ProjectId` 是否存在,且專案狀態為 `Active`。
  - 報銷單建立頁、列表頁、詳細頁已顯示支出類型與專案。
- 新增 EF Core migration:
  - `20260718050858_AddProjectsAndExpenseType`
  - migration 新增 `projects` table,並在 `expense_reports` 加上 `expense_type`、`project_id`。
  - `expense_type` 對既有資料預設為 `General`,避免舊報銷單讀取 enum 時出問題。
  - `dotnet ef database update` 成功;本次桌機 DB 也一起補套了 `AddCashAdvances`、`AddExpenseDetailReceiptFields` 兩個尚未套用的 migration。
- 新增架構文件:
  - `docs/architecture/project-expense-reference.md`
- `.gitignore` 補上 `.devtools.bak/`、`.devdata.bak/`,避免舊 PostgreSQL 備份資料夾被誤納入 git。
- 驗證:
  - `dotnet build` 成功,0 warning / 0 error。
  - `dotnet ef migrations list` 可看到最新 migration。
  - 曾啟動 dev server 並檢查 `/`、`/Projects`、`/ExpenseReports/Create` 都回 200。
  - 後續 Amber 執行 `dotnet run` 遇到 `MSB3027` / `MSB3021`;原因是背景 `ExpenseLite.exe` 鎖住 build output,已停掉該 dev server,再次 `dotnet build` 成功。

## 專案現況

- 已完成功能:
  - 報銷單列表
  - 新增報銷單
  - 報銷單詳細頁
  - 新增 / 移除明細
  - 明細金額加總到報銷單總額
  - 草稿報銷單送審
  - 核准 / 退回 / 拒絕流程 UI
  - 預支款建立與核對列表
  - 報銷單付款方式:員工墊款 / 預支費用
  - 明細單據類型:收據 / 發票,且發票號碼必填
  - 專案建立與專案列表
  - 報銷單支出類型:一般支出 / 專案支出
  - 專案支出報銷單可連到一筆 `Project`
- 進行中 / 未完成:
  - `Project` 結案動作尚未做。
  - 專案結案後,相關報銷單封存供查詢尚未做。
  - 尚未有使用者 / 角色 / 權限,目前仍手動輸入申請人。
  - 尚未有附件或發票照片上傳。
  - 審核流程尚未記錄審核人、審核時間、退回/拒絕原因。
  - 預支款尚未記錄員工實際繳回或公司實際補付,目前只做金額核對。
  - 報銷單 / 預支款 / 專案列表篩選尚未做。
  - UI 仍是基礎 Bootstrap 版。
- build & DB:
  - `dotnet build` 成功。
  - `dotnet ef database update` 成功,桌機本機 DB 已套用最新 migration。
  - 本機連線字串透過 user secrets 設定,不寫入 repo。
  - dev server 目前已停掉;要跑網站可重新執行 `dotnet run`。

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:
  - Controller → Application Service → Domain → Infrastructure
  - Rich Domain Model
  - `ExpenseReport` Aggregate Root
  - `ExpenseDetail` 作為 aggregate 內部 entity
  - `CashAdvance` Aggregate Root
  - `Project` Aggregate Root
  - `Money` Value Object
  - `IExpenseReportRepository`
  - `ICashAdvanceRepository`
  - `IProjectRepository`
  - EF Core owned type mapping
- `/docs/architecture/` 已有的篇章:
  - `layered-architecture.md`
  - `expense-report-aggregate.md`
  - `money-value-object.md`
  - `repository-and-ef-core.md`
  - `cash-advance-reconciliation.md`
  - `project-expense-reference.md`
- 有無偏離 CLAUDE.md 規範(技術債):
  - 無明顯偏離。
  - `CashAdvance` 是獨立 aggregate root;報銷單只用 `CashAdvanceId` 參照,符合跨 aggregate 用 ID 的規範。
  - `Project` 是獨立 aggregate root;報銷單只用 `ProjectId` 參照,沒有把整顆 Project 放進報銷單 aggregate。
  - `ExpenseReport` 自己守支出類型與 `ProjectId` 的 domain rule;Application Service 只負責查 repository 確認 Project 是否存在且可用。
  - 預支款核對目前由 Application Service 查詢報銷單與預支款後組成 DTO,沒有把跨 aggregate 查詢邏輯塞進 Domain entity。

## 開發環境狀態

- 開發 DB(桌機,scoop):
  - 來源:`scoop install postgresql`。
  - binaries:`~\scoop\apps\postgresql\current\bin`。
  - data directory:`~\scoop\apps\postgresql\current\data`(junction → `~\scoop\persist\postgresql\data`,專案外)。
  - database:`expenselite_dev`;application user:`expenselite_app`。
- repo 內本機資料:
  - `.localdb/`、`.devtools.bak/`、`.devdata.bak/` 已在 `.gitignore` 忽略,不會進 Git。
  - 舊的 `.devtools.bak/`、`.devdata.bak/` 若確認 scoop 穩定,之後可由 Amber 決定是否刪除。
- 跨機器注意:
  - 筆電 pull 最新 `main` 後,若 DB 尚未套最新 migration,需執行 `dotnet ef database update`。
  - 筆電 DB 原則同樣是不放在專案資料夾內。

## 待 Amber 決定 / 待辦

- 應用面下一步建議:
  - 優先做「專案結案」:在 Project 上提供結案動作,結案後不可再新增專案支出報銷單。
  - 接著做「專案結案後封存 / 查詢相關報銷單」。
- 可補強:
  - 審核人、審核時間、退回/拒絕原因。
  - 預支款實際結清紀錄(員工已繳回 / 公司已補付),但目前不是必要範圍。
  - 附件或發票照片上傳。
  - 報銷單 / 預支款 / 專案列表篩選。
- 開發環境:
  - 兩台都穩定後,再考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`。刪遠端分支屬不可逆操作,先確認再動。
  - 桌機舊 portable 備份 `.devtools.bak/`、`.devdata.bak/` 仍保留;刪除前需 Amber 明確確認。

## git 狀態

- 本 session 已完成 commit 並 push:
  - `48bb117 feat: 新增專案支出與專案資料`
- 更新本 handoff 後,建議再 commit:
  - `docs: 更新 handoff — 專案支出與專案資料`
- push 前仍需依 CLAUDE.md 規定列出指令、範圍並取得 Amber 確認。
