# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-17 — 預支款核對與明細單據欄位

---

## 本次 session 摘要

- 先驗證本機狀態:
  - `dotnet build` 成功。
  - PostgreSQL 一開始未啟動,執行 `scripts/dev/Start-Postgres.ps1` 後,`dotnet ef database update` 可連線成功。
- 新增「預支款核對」最小功能:
  - 新增 `CashAdvance` aggregate root,記錄領款人、用途、預支日期、預支金額。
  - 報銷單新增付款方式:`EmployeePaid`(員工墊款)、`CashAdvance`(預支費用)。
  - 預支費用報銷單必須連到一筆預支款。
  - 預支款列表顯示預支金額、已核准報銷金額、差額與核對狀態。
  - 範圍刻意只做到「可追蹤、可和報銷單核銷金額核對、月底可查未結清」,沒有做完整會計總帳或現金補繳/補付紀錄。
- 新增「明細單據欄位」:
  - 明細新增單據類型:`Receipt`(收據)、`Invoice`(發票)。
  - 單據類型選發票時,發票號碼必填。
  - 明細列表與新增明細表單已顯示單據類型 / 發票號碼。
- 新增 EF Core migrations:
  - `20260717064836_AddCashAdvances`
  - `20260717072257_AddExpenseDetailReceiptFields`
  - 兩個 migration 已用 `dotnet ef database update` 套到本機 PostgreSQL。
- 新增架構文件:
  - `docs/architecture/cash-advance-reconciliation.md`
- `.gitignore` 新增 `.localdb/`,避免本機 DB 資料夾進 Git。

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
- 進行中 / 未完成:
  - 一般支出 / 專案支出的分類尚未做。
  - `Project` 專案資料與結案封存尚未做。
  - 尚未有使用者 / 角色 / 權限,目前仍手動輸入申請人。
  - 尚未有附件或發票照片上傳。
  - 審核流程尚未記錄審核人、審核時間、退回/拒絕原因。
  - 預支款尚未記錄員工實際繳回或公司實際補付,目前只做金額核對。
  - 列表篩選尚未做。
  - UI 仍是基礎 Bootstrap 版。
- build & DB:
  - `dotnet build` 成功。
  - `dotnet ef database update` 成功,已套用最新 migration。
  - 本機連線字串透過 user secrets 設定,不寫入 repo。

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:
  - Controller → Application Service → Domain → Infrastructure
  - Rich Domain Model
  - `ExpenseReport` Aggregate Root
  - `ExpenseDetail` 作為 aggregate 內部 entity
  - `CashAdvance` Aggregate Root
  - `Money` Value Object
  - `IExpenseReportRepository`
  - `ICashAdvanceRepository`
  - EF Core owned type mapping
- `/docs/architecture/` 已有的篇章:
  - `layered-architecture.md`
  - `expense-report-aggregate.md`
  - `money-value-object.md`
  - `repository-and-ef-core.md`
  - `cash-advance-reconciliation.md`
- 有無偏離 CLAUDE.md 規範(技術債):
  - 無明顯偏離。
  - `CashAdvance` 是獨立 aggregate root;報銷單只用 `CashAdvanceId` 參照,符合跨 aggregate 用 ID 的規範。
  - 預支款核對目前由 Application Service 查詢報銷單與預支款後組成 DTO,沒有把跨 aggregate 查詢邏輯塞進 Domain entity。

## 開發環境狀態

- 開發 DB(桌機,scoop):
  - 來源:`scoop install postgresql`。
  - binaries:`~\scoop\apps\postgresql\current\bin`。
  - data directory:`~\scoop\apps\postgresql\current\data`(junction → `~\scoop\persist\postgresql\data`,專案外)。
  - database:`expenselite_dev`;application user:`expenselite_app`。
- repo 內本機資料:
  - `.localdb/` 仍可能存在於本機,但 `.gitignore` 已忽略,不會進 Git。
  - 舊的 `.devtools.bak/`、`.devdata.bak/` 若確認 scoop 穩定,之後可由 Amber 決定是否刪除。

## 待 Amber 決定 / 待辦

- 應用面下一步建議:
  - 優先做「一般支出 / 專案支出」與 `Project` 專案資料。
  - 接著做專案結案後,相關報銷單封存供查詢。
- 可補強:
  - 審核人、審核時間、退回/拒絕原因。
  - 預支款實際結清紀錄(員工已繳回 / 公司已補付),但目前不是必要範圍。
  - 附件或發票照片上傳。
  - 報銷單 / 預支款列表篩選。
- 開發環境:
  - 另一台筆電若尚未對齊 scoop PostgreSQL,仍需拉 main 後重建 scoop 方向的本機 DB。
  - 兩台都穩定後,再考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`。刪遠端分支屬不可逆操作,先確認再動。

## git 狀態

- 本 session 已完成 commit:
  - `1ed45b7 feat: 新增預支款核對與明細單據欄位`
  - `e2dbceb chore: 忽略本機 localdb 資料夾`
- 更新本 handoff 後,建議再 commit:
  - `docs: 更新 handoff — 預支款與單據欄位`
- 目前 `main` 比 `origin/main` ahead 2(在 handoff commit 前);完成 handoff commit 後會 ahead 3。
- 尚未 push。push 前需依 CLAUDE.md 規定列出指令、範圍並取得 Amber 確認。
