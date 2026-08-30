# 交接單：ExpenseLite 畫面改版

給 Claude Code 的實作指示。**畫面已經全部寫好**（25 支 Razor view ＋ 一支 CSS），這份文件說明怎麼套用，以及還缺哪些後端資料。

依序做完 A → B → C 三段即可。

---

## A. 套用檔案（純複製，不需要判斷）

### A1. 樣式與腳本

| 來源 | 目的地 |
| --- | --- |
| `expenselite.css` | `Web/wwwroot/css/expenselite.css` |
| `wwwroot-js/el-password-strength.js` | `Web/wwwroot/js/el-password-strength.js` |

### A2. Razor view

`views/` 底下 25 支檔案，**整份覆蓋**到 `Web/Views/` 對應位置。Model 型別、tag helper、controller action 名稱全部沿用原本的，只換了結構與 class。

```
views/Shared/_Layout.cshtml              → Web/Views/Shared/_Layout.cshtml
views/Shared/_LayoutLogin.cshtml         → Web/Views/Shared/_LayoutLogin.cshtml
views/Shared/_LoginPartial.cshtml        → Web/Views/Shared/_LoginPartial.cshtml
views/Shared/Error.cshtml                → Web/Views/Shared/Error.cshtml
views/Account/Login.cshtml               → Web/Views/Account/Login.cshtml
views/Account/ChangePassword.cshtml      → Web/Views/Account/ChangePassword.cshtml
views/Account/AccessDenied.cshtml        → Web/Views/Account/AccessDenied.cshtml
views/Home/Index.cshtml                  → Web/Views/Home/Index.cshtml          ← 需要 B1
views/Home/Privacy.cshtml                → Web/Views/Home/Privacy.cshtml
views/ExpenseReports/Index.cshtml        → …                                    ← 需要 B2
views/ExpenseReports/Create.cshtml       → …
views/ExpenseReports/Edit.cshtml         → …
views/ExpenseReports/Details.cshtml      → …
views/ExpenseReports/Print.cshtml        → …
views/CashAdvances/Index.cshtml          → …
views/CashAdvances/Create.cshtml         → …
views/CashAdvances/Edit.cshtml           → …
views/CashAdvances/Details.cshtml        → …
views/CashAdvances/EditSettlement.cshtml → …
views/CashAdvances/VoidSettlement.cshtml → …
views/Projects/Index.cshtml              → …
views/Projects/Create.cshtml             → …
views/Projects/Details.cshtml            → …
views/Users/Index.cshtml                 → …
views/Users/Create.cshtml                → …
views/Users/ResetPassword.cshtml         → …
```

`_ViewImports.cshtml`、`_ViewStart.cshtml`、`_ValidationScriptsPartial.cshtml` 不用動。

### A3. 刪掉不再需要的東西

- `Web/Views/Shared/_Layout.cshtml.css` — ASP.NET scoped CSS，跟單檔做法衝突，整個刪除。
- 覆蓋後，這三支的 `@functions` 裡有被新 mapper 取代的舊方法，若編譯器沒警告也請一併確認已消失（整份覆蓋的話它們自然不存在）：
  - `ExpenseReports/Details.cshtml` 的 `StatusBadgeClass` → 已改用 `StatusClass`
  - `Users/Index.cshtml` 的 `StatusBadgeClass` → 已改用 `StatusClass`
  - `CashAdvances/Details.cshtml` 的 `SettlementRecordRowClass` → 已改用 `.el-settle-row--voided`
- `Print.cshtml` 原本 view 內的 `<style>` 區塊已搬進 `expenselite.css` 的 `@media print`，不要留舊的。

### A4. Bootstrap

`expenselite.css` 最後有一層 Bootstrap 相容層（`.form-control` / `.alert` / `.badge` / `.btn-outline-*`），所以 Bootstrap CSS 可以先留著不會衝突。確認畫面正常後再移除 Bootstrap CSS，JS 若有用到 dropdown/modal 則保留。

### A5. appsettings.json

把 `appsettings.snippet.json` 的內容併進 `appsettings.json`：

```json
"App": {
  "Name": "ExpenseLite",
  "Tagline": "員工墊款、個人預支與零用金支付的申請、審核與核對。帳號由主管建立。"
}
```

兩支 Layout 用 `@inject IConfiguration Config` 讀它，涵蓋功能列字標、登入頁字標、`<title>`、登入頁副標。讀不到時 fallback 為 `"ExpenseLite"`。

---

## B. 需要補的後端資料

三項，B1 是新功能，B2／B3 是加欄位。

### B1. 首頁 DTO 與 AppService（必做，否則 `Home/Index` 編譯不過）

完整規格在 **`handoff-home/README.md`**，包含：

- `HomePageDto` / `HomeTodoDto` 的 record 定義
- 主管與員工各自看到哪些待辦項目、條件、Why 的內容、要不要標洋紅
- 排序規則、Url 該帶什麼篩選參數
- 6 條驗收項目

**三個容易做錯的地方**（該文件裡也有寫）：

