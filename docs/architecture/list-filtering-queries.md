# 列表篩選為什麼放在 Application Service

報銷單與預支款列表篩選屬於查詢 / 呈現需求，不是單一 entity 自己的狀態轉換或 invariant，所以不放進 `ExpenseReport` 或 `CashAdvance` domain entity。

目前做法是 Controller 接收 query string，轉成 Application 層的 query DTO，再由 Application Service 組出列表頁 DTO。View 只負責顯示篩選表單、保留目前條件與渲染結果。

報銷單列表會依關鍵字、狀態、支出類型、付款方式篩選；其中關鍵字只搜尋標題與申請人，狀態類條件交給下拉選單。預支款列表會先由 Application Service 加總已核准報銷金額，算出「未結清 / 已對上 / 公司需補付 / 員工需繳回」這種核對分類，再依關鍵字與核對狀態篩選；其中關鍵字只搜尋領款人與用途。

這裡沒有新增 repository query method，是因為目前資料量與練習階段都還小，沿用既有 `ListAsync()` 後在 Application Service 篩選比較直覺。之後若列表資料量變大，再把篩選條件下推到 repository / EF Core query，讓資料庫負責過濾。

## 列印報表：同一種思路的另一個例子

2026-08-16 新增的 `/ExpenseReports/Print` 也走同一條路——`ExpenseReportAppService.GetPrintReportAsync` 拿區間 + `CurrentUser` viewer，內部套 `ExpenseReportVisibility` 過濾（員工只看自己、主管看全部）、篩 Approved、按送審時間切區間，然後把結果一次整成 `PrintReportDto`（含 4 個維度的分組小計）交給 View。View 只負責 `@media print` CSS 與 `window.print()` 觸發，不做任何篩選或聚合。

跟報銷單列表最大的差別是**多做了「分組聚合」**（按申請人 / 支出類型 / 付款方式 / 專案）。這件事本來也可以放在 View（Razor 可以直接 `.GroupBy`），但**聚合是業務決策**——「日常主管扣掉 Admin」「零用金支付單獨列」「非專案支出獨立成一組」這些定義若變動，希望改一個地方。分組後**每組的 label 也在 App Service 中文化**（`ExpenseTypeLabel` / `PaymentMethodLabel`），避免每個列表 view 各寫一套翻譯表。

**時區小陷阱**寫在 App Service：使用者輸入的 `DateOnly`（例如「8/1」）是本地觀點，`SubmittedAt` 存的是 UTC `DateTimeOffset`。把 `DateOnly` 綁本地 offset 成 `DateTimeOffset` 之後，`DateTimeOffset` 比較會自動用絕對時刻換算，不會在時區交界處差一天。全公司在同一個時區，這樣就夠——跨時區需求出現時才要再想。
