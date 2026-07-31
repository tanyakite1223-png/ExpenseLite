# ExpenseLite — Current Handoff

> 跨 session 接力用。每個 Claude Code session 開始時先讀此檔，結束時更新此檔（舊內容歸檔到 `.claude/handoff-archive/`）。
> 內容聚焦「專案現況 + 架構狀態」，不是學習進度。

> 最後更新：2026-07-31 21:50 — 階段 4a（授權規則）完成，4b（預支款領款人模型）待開始

---

## 專案現況

### 已完成功能

**使用者與登入**
- ASP.NET Core Identity 登入 / 登出；全站預設需要登入，例外要標 `[AllowAnonymous]`
- 兩種角色：員工 / 主管
- 初始帳號種子 `manager`、`employee`（登入帳號用 email 前綴，不是完整 email；顯示名稱為王主管、陳員工）
- Layout 右上角顯示登入者姓名與角色
- 報銷單申請人、審核人、結清處理人、不採用處理人**自動帶入登入者**，畫面上是唯讀；各自同時存 `Guid` 形式的 UserId 與「當時的姓名快照」

**權限（階段 4a）**
- **只有主管可以**：核准 / 退回 / 拒絕報銷單；預支款全部功能（含列表與詳情）；建立與結案專案
- **只有申請人本人可以**修改 / 送審自己的報銷單與明細，**主管沒有例外**
- 員工的 nav 不顯示「預支款」；報銷單列表與詳情的修改 / 送審 / 審核按鈕依登入者隱藏
- 權限不足回 HTTP 403，導到 `/Account/AccessDenied`
- 已實測：繞過畫面直接 POST 送審別人的草稿仍被擋下、資料未被寫入

**報銷單**
- 報銷單列表；列表篩選：關鍵字、狀態、支出類型、付款方式
- 新增報銷單、報銷單詳細頁
- 修改草稿 / 退回報銷單主檔欄位（申請人固定為建立者，不在修改範圍）
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
- 結清紀錄修改（處理人維持原登記者，不會換成修改者）
- 結清紀錄標記為不採用（保留處理人、原因、時間，不是刪除資料）
- 有流程中報銷單時顯示暫估核對，且不允許新增最終結清紀錄

**專案**
- 專案建立與專案列表；列表 keyword 查詢
- 報銷單支出類型：一般支出 / 專案支出；專案支出報銷單可連到一筆 `Project`
- 專案結案；有未完成報銷單時不可結案
- 已結案專案不可新增專案支出報銷單，既有草稿 / 退回單也不可送審
- 專案詳情頁可查詢該專案全部相關報銷單，結案後仍可作為歷史查詢

### 尚未做（已知缺口）

- **員工還看得到所有人的報銷單**，列表與詳情尚未依 `ApplicantUserId` 過濾（階段 5）。權限只做到「動不了別人的單」，還沒做到「看不到別人的單」。
- **報銷單的「對應預支款」下拉選單仍列出全部未結清預支款**，員工在那裡看得到其他人的領款人、用途與金額。這是目前最明顯的資訊外漏點，要等領款人存了 UserId 才能過濾（階段 4b）。
- **預支款可見度目前過度保守**：整塊只給主管，連保管零用金的人都看不到自己那筆的使用狀態。原因同上——領款人無 UserId，無法過濾成「我的預支款」（階段 4b）。
- **報銷單詳情頁不顯示「對應哪一筆預支款」**，連自己選的那筆都看不到。選得到卻看不到，是 UI 缺口。
- **帳號只能靠種子資料產生**：沒有註冊、沒有使用者管理 UI、沒有修改密碼頁。種子目前還會建 demo 的 `employee`（陳員工），正式區不該有這個共用帳號。整塊已規劃為階段 6。
- 沒有附件或發票照片上傳。
- 沒有完整會計總帳、付款憑證、出納日記帳，**且本階段刻意決定不做**。
- 沒有「已核准報銷單更正 / 取消核准」流程。
- UI 仍是基礎 Bootstrap 版。

