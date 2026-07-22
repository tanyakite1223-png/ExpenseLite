# 報銷單 Aggregate

本專案把 `ExpenseReport` 當作 Aggregate Root，`ExpenseDetail` 是它內部的 entity。

明細沒有自己的 repository，也不會在外面單獨建立後直接存檔；新增、修改、移除明細都要走 `ExpenseReport.AddDetail()` / `ExpenseReport.UpdateDetail()` / `ExpenseReport.RemoveDetail()`。這樣 `ExpenseReport` 才能保證送審後不可修改明細、總金額等於明細加總。

審核紀錄 `ExpenseReviewRecord` 也放在 `ExpenseReport` aggregate 內。原因是核准、退回、拒絕本來就是報銷單自己的狀態轉換；狀態改變與審核人、審核時間、退回/拒絕原因必須一起成立，避免出現「狀態已退回但沒有紀錄」或「有紀錄但狀態沒變」的資料。

目前系統還沒有登入與權限，所以審核人先由畫面手動輸入。未來若加入使用者與角色，這裡可以從 `ReviewerName` 升級成目前登入者的 `UserId`，但審核紀錄仍然屬於報銷單流程的一部分。

EF Core 只負責把這個結構存進 PostgreSQL。Aggregate 邊界不是 EF Core 會自動保護的東西，而是我們透過 repository 只暴露 `ExpenseReport` 這個 root 來維持的設計紀律。
