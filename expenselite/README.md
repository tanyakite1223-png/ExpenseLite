# expenselite.css — class 參考

一個檔案，涵蓋設計系統 token、元件、ExpenseLite 應用層樣式，以及 Bootstrap 相容層。

> **要套用到專案，看 `HANDOFF.md`。** 這份只列 class 清單。

## 安裝

1. 把 `expenselite.css` 放到 `wwwroot/css/`。
2. 在 `Views/Shared/_Layout.cshtml` 與 `_LayoutLogin.cshtml` 的 `<head>` 換成：

```html
<link rel="stylesheet" href="~/css/expenselite.css" asp-append-version="true" />
```

Bootstrap 的 CSS 可以先留著（相容層寫在後面會覆蓋它），確認畫面沒問題後再移除，只保留 Bootstrap 的 JS（如果有用到）。

## 檔案分四層

| 層 | 內容 | 要不要改 |
| --- | --- | --- |
| 中文字體 | Noto Serif TC，補 Source Serif 4 缺的中文字 | 不用 |
| Token + 元件 | 設計系統原檔：`--color-*` / `--space-*`、`.btn` `.input` `.table` `.tag` `.card` `.nav` | 不要改，要調色改 `:root` 就好 |
| ExpenseLite 應用層 | `.el-*`，這次設計的頁面骨架 | 新頁面用這層 |
| Bootstrap 相容層 | `.form-control` `.alert` `.badge` `.btn-outline-*` 對映到上面的樣式 | 現有 view 靠這層過渡，新頁面別用 |

## 常用 class

**版面** `.el-shell` `.el-page`（`--narrow` / `--form`）`.el-split`（主欄＋右側欄）`.el-rail` `.el-rail-label` `.el-rail-sep`

**功能列** `.el-nav` + `.el-nav-brand` + `.el-nav-user`，下面接一個 `<div class="el-rule"></div>`（報頭粗細雙線）

**頁首** `.el-head` `.el-title` `.el-sub` `.el-kicker` `.el-meta`

**狀態** `.tag .el-status .el-status--draft|submitted|returned|approved|rejected|voided|cancelled`
色系規則：青＝流程順利，洋紅＝需要注意，灰＝不在流程中。

**表格** `.table.el-table` ＋ 一個欄寬 modifier（`--reports` / `--details` / `--advances` / `--projects` / `--project-reports` / `--users` / `--slip`）；金額欄加 `.num`（靠右），筆數欄加 `.count`（置中），不換行加 `.nowrap`，粗體加 `.strong`；行內新增列 `.el-row-new`，行內編輯列 `.el-row-edit`；儲存格內並排按鈕包 `.el-cell-actions`

**提示** `.el-notice`（灰底墨線）`.el-notice--warn`（洋紅，作廢／退回）`.el-notice--quiet`

**表單** `.field` + `label` + `.input`；輔助說明 `.el-hint`；錯誤 `.el-error`；`.el-fields` 垂直排列（`--inline` label 與控制項同一行，`--tight` 標籤欄收窄）；`.el-filters` 篩選列（`--inline` 單一搜尋欄）；`.el-actions` 按鈕列；多行說明 `textarea.input.el-desc`

**句子式表單** `.el-sentence` 搭 `.seg` / `.seg-opt`；只在需要時出現的欄位包 `.el-conditional`

**送審檢查** `.el-checks` > `.el-check.el-check--ok` / `.el-check--bad`（勾與叉是 CSS 產生的）

**審核紀錄** `.el-timeline` > `.el-timeline-row` > `.el-timeline-at` / `.el-timeline-act` / `.el-timeline-why`

**核對算式（直式帳本）** `.el-ledger` > `.el-ledger-row`（`--total` 結論列）+ `.el-ledger-rule`（全墨等號線）；結論下的第二段包 `.el-ledger-nested`，其中尚待結清那列加 `--left`；每列內是 `.el-ledger-label` + `.el-ledger-num`

**結清紀錄** `.el-settle` > `.el-settle-row`（不採用加 `--voided`）> `.el-settle-at` / `.el-settle-type` / `.el-settle-note` / `.el-settle-sub` / `.el-settle-figure`（含 `.el-settle-amount` + `.el-settle-state`）

**核對狀態** `.tag .el-status .el-recon--settled|pending|unreimbursed`

**統計數字** `.el-stats` > `.el-stat` > `.el-stat-label` + `.el-stat-value`（非數量的值加 `--text`）

**唯讀資訊列表** `.el-facts` > `dt` + `dd`（標籤欄 112px，與 `.el-fields--inline` 同寬）

**列印報表** `.el-print-head`（`.no-print` 標記螢幕專用）`.el-print-range` `.el-print-total`；彙總 `.el-sums` > `.el-sum-title` + `.el-sum-row`；每位申請人一疊 `.el-sheet` > `.el-sheet-head` + `.el-sheet-rule` + `.el-slip`

**登入頁** `.el-login` > `.el-login-brand`（`.el-login-wordmark` + `.el-rule`）+ `.el-login-panel`；密碼強度 `.el-strength` > 三個 `<span>`（達標加 `.on`）+ `.el-rules`

