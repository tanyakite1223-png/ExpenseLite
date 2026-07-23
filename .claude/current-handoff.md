# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔，結束時更新此檔（舊內容歸檔到 `.claude/handoff-archive/`）。
> 內容聚焦「專案現況 + 架構狀態」，不是學習進度。

> 最後更新：2026-07-23 — 預支款結清紀錄修改與不採用（未 commit）

---

## 本次 session 摘要

- 本次原本實作「預支款結清紀錄修改 / 刪除 / 沖銷」，後續經 Amber 判斷不想把系統擴大成會計帳，已收斂為「修改 + 標記為不採用」。
- 已移除沖銷設計：
  - 不保留 `Reversal` / 沖銷紀錄型態。
  - 不做付款憑證、出納日記帳或會計帳務沖銷。
  - 先前短暫產生並套用過的 `AddCashAdvanceSettlementRecordActions` migration 已回退並用 `dotnet ef migrations remove` 移除。
- 目前實作內容：
  - `CashAdvanceSettlementRecord` 可修改結清日期、金額、處理人、備註。
  - `CashAdvanceSettlementRecord` 可標記為不採用，保留處理人、不採用原因、時間。
  - 不採用不是刪除資料；不採用紀錄仍顯示在預支款詳情頁歷史中。
  - 已結清金額只加總仍被採用的結清紀錄。
  - `尚待結清 = 應結清 - 已計入核對的有效已結清`。
  - 若同一筆預支款仍有 Draft / Submitted / Returned 的關聯報銷單，系統只顯示暫估核對，不允許新增最終結清紀錄。
- Application / 架構：
  - 修改與不採用仍透過 `CashAdvance` aggregate root 操作：`UpdateSettlementRecord(...)`、`VoidSettlementRecord(...)`。
  - `CashAdvanceSettlementRecord` 不建立獨立 repository，仍作為 `CashAdvance` aggregate 內部 entity。
  - 是否仍有流程中報銷單屬跨 aggregate 查詢，放在 `CashAdvanceAppService`，沒有塞進 domain entity。
  - 預支款核對金額仍由 `CashAdvanceAppService` 查 `CashAdvance` + `ExpenseReport` 後組 DTO。
- Infrastructure / DB：
  - 新增 migration `20260722145229_AddCashAdvanceSettlementRecordVoiding`。
  - `cash_advance_settlement_records` 新增：
    - `is_voided`
    - `voided_by`
    - `void_reason`
    - `voided_at`
    - `updated_at`
  - 桌機本機 DB 已執行 `dotnet ef database update`，最新 migration 已套用。
- Web / UI：
  - 預支款詳情頁的結清紀錄表新增狀態與操作。
  - 新增修改頁：`/CashAdvances/EditSettlement/{id}?settlementRecordId=...`
  - 新增「不採用此筆結清紀錄」頁：`/CashAdvances/VoidSettlement/{id}?settlementRecordId=...`
  - 預支款詳情頁結清紀錄狀態改為「已計入核對 / 不採用」，避免誤解為報銷單作廢。
  - 預支款列表若有流程中報銷單，會顯示「仍有流程中報銷單」與「查看暫估」。
  - 預支款詳情頁若有流程中報銷單，會顯示暫估提醒，並隱藏新增結清紀錄表單。
- 文件：
  - 更新 `docs/architecture/cash-advance-reconciliation.md`，明確寫本階段不做沖銷 / 會計帳，僅保留修改與不採用。
- 驗證：
  - `dotnet build` 成功，0 warning / 0 error。
  - `dotnet ef database update` 成功。
  - smoke test 成功：
    - `/CashAdvances` 回 200。
    - `/CashAdvances/Details/{id}` 回 200。
    - `/CashAdvances/EditSettlement/{id}?settlementRecordId=...` 回 200。
    - `/CashAdvances/VoidSettlement/{id}?settlementRecordId=...` 回 200。
    - 不採用表單空值 POST 回 200，會顯示 validation。
  - `git diff --check` 無 whitespace error；只有 Windows LF/CRLF 提醒。
  - dev server 已停；`netstat -ano | findstr :5080` 無 LISTENING，只可能短暫看到 TIME_WAIT / CLOSE_WAIT。
- 本次沒有新增 NuGet 套件。

## Amber 實測後的待討論點

