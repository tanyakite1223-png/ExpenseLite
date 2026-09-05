# ExpenseLite — 專案背景（CONTEXT）

> 穩定的背景知識。新 session 看 `current-handoff.md` 就夠；這份在需要查某個功能細節、設計決策理由、或開發環境設定時才讀。
> 只追加、不常改；更新時只加新的，不要把舊的有效紀錄洗掉。

---

## 已完成功能

### 帳號與登入

- ASP.NET Core Identity 登入 / 登出；全站預設需要登入，例外要標 `[AllowAnonymous]`。**`[AllowAnonymous]` 目前只有兩個：登入頁與 AccessDenied**
- 兩種角色：員工 / 主管。登入帳號用 email 前綴，不是完整 email
- **帳號兩態 `UserAccountStatus { Active, Disabled }`**
- **沒有自助註冊。帳號只有兩個來源**：空系統時的 bootstrap，以及主管在使用者管理頁按「新增使用者」
- **登入先驗密碼、再判帳號狀態**。用 `CheckPasswordSignInAsync` 只驗密碼不發 cookie，保留登入失敗鎖定；通過後才 `SignInAsync`
- **使用者管理頁（限主管）**：新增使用者 / 啟用 / 停用 / 改角色 / 重設別人的密碼
- **修改密碼頁**（每個人改自己的，要驗舊密碼）。成功後 `SignOutAsync` 並跳回登入頁
- **首次登入強制改密碼**（2026-08-16）—— 詳見 `docs/architecture/user-accounts.md#首次登入強制改密碼`
- **系統至少保留一位啟用中的主管**：停用最後一位主管、把最後一位主管降成員工，兩個入口都擋
- **主管不能對自己動**（2026-08-16）
- **只剩一位日常主管時給紅色 alert**（2026-08-16）
- **緊急存取帳號（break-glass）**：bootstrap 建的 `Admin` 帶 `IsProtected`，不能停用、不能降級；登入時留 warning log、掛畫面橫幅、更新 `last_signed_in_at`
- **Bootstrap 取代 seed**：角色每次開機確認；帳號只在 users 表為空時建一個主管 `Admin`
- Identity 英文錯誤訊息由 `ChineseIdentityErrorDescriber` 換成中文

### 權限

- **只有主管可以**：核准 / 退回 / 拒絕 / 作廢報銷單；建立 / 修改預支款、登記 / 修改 / 不採用結清紀錄；建立與結案專案；管理使用者
- **只有申請人本人可以**修改 / 送審 / 刪除草稿 / 取消退回單 / 復活取消單
- **報銷單可見度：主管看全部，其他人只看自己是申請人的**
- **預支款：主管看全部，員工只看自己是領款人的**
- 權限不足回 HTTP 403，導到 `/Account/AccessDenied`

### 報銷單

- 列表、篩選（關鍵字、狀態、支出類型、付款方式、顯示已取消）
- 新增、詳情、修改草稿 / 退回單主檔與明細
- 送審 / 核准 / 退回 / 拒絕流程；審核紀錄
- **審核人不能是申請人本人**
- **付款方式三選一**：員工墊款 / 個人預支 / 零用金支付
- 明細單據類型：收據 / 發票（發票號碼必填）
- **附件 / 發票照片上傳：Amber 決定不做**（2026-08-16）

### 報銷單生命週期擴充（2026-08-09）

> 完整理由與圖見 `docs/architecture/expense-report-lifecycle.md`

| 動作 | 誰 | 從哪 | 到哪 | 能反悔 | 要理由 |
|------|-----|------|------|--------|--------|
| 刪除草稿（硬刪） | 申請人 | Draft | 不存在 | 不能 | 不用 |
| 取消退回單（軟刪） | 申請人 | Returned | Cancelled | 能（Restore） | 不用 |
| 復活 | 申請人 | Cancelled | Returned | — | — |
| 作廢已核准單 | 主管（且≠申請人） | Approved | Voided（終態） | 不能 | 必填 |

- 作廢時結清紀錄完全手動；兩個提示時機（作廢前確認框 + 作廢後持續提示）
- **狀態共 7 個**：Draft / Submitted / Returned / Approved / Rejected / Voided / Cancelled

### 預支款

- 建立與核對列表；領款人建立後不可修改
- **一人一筆，只有領款人本人的報銷單能引用**
- 用途可改，金額限「無報銷單引用且無已計入核對的結清紀錄」時才可改
- 實際結清紀錄：公司補付 / 員工繳回
- 結清紀錄修改（更正人維持原登記者）、標記不採用
- **已結清後整筆鎖定**（2026-08-12）：預支款主檔、結清紀錄修改、標記不採用三個入口全關
- 有流程中報銷單時顯示暫估核對，不允許新增最終結清紀錄

