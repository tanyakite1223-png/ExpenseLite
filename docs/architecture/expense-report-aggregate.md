# 報銷單 Aggregate

本專案把 `ExpenseReport` 當作 Aggregate Root，`ExpenseDetail` 是它內部的 entity。

明細沒有自己的 repository，也不會在外面單獨建立後直接存檔；新增、移除明細都要走 `ExpenseReport.AddDetail()` / `ExpenseReport.RemoveDetail()`。這樣 `ExpenseReport` 才能保證送審後不可修改明細、總金額等於明細加總。

EF Core 只負責把這個結構存進 PostgreSQL。Aggregate 邊界不是 EF Core 會自動保護的東西，而是我們透過 repository 只暴露 `ExpenseReport` 這個 root 來維持的設計紀律。
