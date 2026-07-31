# 授權規則放在哪一層

上一篇〈登入與角色在 ExpenseLite 怎麼用〉講的是「**是誰**在操作、怎麼把他記進資料」。這篇講「**他能不能**做這件事」，也就是授權（authorization）。

本專案的授權分成兩種，**落點不同**，這是這篇的重點。

## 兩種授權，兩個落點

| 類型 | 問題長相 | 需要載入資料嗎 | 落點 | 本專案的例子 |
| --- | --- | --- | --- | --- |
| 角色授權 | 「你是不是主管？」 | 不用，看 cookie 裡的角色 claim 就知道 | **Controller** 的 `[Authorize(Roles = ...)]` | 核准 / 退回 / 拒絕報銷單；預支款整塊；建立 / 結案專案 |
| 資源授權 | 「這筆資料是不是你的？」 | 要，得先把報銷單讀出來才知道申請人是誰 | **Application Service** | 只有申請人能修改 / 送審自己的報銷單 |

判斷方法很簡單：**如果不用讀資料庫就能決定，那就擋在 Controller。** 反過來，只要判斷需要「這筆資料的某個欄位」，Controller 就不該做——它得先呼叫 Application Service 拿資料，那就已經進到 Application 層了，不如一開始就在那裡判斷。

### 角色授權：擋在 Controller

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = ExpenseLiteRoles.Manager)]
public async Task<IActionResult> Approve(...)
```

Controller 仍然是薄的——`[Authorize]` 不是它「寫」的邏輯，是它宣告的一個條件，實際執行的是 ASP.NET Core 的授權中介層。這比在 Application Service 裡寫 `if (!isManager) throw` 好，因為請求連 Controller 都進不來，也不用把「角色」這個 Identity 概念傳進 Application。

`CashAdvancesController` 是標在**整個 class** 上，因為預支款的每個動作都限主管。

### 資源授權：擋在 Application Service

```csharp
private static void EnsureCanBeEditedBy(ExpenseReport report, CurrentUser editor)
{
    if (report.ApplicantUserId != editor.UserId)
    {
        throw new ForbiddenOperationException("只有申請人可以修改或送審自己的報銷單。");
    }
}
```

`UpdateAsync`、`AddDetailAsync`、`UpdateDetailAsync`、`RemoveDetailAsync`、`SubmitAsync` 每一個都在載入報銷單之後、動它之前呼叫這個檢查。

## 為什麼不放進 Entity

`ExpenseReport` 自己就有 `ApplicantUserId`，理論上可以做成 `report.EnsureCanBeEditedBy(userId)`。這裡刻意不這樣做，理由是**「誰可以呼叫」和「資料是否合法」是兩件事**：

- 報銷單自己的規則是「只有草稿或退回狀態可以改」——這是 invariant，不管誰來改都成立，所以它在 entity 裡（`EnsureEditable`）。
- 「必須是申請人本人」是存取控制。它不影響資料合不合法，只決定這個請求該不該被受理。

如果把它放進 entity，`AddDetail`、`UpdateDetail`、`RemoveDetail`、`Submit` 每個方法都要多接一個 `currentUser` 參數，Domain API 會被權限概念污染。而 Domain 本來就不該認識「登入者」這個 Web 概念。

**Tradeoff 要老實講**：這代表如果哪天有人寫了一個新的 Application Service 方法卻忘了呼叫 `EnsureCanBeEditedBy`，Domain 不會幫你擋。這是靠約定維持的紀律，跟 aggregate 邊界一樣（見〈報銷單為什麼是 Aggregate Root〉）。換成放進 entity 就能強制，代價是上面說的污染。本專案選了前者。

## 為什麼要一個新的例外型別

新增了 `Application/Shared/ForbiddenOperationException`，沒有沿用既有的 `DomainRuleViolationException`：

| | 意思 | 該怎麼回應 |
| --- | --- | --- |
| `DomainRuleViolationException` | 這個動作在業務上不合法（金額超過、狀態不對） | 顯示訊息讓使用者修正後重試 |
| `ForbiddenOperationException` | 你不該碰這筆資料 | HTTP 403，導到 AccessDenied |

兩者都繼承 `InvalidOperationException` 但是**平行的兄弟**，所以既有的 `catch (DomainRuleViolationException)` 不會誤攔到權限例外。

轉成 HTTP 的工作在 `Web/Filters/ForbiddenOperationExceptionFilter.cs`：Application 只負責丟出「你沒權限」，不需要知道 403 或 AccessDenied 頁這些 HTTP 概念。用一個全域 filter 而不是在六個 action 各寫一次 `catch`，純粹是為了不重複。

## View 隱藏按鈕**不是**權限

這是最容易搞錯的一點。`Details.cshtml` 裡：

```csharp
var isApplicant = Model.Report.ApplicantUserId == User.GetUserId();
var canEdit = isApplicant && Model.Report.Status is ...;
```

這幾行只決定**按鈕要不要畫出來**，是 UX——讓人不要點到會失敗的東西。它擋不住任何攻擊：對方直接對 `POST /ExpenseReports/Submit/{id}` 送一個請求就繞過了，畫面根本沒參與。

**真正的把關永遠在伺服器端**：`[Authorize]` 或 Application Service 的檢查。所以這兩層都要有，而且不能只做 View 那層。反過來說，只做伺服器端、不改 View 也是安全的，只是使用者會點到按鈕才看到錯誤。

`ExpenseReportsController.Edit`（GET）多了一個 `Forbid()` 檢查，也屬於這一類：它讓非申請人連表單都看不到，但實際擋住存檔的是 `UpdateAsync` 裡的檢查。

## 目前的授權規則一覽

| 動作 | 誰可以做 |
| --- | --- |
| 看報銷單列表 / 詳情 | 所有登入者（**尚未**依申請人過濾，見下） |
| 建立報銷單 | 所有登入者，申請人自動是自己 |
| 修改 / 送審報銷單、增刪改明細 | **只有申請人本人**（主管沒有例外） |
| 核准 / 退回 / 拒絕 | 只有主管 |
| 預支款全部功能（含列表、詳情） | 只有主管 |
| 專案列表 / 詳情 | 所有登入者（員工建報銷單要選專案） |
| 建立 / 結案專案 | 只有主管 |

## 還沒做的

- **員工還看得到所有人的報銷單。** 列表與詳情尚未依 `ApplicantUserId` 過濾。
- **預支款的可見度目前過度保守。** 領款人（`PayeeName`）現在只是手動輸入的字串、沒有對應 UserId，所以無法過濾成「我的預支款」，只能整塊關給主管。等領款人改成存 UserId 之後，要放寬給領款人本人——保管零用金的人需要看得到那筆錢的使用狀態。
- **報銷單建立時的「對應預支款」下拉選單仍列出全部未結清預支款**，員工在那裡會看到其他人的領款人與金額。同樣要等領款人存了 UserId 才能過濾。