### 專案

- 建立與列表、結案（有未完成報銷單時不可結案）
- 已結案專案不可新增專案支出報銷單，既有草稿 / 退回單也不可送審
- 專案詳情頁：相關報銷單依可見度過濾 + 排除 Cancelled

### 費用類別

- 明細支出類別改為下拉選單（2026-08 已做）

### 列印報表（2026-08-16）

- `/ExpenseReports/Print?from=YYYY-MM-DD&to=YYYY-MM-DD`，未指定時預設本月
- 員工看自己已核准單、主管看全公司；4 個分組總表 + 依申請人分卡
- `@media print` CSS + `window.print()`，零 NuGet 套件

### 版型改版（進行中）

> 詳見 `current-handoff.md` 的版型改版進度

- Session 1（2026-09-05）：新 CSS + sidebar Layout + login Layout
- Session 2（2026-09-05）：Account / Home / Shared views

---

## 架構狀態

### 已落地的 pattern

- Controller → Application Service → Domain → Infrastructure 分層
- Rich Domain Model
- `ExpenseReport` Aggregate Root（`ExpenseDetail`、`ExpenseReviewRecord` 為內部 entity）
- `CashAdvance` Aggregate Root（`CashAdvanceSettlementRecord` 為內部 entity）
- `Project` Aggregate Root
- `Money` Value Object（`decimal`，DB `numeric(18,2)`，EF owned type）
- `IExpenseReportRepository` / `ICashAdvanceRepository` / `IProjectRepository`（一個 aggregate root 一個 repository）
- **`IUserDirectory`**（唯讀，只回姓名；Application 定義介面、Infrastructure 用 UserManager 實作）
- **`IUserAccountStore` + `UserAccountAppService`**（帳號建立與維護，與 IUserDirectory 刻意分開）
- **`ExpenseReportVisibility`**（「誰看得到一張報銷單」的單一出處）
- EF Core owned type mapping；enum 存字串（`HasConversion<string>()`）
- 列表查詢 DTO / 頁面 DTO 由 Application Service 組裝
- ASP.NET Core Identity：cookie 登入、角色、custom claims principal factory、custom error describer
- 登入者資訊經 `Application/Identity/CurrentUser` record 傳進 Application 與 Domain
- **授權分兩種落點**：角色授權用 `[Authorize(Roles = ...)]`；資源授權（這筆資料是不是你的）在 Application Service
- **`ForbiddenOperationException`** 與 `DomainRuleViolationException` 分開，由 `ForbiddenOperationExceptionFilter` 轉成 HTTP 403
- **`RequirePasswordChangeMiddleware`**（首次登入強制改密碼的全站閘門）
- **報銷單 7 個狀態的完整狀態機**（詳見 `docs/architecture/expense-report-lifecycle.md`）

### Web 層目錄慣例

- `/Web/Controllers`：只接 HTTP、model binding、呼叫 Application Service、回傳 View
- `/Web/ViewModels`：表單與頁面模型，不是 Domain Model
- `/Web/Views`：Razor View，只負責畫面呈現與表單送出
- `/Web/Filters`：MVC filter（目前只有 ForbiddenOperationExceptionFilter）
- `/Web/Middleware`：ASP.NET Core middleware（目前只有 RequirePasswordChangeMiddleware）
- `TempData` 訊息三種：`SuccessMessage`、`ErrorMessage`、`WarningMessage`（WarningMessage 目前無使用者，機制保留）

### `/docs/architecture/` 已有的篇章

- `layered-architecture.md`
- `expense-report-aggregate.md`
- `expense-report-lifecycle.md`（含完整狀態機、Voided vs Cancelled 不對稱）
- `money-value-object.md`
- `repository-and-ef-core.md`
- `cash-advance-reconciliation.md`（含「為什麼零用金不是預支款」與「已結清後鎖定」）
- `project-expense-reference.md`
- `list-filtering-queries.md`（含列印報表章節）
- `identity-and-authentication.md`
- `authorization.md`（含「授權看的是資料，不是頁面」）
- `user-accounts.md`（含自助註冊推翻紀錄、首次改密碼、主管不能對自己動）

### 有無偏離 CLAUDE.md 規範

**無明顯偏離。** 以下是各邏輯落點的判斷原則：

**放在 Domain entity**（屬於 aggregate 自己的規則 / 狀態轉換）：
- `ExpenseReport.Submit/Return/Approve/Reject/Void/Cancel/Restore`——狀態轉換、EnsureReviewerIsNotApplicant
- `CashAdvance.Create/UpdateBasicInfo/UpdateSettlementRecord/VoidSettlementRecord`
- `Project.Close()`

