# 報銷單的生命週期

本篇補的是 `ExpenseReport` 上「單子怎麼結束」的一段——目前有兩種結束方式（`Voided` 與 `Cancelled`），刻意做得很不一樣。是誰能離開流程、能不能反悔、離開之後系統怎麼幫使用者收拾，都不是隨意選的。

## 完整狀態轉換

```
Draft ── Submit ─→ Submitted ── Approve ─→ Approved ── Void（主管）─→ Voided [終態，不可撤銷]
  │                    │                       │
硬刪除                  │                       └─（實際上不會再變）
（申請人）              │
                       ├── Return ─→ Returned ── Submit ─→ Submitted
                       │                 │
                       │                 └── Cancel（申請人）─→ Cancelled
                       │                                            │
                       │                                            └── Restore（申請人）─→ Returned
                       │
                       └── Reject ─→ Rejected [終態]
```

**能修改明細與基本欄位的**只有 `Draft` 與 `Returned`（`ExpenseReport.EnsureEditable` 用 whitelist 擋）。`Cancelled` 想改的話，得先 `Restore` 回 `Returned`——這是刻意的：軟刪之後直接編輯會讓「這張是活的還是死的」變得模糊。

## Voided 不可撤銷、Cancelled 可復活——為什麼不一樣

兩者都是「單子離開流程」，但背後的意思天差地遠。

| | Voided（主管作廢） | Cancelled（申請人軟刪） |
| --- | --- | --- |
| 從哪來 | `Approved` | `Returned` |
| 誰能做 | 主管，且不能是申請人本人 | 申請人本人 |
| 動過錢嗎 | 動過——已核准過就進入預支款核對算式了 | 沒動過——只是退回後的暫存 |
| 能反悔嗎 | **不能。**要救只能重打一張新單 | 能，`Restore` 回 `Returned` |
| 要理由嗎 | 必填，寫進審核紀錄 | 不用 |
| 對列表 | 預設顯示 | 預設隱藏，勾「顯示已取消」才出來 |

不對稱的核心是「有沒有動過錢」。已核准過的單就算被作廢，「這張單當初被批准過」是一件發生過的事——會計上、稽核上都要留住這件事。而且下游（預支款結清紀錄）可能已經被牽動；主管靠一鎚定音的作廢動作，才會謹慎判斷、也才記得去處理牽動的東西。**如果作廢可以撤銷，這條稽核鏈就沒有落腳點**。

Cancelled 反過來——退回單是主管請你回去改的，還沒動到任何錢。這時候申請人自己想放棄（發現重複報、發現不成立），本來就該給他一條路。而且既然沒動錢，反悔的成本也低，開 `Restore` 是划算的。

「作廢要救只能重打」在業界很主流（Concur、Zoho Expense、QuickBooks 都是這樣）。我們沒做「以此單為範本建立新單」的複製按鈕——第一階段刻意不做，補救本來就少發生，加一個永久欄位（`ClonedFromReportId`）跟一堆對應的顯示邏輯不划算。

## 草稿硬刪、退回軟刪——也不一樣

這是另一個不對稱。

- **草稿硬刪**：申請人按刪除，資料就從 DB 消失。原因是草稿從沒進過主管視野，也沒有審核紀錄、沒有結清紀錄綁著——刪掉真的沒有後遺症。就像沒 push 過的 commit，隨時可以丟。
- **退回軟刪**：退回過的單子代表**主管看過**、有一筆退回審核紀錄在。申請人放棄它可以，但主管想追蹤「上週那張處理了嗎」時得有得查。硬刪掉主管會看到單子憑空消失，也失去了退回紀錄的存在。

實作差異：草稿硬刪走 `IExpenseReportRepository.DeleteAsync`（`_dbContext.Remove`）；退回軟刪走 `ExpenseReport.Cancel()` 換一個狀態就好，資料完全不動。

代價要老實記著：**「所有存在過的報銷單」不再是一個等量集合**——草稿刪了就是不見了，退回刪了還在。半年後如果有人以為「軟刪除是全域機制」會踩到這個。這裡不對稱是因為草稿與退回單在主管視角上意義不同，不是統一原則沒做好。

`Cancelled` 的可見度也採一致原則：**列表預設隱藏、詳情頁 URL 進得去**。列表勾「顯示已取消」或明確在狀態下拉選 `Cancelled` 才會顯示——判斷寫在 `ExpenseReportAppService.MatchesFilter`。專案詳情頁的相關報銷單列表也套同一條規則，用 `Where(x => x.Status != Cancelled)` 過濾。

