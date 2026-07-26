# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔，結束時更新此檔（舊內容歸檔到 `.claude/handoff-archive/`）。
> 內容聚焦「專案現況 + 架構狀態」，不是學習進度。

> 最後更新：2026-07-25 20:14 — 開發環境：PostgreSQL 註冊為 Windows 服務；本次無程式碼變更

---

## 專案現況

### 已完成功能

**報銷單**
- 報銷單列表；列表篩選：關鍵字、狀態、支出類型、付款方式
- 新增報銷單、報銷單詳細頁
- 修改草稿 / 退回報銷單主檔欄位
- 新增 / 修改 / 移除草稿或退回報銷單的明細，明細金額加總到報銷單總額
- 草稿 / 退回報銷單送審
- 核准 / 退回 / 拒絕流程 UI；記錄審核人、審核時間、審核動作；退回 / 拒絕記錄原因
- 報銷單詳細頁顯示審核紀錄
- 付款方式：員工墊款 / 預支費用
- 明細單據類型：收據 / 發票，發票號碼必填

**預支款**
- 預支款建立與核對列表；列表篩選：關鍵字、核對狀態
- 預支款用途與金額修改（金額限「無報銷單引用」且「無已計入核對的結清紀錄」時才可改）
- 實際結清紀錄：公司補付 / 員工繳回；詳情頁顯示結清紀錄
- 結清紀錄修改
- 結清紀錄標記為不採用（保留處理人、原因、時間，不是刪除資料）
- 有流程中報銷單時顯示暫估核對，且不允許新增最終結清紀錄

**專案**
- 專案建立與專案列表；列表 keyword 查詢
- 報銷單支出類型：一般支出 / 專案支出；專案支出報銷單可連到一筆 `Project`
- 專案結案；有未完成報銷單時不可結案
- 已結案專案不可新增專案支出報銷單，既有草稿 / 退回單也不可送審
- 專案詳情頁可查詢該專案全部相關報銷單，結案後仍可作為歷史查詢

### 尚未做（已知缺口）

- 沒有使用者 / 角色 / 權限。申請人、審核人、結清處理人、不採用處理人目前仍是**手動輸入**。
- 審核流程沒有記錄真正的登入使用者，也沒有限制誰可以核准 / 退回 / 拒絕。
- 沒有附件或發票照片上傳。
- 沒有完整會計總帳、付款憑證、出納日記帳，**且本階段刻意決定不做**。
- 沒有「已核准報銷單更正 / 取消核准」流程。
- UI 仍是基礎 Bootstrap 版。

### build & DB

- `dotnet build` 成功，0 warning / 0 error。
- 桌機本機 DB 已套用最新 migration `20260722145229_AddCashAdvanceSettlementRecordVoiding`。
- 本機連線字串透過 user secrets 設定，**不寫入 repo**。

---

## 架構狀態

> 跨 session 防止架構飄移的錨點，務必維護。

### 已落地的 pattern

- Controller → Application Service → Domain → Infrastructure
- Rich Domain Model
- `ExpenseReport` Aggregate Root
  - `ExpenseDetail` 作為 aggregate 內部 entity
  - `ExpenseReviewRecord` 作為 aggregate 內部 entity
- `CashAdvance` Aggregate Root
  - `CashAdvanceSettlementRecord` 作為 aggregate 內部 entity
- `Project` Aggregate Root
- `Money` Value Object
- `IExpenseReportRepository` / `ICashAdvanceRepository` / `IProjectRepository`（一個 aggregate root 一個 repository）
- EF Core owned type mapping
- 列表查詢 DTO / 頁面 DTO 由 Application Service 組裝

### Web 層目錄慣例

- `/Web/Controllers`：MVC Controller，只接 HTTP、model binding、呼叫 Application Service、回傳 View。
- `/Web/ViewModels`：表單與頁面模型，不是 Domain Model。
- `/Web/Views`：Razor View，只負責畫面呈現與表單送出。
- 根目錄不再保留 `/Controllers`、`/Models`、`/Views`。
- `Program.cs` 有客製 Razor view location；之後新增 Razor View 要放在 `/Web/Views`。

### `/docs/architecture/` 已有的篇章

- `layered-architecture.md`
- `expense-report-aggregate.md`
- `money-value-object.md`
- `repository-and-ef-core.md`
- `cash-advance-reconciliation.md`
- `project-expense-reference.md`
- `list-filtering-queries.md`

### 有無偏離 CLAUDE.md 規範（技術債）

**無明顯偏離。** 以下是各項邏輯落點的判斷理由，續作時照這個標準延續：

