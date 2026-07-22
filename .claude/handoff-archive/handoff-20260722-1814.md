# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔,結束時更新此檔(舊內容歸檔到 `.claude/handoff-archive/`)。
> 內容聚焦「專案現況 + 架構狀態」,不是學習進度。

> 最後更新:2026-07-22 — 報銷單審核紀錄與退回後明細修改

---

## 本次 session 摘要

- 本次完成「報銷單審核紀錄」:
  - 新增 `ExpenseReviewRecord` 與 `ExpenseReviewAction`,作為 `ExpenseReport` aggregate 內部 entity。
  - `ExpenseReport.Return(...)` / `Approve(...)` / `Reject(...)` 現在會在狀態轉換時一併新增審核紀錄。
  - 審核紀錄包含審核動作、審核人、審核時間、退回/拒絕原因。
  - 退回與拒絕原因由 domain rule 擋必填;核准可不填原因。
  - 詳細頁新增「審核處理」表單,目前審核人先手動輸入,尚未做登入/權限。
  - 詳細頁新增「審核紀錄」列表。
- 本次也補齊「退回後修改明細」:
  - 原本退回後只能新增/移除明細,沒有修改既有明細的入口。
  - 新增 `ExpenseReport.UpdateDetail(...)`,仍由 aggregate root 管理明細修改並重新計算總額。
  - 詳細頁明細列在草稿/退回狀態會出現「修改」按鈕。
  - 點修改後右側表單會切換為「修改明細」並預填資料。
- DB / migration:
  - 新增 migration `20260722040215_AddExpenseReviewRecords`。
  - 新增資料表 `expense_review_records`,以 FK 連到 `expense_reports`,刪除報銷單時 cascade。
  - 桌機本機 DB 已執行 `dotnet ef database update` 並套用成功。
- 文件:
  - 更新 `docs/architecture/expense-report-aggregate.md`,補充審核紀錄與明細修改仍屬於 `ExpenseReport` aggregate。
- 驗證:
  - `dotnet build` 成功,0 warning / 0 error。
  - `dotnet ef database update` 成功。
  - dev server smoke test 成功:
    - `/ExpenseReports` 回 200。
    - `/ExpenseReports/Details/{id}` 回 200。
    - `/ExpenseReports/Details/{id}?editDetailId={detailId}` 回 200。
  - Amber 實測拒絕流程正常。
  - Amber 實測退回後明細修改缺口已補上,後續實測正常。
  - 測試用 dev server 已停掉,5080 沒有殘留佔用。
- 本次沒有新增 NuGet 套件。

## 專案現況

- 已完成功能:
  - 報銷單列表
  - 新增報銷單
  - 報銷單詳細頁
  - 修改草稿 / 退回報銷單主檔欄位
  - 新增 / 修改 / 移除草稿或退回報銷單的明細
  - 明細金額加總到報銷單總額
  - 草稿 / 退回報銷單送審
  - 核准 / 退回 / 拒絕流程 UI
  - 核准 / 退回 / 拒絕時記錄審核人、審核時間、審核動作
  - 退回 / 拒絕時記錄原因
  - 報銷單詳細頁顯示審核紀錄
  - 預支款建立與核對列表
  - 報銷單付款方式:員工墊款 / 預支費用
  - 明細單據類型:收據 / 發票,且發票號碼必填
  - 專案建立與專案列表
  - 報銷單支出類型:一般支出 / 專案支出
  - 專案支出報銷單可連到一筆 `Project`
  - 專案結案
  - 專案有未完成報銷單時不可結案
  - 已結案專案不可新增專案支出報銷單,既有草稿 / 退回單也不可送審
  - 專案列表 keyword 查詢
  - 專案詳情頁可查詢該專案全部相關報銷單,結案後仍可作為歷史查詢
- 進行中 / 未完成:
  - 尚未有使用者 / 角色 / 權限,目前仍手動輸入申請人與審核人。
  - 尚未有附件或發票照片上傳。
  - 審核流程尚未記錄真正的登入使用者,也尚未限制誰可以核准/退回/拒絕。
  - 預支款尚未記錄員工實際繳回或公司實際補付,目前只做金額核對。
  - 報銷單 / 預支款列表篩選尚未做。
  - UI 仍是基礎 Bootstrap 版。
- build & DB:
  - `dotnet build` 成功。
  - `dotnet ef database update` 已成功,桌機本機 DB 已套用最新 migration。
  - 本機連線字串透過 user secrets 設定,不寫入 repo。
  - dev server 目前已停掉;要跑網站可重新執行 `dotnet run`。

## 架構狀態

> 跨 session 防止架構飄移的錨點,務必維護。

- 已落地的 pattern:
  - Controller → Application Service → Domain → Infrastructure
  - Rich Domain Model
  - `ExpenseReport` Aggregate Root
  - `ExpenseDetail` 作為 aggregate 內部 entity
  - `ExpenseReviewRecord` 作為 `ExpenseReport` aggregate 內部 entity
  - `CashAdvance` Aggregate Root
  - `Project` Aggregate Root
  - `Money` Value Object
  - `IExpenseReportRepository`
  - `ICashAdvanceRepository`
  - `IProjectRepository`
  - EF Core owned type mapping
- Web 層目錄慣例:
  - `/Web/Controllers`:MVC Controller,只接 HTTP、model binding、呼叫 Application Service、回傳 View。
  - `/Web/ViewModels`:表單與頁面模型,不是 Domain Model。
  - `/Web/Views`:Razor View,只負責畫面呈現與表單送出。
  - 根目錄不再保留 `/Controllers`、`/Models`、`/Views`。
  - `Program.cs` 有客製 Razor view location;之後新增 Razor View 要放在 `/Web/Views`。