## 結清紀錄「完全手動」、系統只做兩個提示

作廢一張已核准的專案支出報銷單、綁的是個人預支款、預支款上還有已計入的結清紀錄——這個組合是本專案最麻煩的一段。

**麻煩的來源**：`已核准報銷 = Σ Approved 狀態的相關報銷單.TotalAmount`，是每次讀取即時算的。這代表**作廢的當下這張單就自動不算進去了，不用改任何結清紀錄**。但既有的結清紀錄不會自己動——現實中錢真的已經退了或撥了。

具體例子：預支款 5000，員工報了 3500 並核准，主管登記「員工繳回 500」的結清紀錄（差額 -1500，還剩 1000 尚待結清）。之後這張單被作廢——`已核准報銷` 變 0，差額變 -5000（員工要繳回 5000）；那筆「員工繳回 500」的結清紀錄還在，但方向對不上了。

三種可能的處理方向：

- **全自動**：作廢時把相關結清紀錄自動標記不採用。省事但危險——`Void` 一次就永久凍結，主管沒空間反悔
- **半自動**：作廢時列出結清紀錄讓主管勾選要不要一起標記不採用
- **完全手動**：作廢只改單子狀態，結清紀錄留給主管自己去處理

**本專案選完全手動**。理由是「錢實際上要怎麼補救」是主管的判斷，系統沒立場替他決定——把 500 元的繳回紀錄作廢、再補一筆 5000 元的繳回？還是先把 500 標記不採用、確認員工確實會補繳？系統不知道。既然如此就不動，把決定權留給主管。

但完全手動有個副作用：主管作廢完人就走了、忘了去處理結清紀錄。所以系統補**兩個提示時機**：

- **作廢前**：`ExpenseReport` 詳情頁的「作廢處理」表單，若 `AdoptedSettlementRecordCount > 0` 會顯示警告，加上瀏覽器 `confirm()`。**這是攔在動作發生前，讓主管當下就意識到後果**。
- **作廢後**：作廢單的詳情頁 + 相關預支款的詳情頁，只要 `VoidedRelatedReportCount > 0` 就持續顯示提示。**這是攔在後續被遺忘**——當下記得，一週後就忘了；提示長在資料附近，看到就會想起來。

兩個時機一個都不能少：只有「作廢前」的話，主管當下答應了、之後忘了；只有「作廢後」的話，作廢當下不會有充分猶豫。

### 這個「牽動幾筆」怎麼算的

`ExpenseReportAppService.GetAdoptedSettlementRecordCountAsync` 從 `_cashAdvances` 查該報銷單綁的預支款、數 `!IsVoided` 的結清紀錄；沒綁預支款（一般支出 / 員工墊款 / 零用金）永遠回 0。這個欄位放進 `ExpenseReportDetailDto.AdoptedSettlementRecordCount`。

預支款那邊的 `VoidedRelatedReportCount` 走另一條——`CashAdvanceAppService.GetVoidedRelatedReportCountsAsync` 撈全部報銷單、group by 該預支款、數 `Status == Voided`。放進 `CashAdvanceSettlementDetailDto`。

跨 aggregate 的統計都在 Application 層算、組進 DTO；Domain entity 不知道對方存在，繼續維持「跨 aggregate 用 ID 參照」的紀律。

## 「未完成」的定義也順手改成 whitelist

`ExpenseReport` 的狀態這次多了兩個（`Voided`、`Cancelled`），把既有「未完成報銷單」判斷全部一起改：

原本 blacklist：`Status != Approved && Status != Rejected`——只列出兩個「完成」的狀態。多一個狀態就要記得改這裡，容易漏。

現在 whitelist：`Status == Draft || Status == Submitted || Status == Returned`——直接列出三個「還在流程裡」的狀態。未來加狀態時**預設不算未完成**，比較安全。

影響的地方：`EfExpenseReportRepository.CountUnfinishedProjectReportsAsync` / `HasUnfinishedProjectReportsAsync`、`ProjectAppService.CountUnfinishedReports`。共三處，都改成 whitelist。

這也是 handoff 提到「加狀態不用改既有規則」的實踐——`EnsureEditable` 早就是 whitelist（只有 Draft 與 Returned 可改），這次順手把「未完成」也統一過來。
