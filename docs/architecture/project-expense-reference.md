# Project 與報銷單的關係

本專案把 `Project` 做成獨立 aggregate root，因為專案資料有自己的生命週期：先建立專案，再被多張報銷單引用，之後也可能結案。報銷單不直接持有整個 `Project` 物件，只記錄 `ProjectId`，符合跨 aggregate 用 ID 參照的規範。

`ExpenseReport` 自己負責守住支出類型規則：一般支出不能連專案，專案支出必須有 `ProjectId`。Application Service 則負責查 repository，確認使用者選到的 `ProjectId` 真的存在、且目前仍是可使用的專案。

`Project` 自己負責專案生命週期的狀態轉換，目前 `Close()` 會把進行中專案改成已結案，並擋掉重複結案。報銷單建立流程仍在 Application Service 查詢 `Project`，確認專案是 `Active` 才允許建立專案支出；專案結案前也由 Application Service 查詢該專案是否還有 `Draft`、`Submitted`、`Returned` 報銷單。這讓跨 aggregate 查詢留在 Application Service，單一 aggregate 的狀態規則留在 Domain entity。

同一條規則也套用在報銷單送審：如果草稿報銷單連到的專案後來已結案，Application Service 會擋掉送審。這不是 `ExpenseReport` 自己能判斷的事，因為它只保存 `ProjectId`，不知道另一個 aggregate 目前狀態。