### build & DB

- `dotnet build` 成功，0 warning / 0 error。
- 桌機本機 DB 已套用最新 migration `20260727091350_AddActorUserIds`。階段 4a **沒有** 新增 migration（純授權邏輯，沒動 model）。
- 本機連線字串與 Identity 初始密碼都透過 user secrets 設定，**不寫入 repo**（見〈開發環境狀態〉）。

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
- ASP.NET Core Identity：cookie 登入、角色、custom claims principal factory
- 登入者資訊經 `Application/Identity/CurrentUser` 這個 record 傳進 Application 與 Domain
- **授權分兩種落點**：角色授權用 Controller 的 `[Authorize(Roles = ...)]`；資源授權（「這筆資料是不是你的」）在 Application Service
- **`Application/Shared/ForbiddenOperationException`** 與 `DomainRuleViolationException` 分開，由 `Web/Filters/ForbiddenOperationExceptionFilter` 轉成 HTTP 403

### Web 層目錄慣例

- `/Web/Controllers`：MVC Controller，只接 HTTP、model binding、呼叫 Application Service、回傳 View。
- `/Web/ViewModels`：表單與頁面模型，不是 Domain Model。
- `/Web/Views`：Razor View，只負責畫面呈現與表單送出。
- `/Web/Filters`：MVC filter，目前只有把 Application 例外轉成 HTTP 狀態碼這一支。
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
- `identity-and-authentication.md`（認證：「你是誰」怎麼流進 Domain）
- `authorization.md`（授權：「你能不能做」的兩種落點與判斷標準）

### 有無偏離 CLAUDE.md 規範（技術債）

**無明顯偏離。** 以下是各項邏輯落點的判斷理由，續作時照這個標準延續：

放在 Domain entity（屬於單一 aggregate 自己的規則 / 狀態轉換）：
- `ExpenseReport.Create(...)`——建立時就固定申請人的 UserId 與姓名快照
- `ExpenseReport.UpdateBasicInfo(...)`——**不接**申請人參數，申請人建立後不可變
- `ExpenseReport.UpdateDetail(...)`——由 root 控制可修改狀態並重新計算總額
- `ExpenseReport.Return(...)` / `Approve(...)` / `Reject(...)`——狀態轉換並同步建立審核紀錄，帶入審核人 UserId 與姓名快照
- `CashAdvance.UpdateBasicInfo(...)`——已計入核對的結清紀錄存在時不可改預支金額
- `CashAdvance.UpdateSettlementRecord(...)` / `VoidSettlementRecord(...)`——aggregate 內部 entity 操作，一律走 root
- `CashAdvanceSettlementRecord.Update(...)`——**不接**處理人參數，避免更正內容時改寫歷史
- `Project.Close()`

放在 Application Service（跨 aggregate 查詢、use case 編排、DTO 組裝、資源授權）：
- `ExpenseReportAppService`：Project / CashAdvance 是否存在、Project 是否仍可用、列表篩選與 DTO mapping
- `ExpenseReportAppService.EnsureCanBeEditedBy(...)`——「只有申請人能改」需要先載入報銷單才知道申請人是誰，所以不可能在 Controller 判斷
- `CashAdvanceAppService`：預支款是否已有報銷單引用、是否仍有流程中報銷單、差額 / 尚待結清金額計算、核對 DTO 組裝
- 專案是否仍有未完成報銷單、報銷單送審 / 修改時專案是否已結案
- 專案詳情頁的相關報銷單查詢（由 `ProjectId` 查 `ExpenseReport`）
- 報銷單 / 預支款 / 專案的列表篩選與 keyword 查詢（查詢 / 呈現需求，不進 Domain）