## 系統名稱放在設定檔

把 `appsettings.snippet.json` 的內容併進 `appsettings.json`：

```json
"App": {
  "Name": "ExpenseLite",
  "Tagline": "員工墊款、個人預支與零用金支付的申請、審核與核對。帳號由主管建立。"
}
```

兩支 Layout 用 `@inject IConfiguration Config` 讀它，涵蓋功能列字標、登入頁字標、`<title>`、登入頁副標。改名字只要動這一處，而且仍然是真正的文字（可搜尋、可複製、螢幕閱讀器讀得到）。讀不到時 fallback 是 `"ExpenseLite"`，不會空白。

## 已改好的 Razor view

`views/` 底下是照定案畫面（3a／2a）改寫的版型，複製覆蓋到 `Web/Views/` 對應位置即可。Model、tag helper、controller action 名稱全部沿用你原本的，只換了結構與 class。

全部 25 支 view 都改完了，`Web/Views/` 底下沒有遺漏（`_ViewImports` / `_ViewStart` / `_ValidationScriptsPartial` 是設定檔，不需要改）。

| 檔案 | 備註 |
| --- | --- |
| `Shared/_Layout.cshtml` | 功能列＋報頭規線；新增 `Current()` helper 標記目前模組；系統名稱讀 `App:Name` |
| `Shared/_LoginPartial.cshtml` | 功能列右側使用者 |
| `Shared/_LayoutLogin.cshtml` | 登入頁左右對開版式 |
| `Shared/Error.cshtml` | 原本是 ASP.NET 樣板的英文開發說明，改成一句人話＋錯誤代碼 |
| `Account/Login.cshtml` | 去掉 form-floating，改用 `.field` |
| `Account/ChangePassword.cshtml` | 強制改密碼時套登入版式當「第 2 步」；密碼強度三格 |
| `Account/AccessDenied.cshtml` | — |
| `Home/Index.cshtml` | **需要新 DTO**，規格在 `handoff-home/README.md` |
| `ExpenseReports/Index.cshtml` | 抬頭用到 `UnfinishedCount` / `AwaitingReviewCount`，DTO 若還沒有，檔內註解有替代寫法 |
| `ExpenseReports/Create.cshtml` | — |
| `ExpenseReports/Edit.cshtml` | — |
| `ExpenseReports/Details.cshtml` | 作廢前後兩個結清提示都在；`StatusBadgeClass` → `StatusClass` |
| `CashAdvances/Index.cshtml` | **11 欄收成 7 欄**：六個金額欄合成「預支金額」＋「還要怎麼結」；`SettlementTypeText` → `DirectionText` |
| `CashAdvances/Details.cshtml` | 7 欄橫表改成直式帳本；`SettlementRecordRowClass` → `.el-settle-row--voided` |
| `CashAdvances/Create.cshtml` | — |
| `CashAdvances/Edit.cshtml` | 領款人／預支日期改成 `.el-facts` 脈絡，不當欄位 |
| `CashAdvances/EditSettlement.cshtml` | 同上；方向唯讀 |
| `CashAdvances/VoidSettlement.cshtml` | 要撤掉哪一筆攤在動作前面 |
| `ExpenseReports/Print.cshtml` | view 內 50 行 `<style>` 搬進 CSS 的 `@media print`；加了明細不被切開、表頭跨頁重複、連結轉墨色 |
| `Projects/Index.cshtml` | 狀態改標籤；有未完成報銷單時不畫結案鈕 |
| `Projects/Create.cshtml` | — |
| `Projects/Details.cshtml` | 四個框線方塊改成 `.el-stats` 留白分隔；多一個「已核准金額」 |
| `Home/Privacy.cshtml` | 原本是英文佔位文字，改寫成「系統存了你什麼」；不需要可整頁刪除 |
| `Users/Index.cshtml` | 「你自己」「緊急存取帳號」改 `.el-flag`；`StatusBadgeClass` → `StatusClass`；頁尾註解 `.el-page-foot` |
| `Users/Create.cshtml` | — |
| `Users/ResetPassword.cshtml` | — |
| `wwwroot-js/el-password-strength.js` | 放到 `wwwroot/js/`，純顯示，規則仍由 Identity 驗 |

各檔的 `@functions` 只列出新增的 mapper，其餘文字轉換函式沿用原檔，檔內有註解標明哪些可以刪。

## 規則

完整版在 `HANDOFF.md` 的 C 段。摘要：

- view 裡不准出現 `style=""`；元件自己帶 margin。
- 表格欄寬用百分比 ＋ `nth-child`，不要用 px。
- modifier 的 CSS 規則要排在 base 之後，否則同權重時失效。
- 顏色、間距、字級一律用 `var(--*)`。
- 不用框線和卡片切版面，用留白；`.card` 只留給列表項這種真正獨立的東西。
- 同一個小元件不要同時用青色和洋紅。
- 全部襯線，不要為了 UI 另外引入無襯線字。