1. **不要自己重算核對金額。** `CashAdvanceAppService` 已有 `BuildSettlementSummary` 那套算式（差額 = 已核准報銷 − 預支金額，正數公司補付、負數員工繳回）。抽成共用方法或走 `ListPageAsync` 拿現成 DTO，複製一份兩處會漂移。
2. **Count 為 0 的項目不要放進清單。** 不要顯示「0 張等你審核」。全部為 0 時 `Lede` 傳 `null`，view 會顯示「目前沒有等你處理的事」。
3. **主管不能審自己送的單。** 計算「等你審核」時要排除 `ApplicantUserId == viewer.UserId`。

### B2. `ExpenseReportListPageDto` 加兩個欄位

`ExpenseReports/Index.cshtml` 的抬頭那句用到：

```csharp
int UnfinishedCount,      // Draft + Submitted + Returned
int AwaitingReviewCount   // Submitted
```

員工視角只算自己的單，主管視角算全公司。

**若暫時不想改 DTO**：view 檔內有註解，把那句換成原本的「員工墊款、個人預支與零用金支付的申請與追蹤」即可。

### B3. `CashAdvanceListItemDto` 帶 `VoidedRelatedReportCount`

只有 B1 的首頁要顯示「作廢沒收拾」才需要。目前這個數字只在 `GetDetailsAsync` 算，把 `GetVoidedRelatedReportCountsAsync` 的結果也餵進 `MapListItem` 即可。

---

## C. 樣式規則（之後改任何畫面都要遵守）

### C1. Razor view 裡不准出現 `style=""`

一個都不行，包含 `style="margin-top:var(--space-3)"` 這種看起來無害的。目前交付的 25 支 view **零個** `style=""`，請維持。

理由：間距散在 25 支 view 裡，就沒有任何一處能一次調整。

### C2. 元件自己帶 margin

不要用 utility class 補間距。`.el-notice` 自帶 `margin-top`、`.el-split` 自帶 `margin-top`、`.el-title` 自帶 `margin`——view 直接寫 `<div class="el-notice">` 就對位。需要例外時加 modifier（例如 `.el-notice--rail`），不要在 view 裡覆蓋。

### C3. 表格欄寬用百分比 ＋ `nth-child`

```css
.el-table--reports th:nth-child(3){width:16%}
```

**不要用 px。** px 的總和一超出容器，瀏覽器就改用 min-content 分配，最有彈性的文字欄會被壓到一個字一行（這個 bug 發生過）。也不要寫在 `<th>` 上。

### C4. modifier 排在 base 之後

同權重時後面的贏。`.el-filters--inline` 必須寫在 `.el-filters` 之後，否則整組規則失效（這個 bug 發生過兩次）。

### C5. 檔案分層，順序不能換

| # | 層 | 要不要改 |
| --- | --- | --- |
| 1 | 中文字體（Noto Serif TC） | 不用 |
| 2 | Broadsheet token 與元件 | **不要改**，要調色改 `:root` |
| 3 | ExpenseLite 應用層 `.el-*` | 新樣式加在這裡 |
| 4 | Bootstrap 相容層 | 只為過渡，新 view 不要用 |

### C6. 其他

- 顏色、間距、字級一律 `var(--*)`，不要寫死 hex 或 px。
- 狀態色系：**青**＝流程順利（草稿／送審中／已核准），**洋紅**＝需要注意（退回／拒絕／作廢），**灰**＝不在流程中（已取消）。
- 版面用留白分隔，不要用框線或卡片切版。`.card` 只給列表項這種真正獨立的東西。
- 全部襯線字，不要為了 UI 引入無襯線字。
- 數字欄：金額用 `.num`（靠右，對齊個位數比大小），筆數用 `.count`（置中，靠右會貼到鄰欄看起來像同一組數字）。

完整 class 清單在 `README.md`。

---

## D. 驗收

套用完 A 段就可以跑起來（除了首頁需要 B1）。逐頁檢查：

- [ ] 登入頁是左右對開，左邊字標右邊表單
- [ ] 第一次登入被要求改密碼時，套的是登入版式的「第 2 步」，不是一般頁面
- [ ] 功能列是淡青底，下方一條粗細雙線，目前所在模組的連結是青色
- [ ] 報銷單詳情：草稿狀態看得到「送審」；主管看送審中的單會看到核准／退回／拒絕三顆按鈕
- [ ] 報銷單詳情：主管看已核准且有結清紀錄的單，作廢表單上方有洋紅色事前警告
- [ ] 預支款詳情：核對算式是直式帳本（一行一步），不是橫表
- [ ] 預支款列表：只有「預支金額」和「還要怎麼結」兩個金額欄
- [ ] 使用者列表：日常主管少於 2 位時最上方有洋紅提示
- [ ] 列印報表：按 Ctrl+P 預覽，換人會換頁，表頭跨頁會重複，連結是黑色不是青色
- [ ] 全站沒有殘留的 Bootstrap 藍色按鈕或藍色 focus 外框

### 已知限制

版面是流動式，**沒有響應式斷點**（使用情境是桌機為主）。約 1100px 以下使用者列表會很勉強，約 900px 以下主欄＋側欄的頁面會擠。若實測有問題再處理，選項有三個：整頁設最小寬度＋橫向捲軸、加斷點、或不處理。