- `/docs/architecture/` 已有的篇章:
  - `layered-architecture.md`
  - `expense-report-aggregate.md`
  - `money-value-object.md`
  - `repository-and-ef-core.md`
  - `cash-advance-reconciliation.md`
  - `project-expense-reference.md`
- 有無偏離 CLAUDE.md 規範(技術債):
  - 無明顯偏離。
  - `ExpenseReport.UpdateBasicInfo(...)` 屬於單一 aggregate 自己的狀態 / invariant,所以放在 domain entity,不是放在 Controller 或 Application Service。
  - `ExpenseReport.UpdateDetail(...)` 屬於 aggregate 內部明細操作,由 root 控制可修改狀態並重新計算總額。
  - `ExpenseReport.Return(...)` / `Approve(...)` / `Reject(...)` 屬於報銷單狀態轉換,並同步建立審核紀錄。
  - `ExpenseReportAppService` 只做 use case 編排與跨 aggregate 查詢:Project / CashAdvance 是否存在、Project 是否仍可用。
  - `ExpenseDetail` 與 `ExpenseReviewRecord` 仍只透過 `ExpenseReport` 操作,沒有獨立 repository。
  - `CashAdvance` 是獨立 aggregate root;報銷單只用 `CashAdvanceId` 參照,符合跨 aggregate 用 ID 的規範。
  - `Project` 是獨立 aggregate root;報銷單只用 `ProjectId` 參照,沒有把整顆 Project 放進報銷單 aggregate。
  - `Project.Close()` 負責單一 aggregate 的狀態轉換;「專案是否仍有未完成報銷單」與「報銷單送審 / 修改時專案是否已結案」屬跨 aggregate 查詢,放在 Application Service。
  - 專案詳情頁的相關報銷單查詢同樣放在 Application Service,由 `ProjectId` 查詢 `ExpenseReport`;沒有把報銷單放進 `Project` aggregate,也沒有新增封存資料表。
  - 專案列表 keyword 查詢是查詢 / 呈現需求,放在 Application Service 組 DTO,沒有進 Domain entity。
  - 預支款核對目前由 Application Service 查詢報銷單與預支款後組成 DTO,沒有把跨 aggregate 查詢邏輯塞進 Domain entity。
  - 本次未做登入/權限,審核人先由表單手動輸入;未來可升級為目前登入者的 `UserId`。

## 開發環境狀態

- 開發 DB(桌機,scoop):
  - 來源:`scoop install postgresql`。
  - binaries:`~\scoop\apps\postgresql\current\bin`。
  - data directory:`~\scoop\apps\postgresql\current\data`(junction → `~\scoop\persist\postgresql\data`,專案外)。
  - database:`expenselite_dev`;application user:`expenselite_app`。
- repo 內本機資料:
  - `.localdb`、`.devtools.bak`、`.devdata.bak` 已在 `.gitignore` 忽略,不會進 Git。
  - 舊的 `.devtools.bak`、`.devdata.bak` 若確認 scoop 穩定,之後可由 Amber 決定是否刪除。
- 跨機器注意:
  - 筆電 pull 最新 `main` 後,若 DB 尚未套最新 migration,需執行 `dotnet ef database update`。
  - 筆電 DB 原則同樣是不放在專案資料夾內。
- 工具注意:
  - Codex sandbox 執行 git 時可能看到 `C:\Users\Pinecone/.config/git/ignore` permission warning。
  - Amber 本機 PowerShell 執行 `git status --short --branch` 沒有 warning。
  - 目前判斷 warning 是 Codex sandbox 讀不到 repo 外 global ignore,不影響本 repo 已追蹤檔案與 commit / push 狀態判讀。
  - 之後 handoff 的 git 狀態應寫明「跑過哪個指令、看到什麼結果、warning 是否已知」,避免留下互相矛盾的待 commit / 已同步敘述。

## 待 Amber 決定 / 待辦

- 應用面下一步建議:
  - 可補「報銷單 / 預支款列表篩選」,讓查詢能力更完整。
  - 可補「預支款實際結清紀錄」,記錄員工已繳回或公司已補付。
  - 可補「附件或發票照片上傳」,但會牽涉檔案儲存、安全性與大小限制。
  - 之後若 Amber 想把系統推進到多使用者,可另開「使用者 / 角色 / 權限」功能,再把審核人從手動輸入升級成登入者。
- 開發環境:
  - 兩台都穩定後,再考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`。刪遠端分支屬不可逆操作,先確認再動。
  - 桌機舊 portable 備份 `.devtools.bak`、`.devdata.bak` 仍保留;刪除前需 Amber 明確確認。

## git 狀態

- 已存在的最新 commit:
  - `c321202 feat: 新增報銷單審核紀錄`
- push 狀態:
  - `c321202` 已 push 到 `origin/main`。
  - `HEAD -> main`, `origin/main`, `origin/HEAD` 目前同在 `c321202`。
- handoff 更新前狀態確認:
  - Codex 執行 `git status --short --branch` 顯示 `## main...origin/main`。
  - 沒有列出未追蹤、未 staged、未 commit 的檔案。
  - 同一次查詢有出現已知的 `C:\Users\Pinecone/.config/git/ignore` permission warning,屬 repo 外 global ignore 讀取限制。
- 本次 handoff:
  - 舊 handoff 已歸檔到 `.claude/handoff-archive/handoff-20260722-1308.md`。
  - 本檔已更新為 `c321202` push 後的乾淨斷點。
  - 注意:因為本檔與 archive 是本次 handoff 更新產生的新變更,若要收工仍需另行 commit / push handoff 變更。
- 後續若有新變更,push 前仍需依 CLAUDE.md 規定列出指令、範圍並取得 Amber 確認。