授權的分層落點（理由詳見 `docs/architecture/authorization.md`）：
- **不用讀資料庫就能判斷的擋在 Controller**（角色），**需要讀資料才知道的擋在 Application Service**（擁有者）。
- 「只有申請人能改」**刻意不放進 entity**：報銷單自己的 invariant 是「草稿或退回才能改」，不管誰來改都成立；「必須是本人」是存取控制。放進 entity 會讓每個 domain method 都要多接 `currentUser` 參數，污染 Domain API。
- **Tradeoff 老實記著**：因此新增 Application Service 方法時若忘了呼叫 `EnsureCanBeEditedBy`，Domain 不會幫你擋。這跟 aggregate 邊界一樣是靠約定維持的紀律。
- **View 隱藏按鈕不是權限**，只是 UX；真正的把關永遠在 `[Authorize]` 或 Application Service。兩層都要有。

Identity 的分層落點（理由詳見 `docs/architecture/identity-and-authentication.md`）：
- `ApplicationUser` 放 `Infrastructure/Identity`——它繼承 `IdentityUser<Guid>`，直接依賴框架，不能進 Domain。
- 角色常數 `ExpenseLiteRoles` 放 `Application/Identity`——「員工 / 主管」是業務概念，Web 與 Infrastructure 都要用。
- `CurrentUser` 放 `Application/Identity`——Application 只認識它，不認識 `ClaimsPrincipal`；由 Controller 呼叫 `User.ToCurrentUser()` 轉換，Application Service 不碰 `HttpContext`。
- **Domain 不認識 `ApplicationUser`**；記「是誰做的」時只存 `Guid` 形式的 UserId，比照跨 aggregate 用 ID 參照的規則。

aggregate 邊界紀律：
- `ExpenseDetail`、`ExpenseReviewRecord` 只透過 `ExpenseReport` 操作，沒有獨立 repository。
- `CashAdvanceSettlementRecord` 只透過 `CashAdvance` 操作，沒有獨立 repository。
- 報銷單只用 `CashAdvanceId`、`ProjectId` 參照，沒有把整顆 CashAdvance / Project 抓進報銷單 aggregate。

### 後續可能優化

- 列表篩選目前先在 Application Service 對 `ListAsync()` 結果做 in-memory 篩選。資料量變大時，可新增查詢專用 repository method，把篩選下推到 EF Core / PostgreSQL。
- 預支款詳情 / 結清同樣用 `ListAsync()` 查報銷單後在 Application Service 聚合；資料量變大時可下推到 EF Core group query。
- 結清紀錄目前只有 `UpdatedAt`、沒有「這次是誰改的」。真的需要時再加 `UpdatedByUserId`。

---

## 已定案的設計決策

> 這些是 Amber 已經拍板的，續作時不要再推翻或重新設計。