- Amber 對「作廢」語意有疑問，已改用更白話的 UI 文案「不採用此筆結清紀錄」：
  - 目前定義：只是不採用這筆「結清紀錄」，不影響預支款、不影響報銷單。
  - 標記為不採用後不會改變 `已核准報銷`、`差額`、`應結清`。
  - 標記為不採用後會改變 `已結清`、`尚待結清`、核對狀態。
  - 詳情頁狀態欄目前顯示「已計入核對 / 不採用」。
  - 不採用頁已補說明：此動作只影響這筆結清紀錄，不影響預支款與報銷單。
- Amber 已理解「新增結清紀錄」預填 `尚待結清` 的意思：
  - 不管之前紀錄被修改或標記為不採用，最後有效結清紀錄加總仍要補足 `應結清`。
  - 可以分次結清；系統只擋超過尚待結清金額。
- 2026-07-23 釐清：如果問題是「已核准報銷單選錯預支款」或「已核准後因故要取消這張報銷單」，這不屬於結清紀錄不採用；需要另設計「已核准報銷單更正 / 取消核准」流程，否則 `已核准報銷` 與 `應結清` 仍會持續包含該報銷單。
- 明天收尾建議：
  - 確認「不採用此筆結清紀錄」文案是否足夠清楚。
  - 再跑一次 build / smoke test。
  - 更新 handoff 後 commit / push。

## 專案現況

- 已完成功能：
  - 報銷單列表
  - 報銷單列表篩選：關鍵字、狀態、支出類型、付款方式
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
  - 預支款列表篩選：關鍵字、核對狀態
  - 預支款實際結清紀錄：公司補付 / 員工繳回
  - 預支款詳情頁顯示結清紀錄
  - 預支款結清紀錄修改
  - 預支款結清紀錄標記為不採用
  - 預支款有流程中報銷單時顯示暫估核對，且不允許新增最終結清紀錄
  - 報銷單付款方式：員工墊款 / 預支費用
  - 明細單據類型：收據 / 發票，且發票號碼必填
  - 專案建立與專案列表
  - 報銷單支出類型：一般支出 / 專案支出
  - 專案支出報銷單可連到一筆 `Project`
  - 專案結案
  - 專案有未完成報銷單時不可結案
  - 已結案專案不可新增專案支出報銷單，既有草稿 / 退回單也不可送審
  - 專案列表 keyword 查詢
  - 專案詳情頁可查詢該專案全部相關報銷單，結案後仍可作為歷史查詢
- 進行中 / 未完成：
  - 今日「修改 + 不採用」功能已實作並驗證，但尚未 commit。
  - 尚未有使用者 / 角色 / 權限，目前仍手動輸入申請人、審核人、結清處理人、不採用處理人。
  - 尚未有附件或發票照片上傳。
  - 審核流程尚未記錄真正的登入使用者，也尚未限制誰可以核准 / 退回 / 拒絕。
  - 尚未做完整會計總帳、付款憑證或出納日記帳，且目前決定本階段不做。
  - UI 仍是基礎 Bootstrap 版。
- build & DB：
  - `dotnet build` 成功。
  - 桌機本機 DB 已套用最新 migration `20260722145229_AddCashAdvanceSettlementRecordVoiding`。
  - 本機連線字串透過 user secrets 設定，不寫入 repo。
  - dev server 目前已停掉；要跑網站可重新執行 `dotnet run --urls http://localhost:5080`。

## 架構狀態

> 跨 session 防止架構飄移的錨點，務必維護。

- 已落地的 pattern：
  - Controller → Application Service → Domain → Infrastructure
  - Rich Domain Model
  - `ExpenseReport` Aggregate Root
  - `ExpenseDetail` 作為 aggregate 內部 entity
  - `ExpenseReviewRecord` 作為 `ExpenseReport` aggregate 內部 entity
  - `CashAdvance` Aggregate Root
  - `CashAdvanceSettlementRecord` 作為 `CashAdvance` aggregate 內部 entity
  - `Project` Aggregate Root
  - `Money` Value Object
  - `IExpenseReportRepository`
  - `ICashAdvanceRepository`
  - `IProjectRepository`
  - EF Core owned type mapping
  - 列表查詢 DTO / 頁面 DTO 由 Application Service 組裝
- Web 層目錄慣例：
  - `/Web/Controllers`：MVC Controller，只接 HTTP、model binding、呼叫 Application Service、回傳 View。
  - `/Web/ViewModels`：表單與頁面模型，不是 Domain Model。
  - `/Web/Views`：Razor View，只負責畫面呈現與表單送出。
  - 根目錄不再保留 `/Controllers`、`/Models`、`/Views`。
  - `Program.cs` 有客製 Razor view location；之後新增 Razor View 要放在 `/Web/Views`。
