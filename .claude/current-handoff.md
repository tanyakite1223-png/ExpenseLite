# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-15 — 桌機開發環境從 portable 切換成 scoop

---

## 上次 session 摘要

- 把桌機的 PostgreSQL 開發環境從「repo 內 portable binaries」改成「scoop 安裝」,主要動機是不想把 DB 資料放在專案目錄內。
- 合併了筆電先前推的 `chore/dev-env-scoop` 分支(merge commit),帶進 `scripts/dev/Resolve-PgEnv.ps1`(自動判斷 scoop / portable)與 scoop 環境筆記。
- 桌機實際切換內容:
  - `scoop install postgresql`(18.4-2);安裝腳本會自動 `initdb`,data 目錄是 junction → `~\scoop\persist\postgresql\data`(專案外)。
  - 停掉舊的 portable postgres,把 `.devtools/` → `.devtools.bak/`、`.devdata/` → `.devdata.bak/`(改名保留,未刪除)。
  - 建立 `expenselite_dev`(UTF8 / C locale)與 `expenselite_app` role,密碼用 `\password` 設定(未經過 AI)。
  - `pg_hba.conf` 全部改成 `scram-sha-256` 並 reload;無密碼連線已驗證會被拒。
  - `dotnet ef database update` 成功套用兩個 migration → 整條連線鏈(密碼、權限、UTF8)驗證通過。
- 另一台筆電的 `chore/laptop-postgres-env` 分支**未合併**(方向是把 DB 放在專案內的 `.localdb`,與這次決定相反);其中有用的兩點已手動 fold 進 main:`Stop-Postgres.ps1` 改用 `postmaster.pid` 停程序、portable `initdb` 補 `--locale=C`。

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
  - `dotnet build` 成功。
  - 本機 scoop PostgreSQL 已可用,`dotnet ef database update` 已跑過。
  - 本機連線字串透過 user secrets 設定,不寫入 repo。
- 開發 DB(桌機,scoop):
  - 來源:`scoop install postgresql`(18.4-2)。
  - binaries:`~\scoop\apps\postgresql\current\bin`。
  - data directory:`~\scoop\apps\postgresql\current\data`(junction → `~\scoop\persist\postgresql\data`,專案外)。
  - database:`expenselite_dev`(UTF8 / C);application user:`expenselite_app`;認證 `scram-sha-256`。
  - 舊的 portable 環境已改名為 `.devtools.bak/`、`.devdata.bak/`(未刪除,確認 scoop 穩定後可自行清掉)。
- 開發環境筆記:
  - `docs/development/postgresql-windows-scoop.md`(目前主用)
  - `docs/development/postgresql-windows-portable.md`(較早做法,留作備援)
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
  - 無明顯偏離(本次只動開發環境,未動應用程式架構)。
  - EF Core tool 版本 `10.0.7` 比 runtime `10.0.9` 舊,只有提示,未阻擋 migration / build。

## 待 Amber 決定 / 待辦

- 應用功能:下一個方向建議從審核流程開始(核准 / 退回 / 拒絕),也可先做列表篩選、刪除草稿、編輯基本資料或 UI 改善。
- 開發環境(跨機器):
  - 另一台筆電(目前 checkout 在 `chore/laptop-postgres-env`)要對齊:先 `git checkout main && git pull` 拿到 scoop 支援,再重做一次 scoop 目錄的 initdb / 建 role / 建 UTF8 db(等同這次桌機 Phase 2 的縮小版);舊的 `.localdb/` 在 main 上不再被 ignore,驗證後可刪。
  - 兩台都切到 scoop 之後,再清掉遠端分支 `chore/dev-env-scoop`(已合併)與 `chore/laptop-postgres-env`(已榨乾價值)。刪遠端分支屬不可逆操作,先確認再動。
  - 桌機確認 scoop 穩定後,可刪 `.devtools.bak/`、`.devdata.bak/`。

## 下一步建議

- 應用面優先做「審核流程 UI」:
  - 在詳細頁依狀態顯示核准 / 退回 / 拒絕按鈕
  - Application Service 呼叫 `report.Approve()` / `report.Return()` / `report.Reject()`
  - 補必要的頁面訊息與錯誤處理

## git 狀態

- 本 session 的 commit(待 push):
  - `merge: 併入 scoop / portable 雙支援的開發環境腳本`
  - `refactor: Stop-Postgres 改用 postmaster.pid 精準停 postgres`
  - `docs: scoop / portable 開發環境筆記對齊實際流程`
  - `docs: 更新 handoff — 桌機切換 scoop 開發環境`
- 遠端分支:`origin/chore/dev-env-scoop`(已合併)、`origin/chore/laptop-postgres-env`(未合併,待處理)。