- **角色只有兩種：員工 / 主管**。審核與預支款 / 結清作業都歸主管，不另設會計或系統管理員角色。
- **登入帳號用 email 前綴**（`manager`、`employee`），不是完整 email。email 欄位保留，供之後密碼重設等用途。
- **「是誰做的」一律存兩個欄位**：`Guid?` 形式的 UserId（給程式比對用）+ 姓名字串（給人看的歷史紀錄）。姓名是**寫入當下的快照**，使用者日後改名不會改寫歷史；UserId 可為 null，代表這筆資料早於登入功能。
- **操作者才自動帶入登入者，當事人不會**。預支款的**領款人是「當事人」**，所以不自動帶登入者——主管很可能替員工建立預支款。但「不自動帶登入者」不等於「只能存字串」，見下方領款人的決策。
- **申請人 = 建立報銷單的人**，建立後不可修改。
- **修改結清紀錄不會換掉處理人**：處理人是當初登記那筆結清的人，別人來更正金額 / 備註都不會蓋掉。
- **主管沒有「代改草稿」的代理權**（2026-07-31 定案）：只有申請人本人能修改 / 送審自己的草稿，主管的角色是審核，不是代改。理由：目前沒有記錄「誰送審」，主管代送會讓責任歸屬變模糊——單子看起來像員工自己送的。
- **專案的建立與結案限主管**（2026-07-31 定案）：結案會擋掉其他人的報銷單送審，不該讓員工做。專案列表與詳情維持所有人可看，因為員工建報銷單要選專案。
- **領款人改成從使用者清單選、存 `PayeeUserId` + 姓名快照**（2026-07-31 定案，**推翻 7/27「維持手動輸入」的決策**）。當初手動輸入是因為權限還沒做；現在有登入了，沒有 UserId 就無法判斷「這筆預支款是不是我的」，連「員工只看自己的預支款」與下拉過濾都做不到。仍然不是自動帶入登入者，是主管挑人。
- **預支款要區分兩種用途**（2026-07-31 定案）：
  - **零用金（共用）**：主管撥一筆錢給固定的設計師保管，其他設計師要用錢就跟他領，各自開自己的報銷單引用**同一筆**預支款。領款人 = 保管人。
  - **個人預支**：主管直接把錢給某位設計師，那人自己用自己報。領款人 = 那個人，**只有他能引用這筆**。
  - 為什麼非得區分：報銷單的預支款下拉如果一律「大家都能選」，個人預支的錢會被別人報掉；一律「只有領款人能選」，零用金就沒人用得了。這是模型缺一個欄位，不是權限規則能解決的。
  - **領款人要能看到自己那筆預支款的使用狀態**（保管零用金的人尤其需要）。
  - **非保管人看不到零用金的詳情頁**：詳情頁是財務核對視角（差額、應結清、結清紀錄、新增結清表單），使用者只需要開自己的報銷單。想知道還剩多少就直接問保管人——跟現實中零用金放抽屜一樣，這件事不用搬進系統。
  - **但零用金一定會出現在所有人的報銷單下拉**，含保管人與預支金額。選不到看不見的東西，而且使用者本來就要知道去找誰領。個人預支則只出現在領款人本人的下拉。所以「非保管人看不到詳情」不等於「看不到這筆零用金存在」。
  - **新建預支款預設「個人預支」**：預設選限制緊的一方，忘了改的後果比較小。反過來預設零用金，忘了改就是「本該專屬某人的錢，全公司都能報」。
  - **選了零用金時，建立完成後要顯示明確提示**（「這筆是零用金（共用），所有人都能引用」），讓選錯當下就看得出來。跟上一條是兩層保險：預設擋住無意識的錯誤，提示擋住有意識但選錯的。
- **不做沖銷 / 會計帳**：不保留 `Reversal` 型態，不做付款憑證、出納日記帳、會計帳務沖銷。
- **「不採用」不是「作廢」**：UI 文案用「不採用此筆結清紀錄」。
  - 只影響這筆結清紀錄，不影響預支款、不影響報銷單。
  - 不會改變 `已核准報銷`、`差額`、`應結清`。
  - 會改變 `已結清`、`尚待結清`、核對狀態。
  - 不採用不是刪除；紀錄仍顯示在詳情頁歷史中。詳情頁狀態欄顯示「已計入核對 / 不採用」。
- **結清金額規則**：`尚待結清 = 應結清 - 已計入核對的有效已結清`；已結清金額只加總仍被採用的結清紀錄。可分次結清，系統只擋超過尚待結清的金額。
- **暫估規則**：同一筆預支款若仍有 Draft / Submitted / Returned 的關聯報銷單，只顯示暫估核對，不允許新增最終結清紀錄。
- **帳號生命週期：註冊（自助）→ 啟用（主管）→ 用 → 停用（主管）**（2026-07-27 定案，實作見階段 6）
  - **員工自行註冊**，登入頁上有連結，`[AllowAnonymous]`。註冊只能產生員工，不能自封主管。
  - **註冊後預設 `Pending`，主管啟用才能登入**。避免任何連得到主機的人直接就能用。
  - **帳號狀態改成三態 `UserAccountStatus { Pending, Active, Disabled }`**，取代目前的 `IsActive` bool——bool 分不出「還沒啟用過」和「離職被停用」，登入訊息會講錯。
  - **登入先驗密碼、再判帳號狀態**，狀態訊息才講明白（「尚待主管啟用」/「已停用」）。順序相反的話，不用知道密碼就能探測帳號是否存在。
  - **系統至少要保留一個啟用中的主管**：最後一個主管不能停用自己或把自己降成員工，否則全公司進不去且無後門。
  - **正式區 bootstrap**：users 表為空時自動建**一個**帳號 `Admin`，角色是**主管**（不是第三種角色），密碼取自 `Identity:SeedPassword`。上線後第一件事是用「修改密碼」頁換掉它，換完可把主機上的該設定值移除。之後由 `Admin` 指派其他主管。
  - 選 seed 而不是「第一個註冊的人自動變主管」的理由：seed 在第一次啟動時就完成，那時還沒人能連進來註冊，**不存在搶註冊的空窗期**；行為也可預測。
