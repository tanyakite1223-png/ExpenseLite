# 列表篩選為什麼放在 Application Service

報銷單與預支款列表篩選屬於查詢 / 呈現需求，不是單一 entity 自己的狀態轉換或 invariant，所以不放進 `ExpenseReport` 或 `CashAdvance` domain entity。

目前做法是 Controller 接收 query string，轉成 Application 層的 query DTO，再由 Application Service 組出列表頁 DTO。View 只負責顯示篩選表單、保留目前條件與渲染結果。

報銷單列表會依關鍵字、狀態、支出類型、付款方式篩選；其中關鍵字只搜尋標題與申請人，狀態類條件交給下拉選單。預支款列表會先由 Application Service 加總已核准報銷金額，算出「未結清 / 已對上 / 公司需補付 / 員工需繳回」這種核對分類，再依關鍵字與核對狀態篩選；其中關鍵字只搜尋領款人與用途。

這裡沒有新增 repository query method，是因為目前資料量與練習階段都還小，沿用既有 `ListAsync()` 後在 Application Service 篩選比較直覺。之後若列表資料量變大，再把篩選條件下推到 repository / EF Core query，讓資料庫負責過濾。