- `/docs/architecture/` 已有的篇章：
  - `layered-architecture.md`
  - `expense-report-aggregate.md`
  - `money-value-object.md`
  - `repository-and-ef-core.md`
  - `cash-advance-reconciliation.md`
  - `project-expense-reference.md`
  - `list-filtering-queries.md`
- 有無偏離 CLAUDE.md 規範（技術債）：
  - 無明顯偏離。
  - `ExpenseReport.UpdateBasicInfo(...)` 屬於單一 aggregate 自己的狀態 / invariant，所以放在 domain entity，不是放在 Controller 或 Application Service。
  - `ExpenseReport.UpdateDetail(...)` 屬於 aggregate 內部明細操作，由 root 控制可修改狀態並重新計算總額。
  - `ExpenseReport.Return(...)` / `Approve(...)` / `Reject(...)` 屬於報銷單狀態轉換，並同步建立審核紀錄。
  - `ExpenseReportAppService` 只做 use case 編排與查詢組裝：Project / CashAdvance 是否存在、Project 是否仍可用、列表篩選與 DTO mapping。
  - `ExpenseDetail` 與 `ExpenseReviewRecord` 仍只透過 `ExpenseReport` 操作，沒有獨立 repository。
  - `CashAdvance` 是獨立 aggregate root；報銷單只用 `CashAdvanceId` 參照，符合跨 aggregate 用 ID 的規範。
  - `CashAdvanceSettlementRecord` 只透過 `CashAdvance` 操作，沒有獨立 repository。
  - 結清紀錄修改與標記為不採用屬於 `CashAdvance` aggregate 內部 entity 的操作，所以放在 `CashAdvance` root。
  - 預支款差額 / 尚待結清金額 / 流程中報銷單判斷需要跨 `CashAdvance` 與 `ExpenseReport` 查詢，所以放在 `CashAdvanceAppService` 組 DTO，沒有塞進 Domain entity。
  - 本階段刻意不做沖銷，避免進入會計帳 / 出納日記帳範圍。
  - `Project` 是獨立 aggregate root；報銷單只用 `ProjectId` 參照，沒有把整顆 Project 放進報銷單 aggregate。
  - `Project.Close()` 負責單一 aggregate 的狀態轉換；「專案是否仍有未完成報銷單」與「報銷單送審 / 修改時專案是否已結案」屬跨 aggregate 查詢，放在 Application Service。
  - 專案詳情頁的相關報銷單查詢同樣放在 Application Service，由 `ProjectId` 查詢 `ExpenseReport`；沒有把報銷單放進 `Project` aggregate，也沒有新增封存資料表。
  - 專案列表 keyword 查詢是查詢 / 呈現需求，放在 Application Service 組 DTO，沒有進 Domain entity。
  - 報銷單 / 預支款列表篩選是查詢 / 呈現需求，放在 Application Service 組 DTO，沒有進 Domain entity。
- 後續可能優化：
  - 目前列表篩選先在 Application Service 對既有 `ListAsync()` 結果做 in-memory 篩選。若資料量變大，可新增查詢專用 repository method，把篩選下推到 EF Core / PostgreSQL。
  - 預支款詳情 / 結清同樣目前用 `ListAsync()` 查報銷單後在 Application Service 聚合；資料量變大時可下推到 EF Core group query。

## 開發環境狀態

- 開發 DB（桌機，scoop）：
  - 來源：`scoop install postgresql`。
  - binaries：`~\scoop\apps\postgresql\current\bin`。
  - data directory：`~\scoop\apps\postgresql\current\data`（junction → `~\scoop\persist\postgresql\data`，專案外）。
  - database：`expenselite_dev`；application user：`expenselite_app`。
- repo 內本機資料：
  - `.localdb`、`.devtools.bak`、`.devdata.bak` 已在 `.gitignore` 忽略，不會進 Git。
  - 舊的 `.devtools.bak`、`.devdata.bak` 若確認 scoop 穩定，之後可由 Amber 決定是否刪除。
- 跨機器注意：
  - 其他機器 pull 後，若 DB 尚未套最新 migration，需執行 `dotnet ef database update`。
  - 本次新增 migration `20260722145229_AddCashAdvanceSettlementRecordVoiding`，其他機器需要更新 DB。
  - 筆電 DB 原則同樣是不放在專案資料夾內。