- **2026-07-23 釐清的邊界**：「已核准報銷單選錯預支款」或「已核准後要取消該報銷單」**不屬於**結清紀錄不採用的範圍，需要另設計「已核准報銷單更正 / 取消核准」流程，否則 `已核准報銷` 與 `應結清` 會持續包含該報銷單。

---

## 開發環境狀態

### user secrets

`UserSecretsId` 是 `expenselite-local-dev`。目前需要兩個 key，**都不進 repo**：

- `ConnectionStrings:ExpenseLite`——PostgreSQL 連線字串。
- `Identity:SeedPassword`——初始帳號的密碼。沒設的話 app 仍可啟動，只是跳過建立初始帳號並留下 warning log。

新機器要自己設一次：`dotnet user-secrets set "Identity:SeedPassword" "<密碼>"`。

### 開發 DB（桌機，scoop）

- 來源：`scoop install postgresql`，版本 PostgreSQL 18.4。
- binaries：`~\scoop\apps\postgresql\current\bin`（`current` 是 junction → `~\scoop\apps\postgresql\18.4-2`）。
- data directory：`~\scoop\persist\postgresql\data`（專案外；`~\scoop\apps\postgresql\current\data` 是指向它的 junction）。
- database：`expenselite_dev`；application user：`expenselite_app`。
- dev 資料中有測試用報銷單，標題以「（測試）」開頭：一張是驗證階段 3 用的，一張是驗證階段 4a 授權用的草稿（申請人陳員工，刻意留著給手動點測用）。系統沒有刪除報銷單的功能，所以它們會一直留著，**不是資料異常**。
- dev 資料的 21 張舊報銷單 `applicant_user_id` 為 null（早於登入功能），所以任何人都不是申請人，一律不可修改 / 送審。這是預期行為。
- dev 資料有 **7 筆預支款**，領款人都是手打字串（Candy、Belle、Amber、Amy、Tone），**沒有一個對應到真實帳號**（users 表目前只有 `manager`、`employee`）。用途欄位字面上全是「零用金」，其中一筆是錯字「零土金」。每筆都有 1–3 張報銷單引用、0–4 筆結清紀錄，**所以不適合刪掉重建**——會牽連那些報銷單與結清紀錄。階段 4b 的處理方式見〈下一步：階段 4b〉。

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
- `dotnet ef` 加 `--no-build` 時會拿到還沒重新編譯的 model snapshot，可能誤報 `PendingModelChangesWarning`。改動 model 後先 `dotnet build` 再跑 `dotnet ef`。
- 註冊 / 啟停服務需要系統管理員權限；一般 session 權限不足時會需要 UAC 確認。
- `git diff --check` 或 `git add` 可能出現 LF/CRLF 提醒，是 Windows 換行格式提示，不是程式錯誤。
- 用 PowerShell 對 HTML 內容做中文 `-match` 比對可能因編碼而誤判；驗證資料狀態要直接查 DB，不要信 HTML 比對結果。
- Codex sandbox 執行 git 時可能看到 `C:\Users\Pinecone/.config/git/ignore` permission warning；是 sandbox 讀不到 repo 外 global ignore，不影響本 repo 狀態判讀。Amber 本機 PowerShell 沒有這個 warning。
- `dotnet run` 若由 Codex sandbox 啟動，可能因 repo 外 `NuGet.Config` 權限被擋；可改用本機 PowerShell 或允許 Codex 在 sandbox 外啟動。

