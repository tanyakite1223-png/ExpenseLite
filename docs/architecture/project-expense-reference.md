# Project 與報銷單的關係

本專案把 `Project` 做成獨立 aggregate root，因為專案資料有自己的生命週期：先建立專案，再被多張報銷單引用，之後也可能結案。報銷單不直接持有整個 `Project` 物件，只記錄 `ProjectId`，符合跨 aggregate 用 ID 參照的規範。

`ExpenseReport` 自己負責守住支出類型規則：一般支出不能連專案，專案支出必須有 `ProjectId`。Application Service 則負責查 repository，確認使用者選到的 `ProjectId` 真的存在、且目前仍是可使用的專案。