**放在 Application Service**（跨 aggregate 查詢、use case 編排、資源授權）：
- `EnsureCanBeEditedBy` / `EnsureCanBeViewedBy` / `EnsureCanBeManagedByApplicant`（申請人授權）
- `EnsureNotLastActiveManagerAsync`（跨多個使用者，放 App Service 而非 Domain，誠實的例外）
- `ExpenseReportVisibility`（兩個呼叫點才抽成獨立靜態類別）
- 列表篩選、DTO mapping、分組聚合

**授權的分層落點**：
- 不用讀 DB 就能判斷的擋在 Controller（角色）；需要讀資料才知道的擋在 Application Service（擁有者）
- View 隱藏按鈕不是權限，只是 UX；真正的把關永遠在 `[Authorize]` 或 Application Service

**後續可能優化**（先記著，不急）：
- 列表可見度過濾目前 in-memory，資料量大時可下推到 EF Core query
- `IdentityUserAccountStore.ListAllAsync` 是 N+1，人數少時無所謂
- `ExpenseReportAppService.GetProjectNameAsync` 是死碼

---

## 已定案的設計決策

> **不要再推翻或重新設計這些。** 下方列出理由以防止重複討論。

### 使用者與責任歸屬

- **角色只有兩種：員工 / 主管**。`Admin` 不是第三種角色
- **登入帳號用 email 前綴**，不是完整 email
- **「是誰做的」一律存兩個欄位**：`Guid?` UserId + 姓名字串（寫入當下的快照）
- **申請人 = 建立者，建立後不可修改。領款人建立後也不可修改**
- **修改結清紀錄不會換掉處理人**
- **主管沒有「代改草稿」的代理權**（責任歸屬會模糊）
- **主管本人可以建立報銷單，但不能核准自己的單**（需要另一位主管）

### 帳號生命週期（2026-08-08 大幅推翻舊設計）

> 完整理由見 `docs/architecture/user-accounts.md` 的〈推翻紀錄：原本有自助註冊〉

- **已推翻「員工自助註冊 → 主管啟用」**：沒有 SMTP 就無法安全處理「帳號已存在」的情況
- **不要再提議把自助註冊做回來**（沒有 SMTP 就會原地繞回同一個死結）
- **已推翻三態，收斂成兩態**：`Pending` 唯一的來源是自助註冊，拿掉後它是死狀態
- **建立的帳號一出生就是 Active**，角色由主管在建立表單直接選
- **系統至少要保留一個啟用中的主管**

### 緊急存取帳號 break-glass（2026-08-07）

- `Admin` 是緊急存取帳號，不是日常帳號
- 不能停用、不能降成員工，判斷依據是 `IsProtected` 旗標（不是比對帳號名稱）
- UI 沒有任何地方能開關；增減緊急帳號是刻意的手動 SQL 動作
- **告警三路**：伺服器 warning log、畫面紅色橫幅、使用者管理頁的最後登入時間

### 不做刪除使用者（2026-08-07）

- 系統只能停用帳號。業務資料表對 `users` 沒有外鍵，刪掉一個人那些 Guid 就指向不存在的人
- **停用才是對的模型**：不能登入，但歷史查得到

### 報銷單生命週期擴充（2026-08-09）

> 完整理由見 `docs/architecture/expense-report-lifecycle.md`

- **作廢不可撤銷**，要救只能重打新單（稽核鏈不能斷）
- **不做「以此單為範本」按鈕**（第一階段刻意跳過）
- **作廢時結清紀錄完全手動**
- **草稿硬刪、退回軟刪的不對稱是刻意的**（草稿沒進過主管視野）

### 預支款與零用金（2026-08-05 定案，2026-08-06 實作）

> 完整理由見 `docs/architecture/cash-advance-reconciliation.md`

- **零用金不是預支款**（兩者放進同一條核對算式必然算錯）
- **不要再提議把零用金做回預支款**
- **零用金是一種付款方式，不是一筆資料**
- **付款方式留在報銷單層級，不下沉到明細**
- **抽屜裡的現金餘額不進系統**

### 專案與代墊

- **「專案支出」是分類與歸屬，不是請款依據，也不是專案損益**
- **代墊客戶購買商品的「向客戶請款」不進系統**
- **「業務靠專案詳情頁看到全部代墊資料」這個用途已作廢**（2026-08-06）

### 結清紀錄

- **不做沖銷 / 會計帳**
- **「不採用」的對象是「一筆結清紀錄」**，不是報銷單也不是預支款
- **已結清後鎖定（2026-08-12）**：保守做法，之後需要更正要另開正式更正流程
- **仍沒鎖定「已結清後又被新單引用並核准」**——已知缺口，待觀察