- 工具注意：
  - Codex sandbox 執行 git 時可能看到 `C:\Users\Pinecone/.config/git/ignore` permission warning。
  - Amber 本機 PowerShell 執行 `git status --short --branch` 沒有 warning。
  - 目前判斷 warning 是 Codex sandbox 讀不到 repo 外 global ignore，不影響本 repo 已追蹤檔案與 commit / push 狀態判讀。
  - `git diff --check` 可能出現 LF/CRLF 提醒，目前是 Windows 換行格式提示，不是程式錯誤。
  - `dotnet run` 若由 Codex sandbox 啟動，可能因 repo 外 `NuGet.Config` 權限被擋；可改用 Amber 本機 PowerShell 或允許 Codex 在 sandbox 外啟動。
  - 若 dev server 還在跑，Windows 會鎖住 `bin\Debug\net10.0\ExpenseLite.exe`；重新 `dotnet build` 前需先停掉該 process。

## 待 Amber 決定 / 待辦

- 明天優先：
  - 確認「不採用此筆結清紀錄」文案是否足夠清楚。
  - 再跑 build、smoke test。
  - 若確認可收尾，更新 handoff 後 commit / push。
- 應用面下一步建議：
  - 可補「附件或發票照片上傳」，但會牽涉檔案儲存、安全性與大小限制。
  - 可另開「已核准報銷單更正 / 取消核准」流程，用來處理已核准後才發現選錯預支款、報銷單不成立、或需要從預支款核對中排除的情境。
  - 之後若 Amber 想把系統推進到多使用者，可另開「使用者 / 角色 / 權限」功能，再把申請人 / 審核人 / 結清處理人從手動輸入升級成登入者。
  - 若未來真的要做沖銷、付款憑證或出納日記帳，需另開會計帳範圍設計，不建議混進目前第一階段。
- 開發環境：
  - 兩台都穩定後，再考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`。刪遠端分支屬不可逆操作，先確認再動。
  - 桌機舊 portable 備份 `.devtools.bak`、`.devdata.bak` 仍保留；刪除前需 Amber 明確確認。

## git 狀態

- 已存在的最新 commit：
  - `4765278 docs: 更新預支款結清 handoff`
- push 狀態：
  - `4765278` 已在 `origin/main`。
  - `git status --short --branch` 顯示 `## main...origin/main`，目前沒有 ahead / behind。
- 目前有未 commit 變更：
  - `Application/CashAdvances/CashAdvanceAppService.cs`
  - `Application/CashAdvances/CashAdvanceCommands.cs`
  - `Application/CashAdvances/CashAdvanceDtos.cs`
  - `Domain/CashAdvances/CashAdvance.cs`
  - `Domain/CashAdvances/CashAdvanceSettlementRecord.cs`
  - `Infrastructure/Persistence/CashAdvanceConfiguration.cs`
  - `Infrastructure/Persistence/Migrations/ExpenseLiteDbContextModelSnapshot.cs`
  - `Infrastructure/Persistence/Migrations/20260722145229_AddCashAdvanceSettlementRecordVoiding.cs`
  - `Infrastructure/Persistence/Migrations/20260722145229_AddCashAdvanceSettlementRecordVoiding.Designer.cs`
  - `Web/Controllers/CashAdvancesController.cs`
  - `Web/ViewModels/CashAdvances/CashAdvanceSettlementRecordActionPages.cs`
  - `Web/ViewModels/CashAdvances/EditCashAdvanceSettlementForm.cs`
  - `Web/ViewModels/CashAdvances/VoidCashAdvanceSettlementForm.cs`
  - `Web/Views/CashAdvances/Details.cshtml`
  - `Web/Views/CashAdvances/EditSettlement.cshtml`
  - `Web/Views/CashAdvances/Index.cshtml`
  - `Web/Views/CashAdvances/VoidSettlement.cshtml`
  - `docs/architecture/cash-advance-reconciliation.md`
  - `.claude/current-handoff.md`
  - `.claude/handoff-archive/handoff-20260722-2343.md`
- 本次 handoff：
  - 舊 handoff 已歸檔到 `.claude/handoff-archive/handoff-20260722-2343.md`。
  - 本檔已更新為明天可接續的未 commit 狀態。
- 建議 commit message：
  - `feat: 新增預支款結清紀錄修改與不採用`
- 後續若要 git add / commit / push，仍需依 CLAUDE.md 規定先列出指令、範圍與 commit message，取得 Amber 明確確認後才能執行。