放在 Domain entity（屬於單一 aggregate 自己的規則 / 狀態轉換）：
- `ExpenseReport.UpdateBasicInfo(...)`
- `ExpenseReport.UpdateDetail(...)`——由 root 控制可修改狀態並重新計算總額
- `ExpenseReport.Return(...)` / `Approve(...)` / `Reject(...)`——狀態轉換並同步建立審核紀錄
- `CashAdvance.UpdateBasicInfo(...)`——已計入核對的結清紀錄存在時不可改預支金額
- `CashAdvance.UpdateSettlementRecord(...)` / `VoidSettlementRecord(...)`——aggregate 內部 entity 操作，一律走 root
- `Project.Close()`

放在 Application Service（跨 aggregate 查詢、use case 編排、DTO 組裝）：
- `ExpenseReportAppService`：Project / CashAdvance 是否存在、Project 是否仍可用、列表篩選與 DTO mapping
- `CashAdvanceAppService`：預支款是否已有報銷單引用、是否仍有流程中報銷單、差額 / 尚待結清金額計算、核對 DTO 組裝
- 專案是否仍有未完成報銷單、報銷單送審 / 修改時專案是否已結案
- 專案詳情頁的相關報銷單查詢（由 `ProjectId` 查 `ExpenseReport`）
- 報銷單 / 預支款 / 專案的列表篩選與 keyword 查詢（查詢 / 呈現需求，不進 Domain）

aggregate 邊界紀律：
- `ExpenseDetail`、`ExpenseReviewRecord` 只透過 `ExpenseReport` 操作，沒有獨立 repository。
- `CashAdvanceSettlementRecord` 只透過 `CashAdvance` 操作，沒有獨立 repository。
- 報銷單只用 `CashAdvanceId`、`ProjectId` 參照，沒有把整顆 CashAdvance / Project 抓進報銷單 aggregate。

### 後續可能優化

- 列表篩選目前先在 Application Service 對 `ListAsync()` 結果做 in-memory 篩選。資料量變大時，可新增查詢專用 repository method，把篩選下推到 EF Core / PostgreSQL。
- 預支款詳情 / 結清同樣用 `ListAsync()` 查報銷單後在 Application Service 聚合；資料量變大時可下推到 EF Core group query。

---

## 已定案的設計決策

> 這些是 Amber 已經拍板的，續作時不要再推翻或重新設計。

- **不做沖銷 / 會計帳**：不保留 `Reversal` 型態，不做付款憑證、出納日記帳、會計帳務沖銷。曾短暫產生的 `AddCashAdvanceSettlementRecordActions` migration 已回退並 `dotnet ef migrations remove`。
- **「不採用」不是「作廢」**：UI 文案用「不採用此筆結清紀錄」。
  - 只影響這筆結清紀錄，不影響預支款、不影響報銷單。
  - 不會改變 `已核准報銷`、`差額`、`應結清`。
  - 會改變 `已結清`、`尚待結清`、核對狀態。
  - 不採用不是刪除；紀錄仍顯示在詳情頁歷史中。詳情頁狀態欄顯示「已計入核對 / 不採用」。
- **結清金額規則**：`尚待結清 = 應結清 - 已計入核對的有效已結清`；已結清金額只加總仍被採用的結清紀錄。可分次結清，系統只擋超過尚待結清的金額。
- **暫估規則**：同一筆預支款若仍有 Draft / Submitted / Returned 的關聯報銷單，只顯示暫估核對，不允許新增最終結清紀錄。
- **2026-07-23 釐清的邊界**：「已核准報銷單選錯預支款」或「已核准後要取消該報銷單」**不屬於**結清紀錄不採用的範圍，需要另設計「已核准報銷單更正 / 取消核准」流程，否則 `已核准報銷` 與 `應結清` 會持續包含該報銷單。

---

## 開發環境狀態

### 開發 DB（桌機，scoop）

- 來源：`scoop install postgresql`，版本 PostgreSQL 18.4。
- binaries：`~\scoop\apps\postgresql\current\bin`（`current` 是 junction → `~\scoop\apps\postgresql\18.4-2`）。
- data directory：`~\scoop\persist\postgresql\data`（專案外；`~\scoop\apps\postgresql\current\data` 是指向它的 junction）。
- database：`expenselite_dev`；application user：`expenselite_app`。

**已註冊為 Windows 服務 `postgresql-18`（2026-07-25）**
- 啟動類型 `Automatic`：開機自動啟動、關機自動正常停止，不用再手動 `pg_ctl start`。
- 執行帳號 `LocalSystem`（已確認 data 目錄 ACL 中 `NT AUTHORITY\SYSTEM` 有 FullControl）。
- 服務執行檔：`...\current\bin\pg_ctl.exe runservice -N "postgresql-18" -D "C:\Users\Pinecone\scoop\persist\postgresql\data" -w`
- 查詢：`Get-Service postgresql-18`（不需 admin）
- 手動啟停：`Start-Service postgresql-18` / `Stop-Service postgresql-18`（需系統管理員權限的 PowerShell）
- 還原註冊：`pg_ctl unregister -N postgresql-18`（需 admin）
- 註冊時用的指令：`pg_ctl register -N postgresql-18 -D "C:\Users\Pinecone\scoop\persist\postgresql\data" -S auto`
  - data path 刻意用 `scoop\persist` 真實路徑而非 `current` junction，避免 scoop 更新 app 版本時資料路徑跟著飄。