### 應用面候補（尚未排序，未決定）

- **UI 用詞**：「不採用」按鈕與狀態欄受詞不明確，考慮改成「不採用此筆結清」。Amber 尚未決定

---

## 開發環境狀態

### User Secrets

`UserSecretsId`：`expenselite-local-dev`。兩個 key，**都不進 repo**：
- `ConnectionStrings:ExpenseLite`——PostgreSQL 連線字串
- `Identity:SeedPassword`——bootstrap 建立 Admin 帳號的密碼

新機器：`dotnet user-secrets set "Identity:SeedPassword" "<密碼>"`

**注意**：`dotnet run --no-launch-profile` 不會載入 user secrets。

### 桌機 DB（scoop，PostgreSQL 18.4）

- binaries：`~\scoop\apps\postgresql\current\bin`
- data：`~\scoop\persist\postgresql\data`
- database：`expenselite_dev`；application user：`expenselite_app`
- **已註冊為 Windows 服務 `postgresql-18`**（Automatic 啟動）
  - 查詢：`Get-Service postgresql-18`
  - 手動啟停：`Start-Service` / `Stop-Service postgresql-18`（需 admin）
- `logging_collector = on`；log 位置：`~\scoop\persist\postgresql\data\log\`

### Migration 歷史（最新在上）

- `AddRequirePasswordChange`（2026-08-16）：`users.require_password_change` bool，backfill false
- `AddProtectedAccountAndLastSignIn`（2026-08-07）：`is_protected`、`last_signed_in_at`
- `ReplaceUserIsActiveWithStatus`（2026-08-07）：**手改過**，先加欄位再換算再砍舊欄的順序
- 桌機已套用最新。筆電停在舊版，pull 後要執行 `dotnet build` 再 `dotnet ef database update`

### 現有帳號（2026-08-07 重建後）

- `Admin` / 管理者 / Active / 主管 / **受保護**，密碼 `Admin123`
- `amber` / 王主管 / Active / 主管，密碼 `Amber123`
- `April` / 小波 / Active / 主管，密碼 `April123`
- `Butter` / 小奶油 / Active / 員工，密碼 `Butter123`

### 現有 Fixture（2026-08-09 灌入，SQL 未落檔進 repo）

- 專案 `11111111-...`：品牌識別更新案 - 綠地科技，Active
- 預支款 `22222222-...`：Butter 領 5000
- 報銷單 `33333333-...`：Butter 申請、個人預支綁上述預支款、3500，**已被 April 作廢（Voided）**
- 結清紀錄 `44444444-...`：員工繳回 500，仍被採用
- 2026-08-16 額外灌入列印熱鬧版 fixture（4 張 Approved 報銷單，SQL 未落檔）

### 疑難排解

- `Failed to connect to 127.0.0.1:5432`→ 先查 `Get-Service postgresql-18`，再看 `data\log\` 最新 log
- dev server 還在跑時 Windows 鎖住 exe → `Get-Process ExpenseLite | Stop-Process`
- `dotnet ef --no-build` 可能報 `PendingModelChangesWarning` → **動過 model 一律先 `dotnet build` 再跑 `dotnet ef`**
- **拿掉 enum 值時**，存字串的 enum DB 若有殘留資料會炸 → 先查 DB 有沒有殘留（教訓：2026-08-08 拿掉 `Pending` 時就遇到）
- **Migration 順序陷阱**：EF 預設「先 DropColumn 再 AddColumn」，換欄位型別的 migration 產生後一定要打開來看，必要時手改成「先加 → 換算 → 砍」
- **含中文的 SQL 用 `psql -c` 傳給 PowerShell 會噴 UTF8 錯誤** → 寫成 `.sql` 檔再用 `psql -f` 執行
- `psql` 不要讓它有機會問密碼（設 `$env:PGPASSWORD`，加 `-w`）
- **EF Core IQueryable 不能呼叫私有 C# helper method**（會 LINQ translation error）→ whitelist 條件直接寫在 LINQ expression 裡
- **`Invoke-WebRequest` 驗權限**：加 `-MaximumRedirection 0` 會丟例外而非回 302 → 改用 `HttpClient` 搭 `AllowAutoRedirect = $false`

### 開發環境待辦

- 筆電做一次 PostgreSQL 服務註冊與 `logging_collector` 設定，設 `Identity:SeedPassword`；pull 後套 migration
- 兩台穩定後，考慮清掉遠端舊分支 `origin/chore/dev-env-scoop`、`origin/chore/laptop-postgres-env`（刪遠端分支屬不可逆，先確認）
- 桌機舊 portable 備份 `.devtools.bak`、`.devdata.bak` 待 Amber 決定是否刪除