---

## 待 Amber 決定 / 下一步

### 進行中：權限與帳號工作（分 6 階段，已完成 1–3 與 4a）

1. ✅ Identity 基礎建設
2. ✅ 登入 / 登出
3. ✅ 把手動輸入的人名改成登入者——申請人、審核人、結清處理人、不採用處理人
4. **授權規則**，2026-07-31 拆成兩段：
   - ✅ **4a 純授權**——主管才能核准 / 退回 / 拒絕；預支款整塊限主管；只有申請人能修改 / 送審自己的草稿；專案建立 / 結案限主管
   - ⬜ **4b 預支款領款人模型**——見下方
5. ⬜ 員工只看自己的報銷單——列表與詳情依 `ApplicantUserId` 過濾，主管看得到全部
6. ⬜ 帳號自助註冊與使用者管理——細節見下方

### 下一步：階段 4b — 預支款領款人模型

> 為什麼從階段 4 拆出來：4a 是改既有程式的判斷邏輯，4b 要動 Domain model 加 migration，混在一起 review 會太痛苦。
> 設計決策與理由見〈已定案的設計決策〉的領款人與預支款用途兩條。

**要做的：**

1. ⬜ `CashAdvance` 加 `PayeeUserId`，`PayeeName` 保留為姓名快照（比照既有的「UserId + 姓名快照」慣例）
2. ⬜ 新增 `Domain/CashAdvances/CashAdvanceUsage`：`{ Personal, PettyCash }`（命名待確認），UI 文案「個人預支 / 零用金（共用）」
3. ⬜ 建立預支款的領款人改成使用者下拉。**需要新增「列出可選使用者」的查詢**——目前沒有，要在 `Application/Identity` 加一個介面（例如 `IUserDirectory`）+ Infrastructure 用 `UserManager` 實作。階段 6 的使用者管理頁也會用到，不算白做。
4. ⬜ migration
5. ⬜ 報銷單的預支款下拉過濾（落點 Application Service）：零用金全員可選；個人預支只有 `PayeeUserId == 登入者` 可選
6. ⬜ 預支款可見度放寬：主管看全部，員工看「自己是領款人」的（把 4a 的整塊 `[Authorize]` 改成細一點的規則）
7. ⬜ 預支款列表與詳情要顯示用途類型（個人預支 / 零用金），否則新欄位在畫面上是隱形的
8. ⬜ 建立預支款若選零用金，完成後顯示共用提示（現有 `TempData` 慣例只有 Success / Error，這個是提醒不是成功訊息，可能要加一個 warning 樣式）
9. ⬜ 報銷單詳情頁補上「對應預支款」資訊
10. ⬜ 回頭更新 `docs/architecture/authorization.md` 的「還沒做的」與 `cash-advance-reconciliation.md`

**已確認不用改的**：多人共用同一筆零用金的金額核算**已經天然支援**。`CashAdvanceAppService.GetApprovedAmountsByCashAdvanceAsync()` 是按 `CashAdvanceId` 分組加總所有已核准報銷單、不看申請人，所以零用金被三個人各報一筆，`已核准報銷` 會正確加總，結清邏輯不動。

**舊資料怎麼處理（2026-07-31 定案）：**

- **不刪任何東西。** `PayeeUserId` 設成 nullable，7 筆舊預支款留 null——沒人是領款人，所以沒人拿到「領款人可見」的權限。這跟 21 張舊報銷單 `applicant_user_id` 為 null 是同一個處理方式，不發明新規則。
- `PayeeName` 本來就要保留當姓名快照，所以畫面上仍顯示「Candy」等原本的名字，只是背後沒有 UserId。
- **`CashAdvanceUsage` 一律 backfill 成零用金**——這 7 筆的用途欄位字面上就寫「零用金」，backfill 剛好符合事實，而且它們是核對邏輯的測試 fixture，維持全員可選才不會擋住測試。
- 注意「migration 幫舊資料填什麼」和「新建表單預設什麼」是**兩個獨立的決定**，這次剛好方向相反（backfill 零用金、新建預設個人預支），各有各的理由。
- 舊資料的實際內容見〈開發環境狀態 / 開發 DB〉。

