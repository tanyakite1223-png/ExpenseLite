# 列表篩選為什麼放在 Application Service

報銷單與預支款列表篩選屬於查詢 / 呈現需求，不是單一 entity 自己的狀態轉換或 invariant，所以不放進 `ExpenseReport` 或 `CashAdvance` domain entity。

目前做法是 Controller 接收 query string，轉成 Application 層的 query DTO，再由 Application Service 組出列表頁 DTO。View 只負責顯示篩選表單、保留目前條件與渲染結果。

報銷單列表會依關鍵字、狀態、支出類型、付款方式篩選；其中關鍵字只搜尋標題與申請人，狀態類條件交給下拉選單。預支款列表會先由 Application Service 加總已核准報銷金額，算出「未結清 / 已對上 / 公司需補付 / 員工需繳回」這種核對分類，再依關鍵字與核對狀態篩選；其中關鍵字只搜尋領款人與用途。

這裡沒有新增 repository query method，是因為目前資料量與練習階段都還小，沿用既有 `ListAsync()` 後在 Application Service 篩選比較直覺。之後若列表資料量變大，再把篩選條件下推到 repository / EF Core query，讓資料庫負責過濾。

## 列印報表：同一種思路的另一個例子

2026-08-16 新增的 `/ExpenseReports/Print` 也走同一條路——`ExpenseReportAppService.GetPrintReportAsync` 拿區間 + `CurrentUser` viewer，內部套 `ExpenseReportVisibility` 過濾（員工只看自己、主管看全部）、篩 Approved、按送審時間切區間，然後把結果一次整成 `PrintReportDto`（含 4 個維度的分組小計）交給 View。View 只負責 `@media print` CSS 與 `window.print()` 觸發，不做任何篩選或聚合。

跟報銷單列表最大的差別是**多做了「分組聚合」**（按申請人 / 支出類型 / 付款方式 / 專案）。這件事本來也可以放在 View（Razor 可以直接 `.GroupBy`），但**聚合是業務決策**——「日常主管扣掉 Admin」「零用金支付單獨列」「非專案支出獨立成一組」這些定義若變動，希望改一個地方。分組後**每組的 label 也在 App Service 中文化**（`ExpenseTypeLabel` / `PaymentMethodLabel`），避免每個列表 view 各寫一套翻譯表。

**時區小陷阱**寫在 App Service：使用者輸入的 `DateOnly`（例如「8/1」）是本地觀點，`SubmittedAt` 存的是 UTC `DateTimeOffset`。把 `DateOnly` 綁本地 offset 成 `DateTimeOffset` 之後，`DateTimeOffset` 比較會自動用絕對時刻換算，不會在時區交界處差一天。全公司在同一個時區，這樣就夠——跨時區需求出現時才要再想。

## 分頁（2026-09-14）

所有 list 頁（報銷單、預支款、專案、費用類別、使用者，以及專案詳情頁的「相關報銷單」）都固定每頁 20 筆，用同一顆 `PageInfo`（`Application/Shared/PageInfo.cs`）：`PageInfo.Create(page, totalCount)` 算出總頁數、把不合法的頁碼（<1 或超過總頁數）夾回邊界，App Service 再用它的 `Skip`/`PageSize` 切出當頁資料。View 端共用一個 `_Pagination.cshtml` partial，直接讀目前網址的 querystring（扣掉 `page`）組頁碼連結，所以不管報銷單列表有多少個篩選欄位，partial 都不用知道有哪些參數。

**分頁動作放在 Application Service，不下推到 EF Core `Skip`/`Take`**：跟既有篩選一樣，資料先用 `ListAsync()` 整批撈進記憶體再篩選、排序、切頁。理由同一份——資料量與練習階段都還小。之後資料量變大，篩選、排序、分頁要一起下推到 repository / EF Core query，不能只搬分頁。

**「共 N 筆」的 N 一定是篩選後、分頁前的總數**（`PageInfo.TotalItemCount`），不是當頁筆數——這是分頁後最容易犯的錯：View 原本圖方便直接拿 `Model.Reports.Count`，分頁後這個數字只會是 20 或更少。同樣的道理，專案詳情頁的「已核准金額」原本在 View 用 LINQ 對 `Model.ExpenseReports` 加總，分頁後那份清單只剩當頁，所以改成 App Service 算好整批的 `ApprovedExpenseReportAmount` 一起放進 DTO——這也是 §4.7「DTO 不該讓 View 對它做業務聚合」的自然結果，不是分頁才新增的規則，只是分頁把原本沒發作的問題揭出來。

**首頁待辦數用的是 `CashAdvanceAppService.ListItemsForViewerAsync`，不是分頁後的 `ListPageAsync`**：首頁要算「全部待辦數量」，如果重用會被分頁截斷的方法，數量在資料超過 20 筆後會悄悄算錯。兩個方法都呼叫同一個私有 `BuildListItemsAsync`，差別只在要不要篩選、分頁。