**DB log**
- `postgresql.conf` 的 `logging_collector` 已由 `off` 改為 `on`。原因：`pg_ctl register` 沒有 `-l` 參數，服務模式下不設定的話 log 只會進 Windows 事件檢視器。
- log 位置：`~\scoop\persist\postgresql\data\log\postgresql-YYYY-MM-DD_HHmmss.log`，自動輪替。
- **注意**：`postgresql.conf` 在 repo 之外，這個設定**不會進 Git**，換機器要自己設定一次。

**已知風險**
- 服務的執行檔走 `current` junction。若日後 `scoop update postgresql` 升上 PG 19 這類大版本，新 binary 會對到舊的 18 資料目錄，服務會啟動失敗（log 會明講版本不符）。屆時需要做資料的 major upgrade，**不是資料損壞**。
- 筆電尚未做同樣的服務註冊，需要時要在筆電另外執行一次。

### repo 內本機資料

- `.localdb`、`.devtools.bak`、`.devdata.bak` 已在 `.gitignore` 忽略，不會進 Git。
- 舊的 `.devtools.bak`、`.devdata.bak` 若確認 scoop 穩定，之後可由 Amber 決定是否刪除。

### 跨機器注意

- 其他機器 pull 後，若 DB 尚未套最新 migration（版本見〈專案現況 / build & DB〉），需執行 `dotnet ef database update`。
- 筆電 DB 原則同樣是不放在專案資料夾內。

### 疑難排解

- 頁面出現 `Failed to connect to 127.0.0.1:5432` → 先查 `Get-Service postgresql-18` 是否 Running，再看 `data\log\` 最新 log。這是 DB 沒起來，**不是程式錯誤**。
- 若 dev server 還在跑，Windows 會鎖住 `bin\Debug\net10.0\ExpenseLite.exe`；重新 `dotnet build` 前需先停掉該 process。
- 註冊 / 啟停服務需要系統管理員權限；一般 session 權限不足時會需要 UAC 確認。
- `git diff --check` 可能出現 LF/CRLF 提醒，是 Windows 換行格式提示，不是程式錯誤。
- Codex sandbox 執行 git 時可能看到 `C:\Users\Pinecone/.config/git/ignore` permission warning；是 sandbox 讀不到 repo 外 global ignore，不影響本 repo 狀態判讀。Amber 本機 PowerShell 沒有這個 warning。
- `dotnet run` 若由 Codex sandbox 啟動，可能因 repo 外 `NuGet.Config` 權限被擋；可改用本機 PowerShell 或允許 Codex 在 sandbox 外啟動。

---

## 待 Amber 決定 / 下一步

### 優先討論（下個 session 開場）

**「預支款已結清」後，是否仍要保留修改功能？**
- 若保留，需要一起定義已結清後哪些欄位不可修改。
- 初步待討論欄位：預支款本身的用途 / 預支金額；結清紀錄的結清日期 / 結清金額 / 處理人 / 備註。

### 應用面候補（尚未排序）

- **已核准報銷單更正 / 取消核准**流程：處理已核准後才發現選錯預支款、報銷單不成立、或需要從預支款核對中排除的情境。
- **附件 / 發票照片上傳**：會牽涉檔案儲存、安全性與大小限制。
- **使用者 / 角色 / 權限**：把申請人 / 審核人 / 結清處理人從手動輸入升級成登入者。
- 若未來真的要做沖銷、付款憑證或出納日記帳，需另開會計帳範圍設計，**不建議混進第一階段**。

### 開發環境待辦

- 筆電也做一次 PostgreSQL 服務註冊與 `logging_collector` 設定。
- 兩台都穩定後，再考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`。**刪遠端分支屬不可逆操作，先確認再動。**
- 桌機舊 portable 備份 `.devtools.bak`、`.devdata.bak` 仍保留；**刪除前需 Amber 明確確認**。

---

## 本檔維護紀律

- **每項資訊只有一個出處。** 不在多處重述同一件事——重複的地方遲早只更新其中一處，變成互相矛盾。
- 需要交叉引用時，用「見〈某章節〉」指過去，不要複製內容。
- 更新時先刪過期內容，再寫新內容；不要讓做完的待辦跟有效待辦並存。
- **不放「當下瞬間狀態」**：dev server 開著沒、port 有沒有在聽、最新 commit hash、有沒有未 commit 變更——這些下次開工就過期了，該現查（`git status` / `git log` / `Get-Service`）。本檔尤其不記錄自己的 commit hash，因為 commit 發生在寫檔之後，寫下去當下就是錯的。