**還沒決定的：**

- `CashAdvanceUsage` 的 enum 命名與 UI 文案（目前提案 `{ Personal, PettyCash }`／「個人預支 / 零用金（共用）」）。

### 階段 6：帳號自助註冊與使用者管理

> 設計決策與理由見〈已定案的設計決策〉的「帳號生命週期」。

1. ⬜ 員工自助註冊頁（登入頁加連結，`[AllowAnonymous]`），註冊後狀態為 `Pending`
2. ⬜ `IsActive` → `UserAccountStatus` 三態 + migration
3. ⬜ 登入流程改成先驗密碼再判狀態，各狀態給明確訊息（實作用 `CheckPasswordSignInAsync` 只驗密碼不發 cookie，保留登入失敗鎖定）
4. ⬜ 使用者管理頁（限主管）：啟用 / 停用 / 設角色，並守住「至少一個啟用中的主管」（這條規則跨多個使用者，落點可能是 Application Service，屆時再判斷）
5. ⬜ 修改密碼頁（每個人改自己的）
6. ⬜ Seed 收斂成 bootstrap：僅 users 表為空時建一個主管 `Admin`，拿掉 demo 的 `employee`

### 暫緩，等權限做完再回頭

**「預支款已結清」後是否拿掉修改功能。** Amber 原本決定要拿掉，但接著判斷「先把權限補起來再回頭看，方向會更明確」——因為「誰能改」會影響「還能不能改」。回頭時要決定的具體範圍：

- 已結清後要鎖的：預支款的用途 / 預支金額、結清紀錄的結清日期 / 金額 / 備註。
- **尚未決定**：「標記為不採用」要不要一起鎖。保留它等於留一條更正出口（不採用後回到未結清就能重做）；一起鎖則是完全凍結，打錯字只能等「已核准報銷單更正 / 取消核准」流程。
- 注意 `已結清` 包含兩種情況：差額為 0（根本不需要結清紀錄）、以及尚待結清為 0（結清紀錄補齊）。

### 應用面候補（尚未排序）

- **已核准報銷單更正 / 取消核准**流程：處理已核准後才發現選錯預支款、報銷單不成立、或需要從預支款核對中排除的情境。
- **附件 / 發票照片上傳**：會牽涉檔案儲存、安全性與大小限制。
- 若未來真的要做沖銷、付款憑證或出納日記帳，需另開會計帳範圍設計，**不建議混進第一階段**。

### 開發環境待辦

- 筆電也做一次 PostgreSQL 服務註冊與 `logging_collector` 設定，並設 `Identity:SeedPassword`。
- 兩台都穩定後，再考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`。**刪遠端分支屬不可逆操作，先確認再動。**
- 桌機舊 portable 備份 `.devtools.bak`、`.devdata.bak` 仍保留；**刪除前需 Amber 明確確認**。

---

## 本檔維護紀律

- **每項資訊只有一個出處。** 不在多處重述同一件事——重複的地方遲早只更新其中一處，變成互相矛盾。
- 需要交叉引用時，用「見〈某章節〉」指過去，不要複製內容。
- 更新時先刪過期內容，再寫新內容；不要讓做完的待辦跟有效待辦並存。
- **不放「當下瞬間狀態」**：dev server 開著沒、port 有沒有在聽、最新 commit hash、有沒有未 commit 變更——這些下次開工就過期了，該現查（`git status` / `git log` / `Get-Service`）。本檔尤其不記錄自己的 commit hash，因為 commit 發生在寫檔之後，寫下去當下就是錯的。
