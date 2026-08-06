# 授權規則放在哪一層

上一篇〈登入與角色在 ExpenseLite 怎麼用〉講的是「**是誰**在操作、怎麼把他記進資料」。這篇講「**他能不能**做這件事」，也就是授權（authorization）。

本專案的授權分成兩種，**落點不同**，這是這篇的重點。

## 兩種授權，兩個落點

| 類型 | 問題長相 | 需要載入資料嗎 | 落點 | 本專案的例子 |
| --- | --- | --- | --- | --- |
| 角色授權 | 「你是不是主管？」 | 不用，看 cookie 裡的角色 claim 就知道 | **Controller** 的 `[Authorize(Roles = ...)]` | 核准 / 退回 / 拒絕報銷單；預支款的建立 / 修改 / 結清；建立 / 結案專案 |
| 資源授權 | 「這筆資料是不是你的？」 | 要，得先把資料讀出來才知道申請人 / 領款人是誰 | **Application Service** | 只有申請人能修改 / 送審自己的報銷單；只有主管或領款人看得到一筆預支款 |

判斷方法很簡單：**如果不用讀資料庫就能決定，那就擋在 Controller。** 反過來，只要判斷需要「這筆資料的某個欄位」，Controller 就不該做——它得先呼叫 Application Service 拿資料，那就已經進到 Application 層了，不如一開始就在那裡判斷。

### 角色授權：擋在 Controller

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = ExpenseLiteRoles.Manager)]
public async Task<IActionResult> Approve(...)
```

Controller 仍然是薄的——`[Authorize]` 不是它「寫」的邏輯，是它宣告的一個條件，實際執行的是 ASP.NET Core 的授權中介層。這比在 Application Service 裡寫 `if (!isManager) throw` 好，因為請求連 Controller 都進不來，也不用把「角色」這個 Identity 概念傳進 Application。

`CashAdvancesController` 一開始是標在**整個 class** 上的，因為那時預支款的每個動作都限主管。後來領款人存了 UserId、可以判斷「這筆是不是你的」之後，class 層的標註就拆掉了，改成只標在會**動資料**的 action（建立、修改、登記 / 修改 / 不採用結清）；列表與詳情這兩個**看**的 action 不標，改由 Application Service 依領款人過濾。這是一個典型的演進：**能用角色表達的就用 `[Authorize]`，表達不了的才往下沉。**

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

預支款的可見度是同一種東西，只是判斷條件多了角色：

```csharp
private static void EnsureCanBeViewedBy(CashAdvance cashAdvance, CurrentUser viewer)
{
    if (viewer.IsManager || cashAdvance.PayeeUserId == viewer.UserId)
    {
        return;
    }

    throw new ForbiddenOperationException("只有主管或這筆預支款的領款人可以查看。");
}
```

列表用同一條規則的布林版本先過濾再算總筆數，`GetDetailsAsync` 則直接丟例外。**先過濾再算總數**是刻意的：否則員工進到空列表會看到「沒有符合篩選條件」，但其實是那些資料根本不該給他看。

報銷單的可見度規則長得一模一樣（主管看全部，否則要是申請人本人），但它被抽成一個獨立的靜態類別 `Application/ExpenseReports/ExpenseReportVisibility`：

```csharp
public static bool CanBeViewedBy(ExpenseReport report, CurrentUser viewer)
    => viewer.IsManager || report.ApplicantUserId == viewer.UserId;
```

抽出來的理由不是「比較漂亮」，而是**同一條規則有兩個呼叫點**：`ExpenseReportAppService`（報銷單列表與詳情）和 `ProjectAppService`（專案詳情頁的相關報銷單）。預支款目前只有 `CashAdvanceAppService` 一個呼叫點，所以還留在那個 service 裡當私有方法——等它也長出第二個呼叫點再抽，不用預先統一。

### 授權看的是「資料」，不是「頁面」

專案詳情頁是這條規則的好例子。這一頁本身**所有人都進得來**（員工要選專案），但頁面中間那張「相關報銷單」表格列的是報銷單，就得照報銷單的可見度過濾。少了這一步，員工被擋在 `/ExpenseReports` 之外，卻可以從專案頁看到同事的單子標題、金額、狀態——**權限做在頁面上就會留下這種側門，做在資料上才不會。**

同一頁上的「未完成報銷單」「報銷單總數」兩個統計數字也跟著過濾後的清單算。數字看起來人畜無害，但它一樣在回答「別人有幾張單」；而且如果詳情頁過濾了、列表頁的數字沒過濾，員工會在兩個畫面看到互相矛盾的數字，那比洩漏更難解釋。專案列表的數字則是把條件下推到 EF Core 查詢（`CountUnfinishedProjectReportsAsync(applicantUserId)`），不是撈出來再過濾——這個查詢本來就是 group by，多一個 where 條件比載入全部報銷單划算。

#### `CurrentUser` 為什麼多了 `IsManager`

前一篇說「Application 不認識角色，角色檢查留在 Controller」。這裡加了 `CurrentUser.IsManager`，看起來像打臉，其實界線沒變：

- **純角色檢查**（「只有主管能核准」）仍然擋在 `[Authorize]`，Application 不參與。
- 但「**同一份資料，主管看得比較多**」不是純角色檢查——它要同時看角色與資料的擁有者，`[Authorize]` 表達不出來。這時 Application 就得知道呼叫者是不是主管。

`CurrentUser` 是 Application 自己的型別（不是 `ClaimsPrincipal`），所以多一個布林值不會讓 Application 綁到 ASP.NET Core Identity 上。Domain 仍然完全不認識角色。

### 不是授權，卻常被誤認的：誰能引用一筆預支款

報銷單的「對應預支款」下拉只列出領給你自己的預支款，這**不是**權限規則——連主管都不能拿別人的預支款來報自己的單。預支款本來就是公司先撥給某個人的錢，一人一筆，理由見〈預支款為什麼獨立成 CashAdvance〉。

分辨方法：**如果換一個角色答案就會變，那是授權；如果不管誰來答案都一樣，那是模型或業務規則。**

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
| 看報銷單列表 / 詳情 | **主管，或申請人本人** |
| 建立報銷單 | 所有登入者，申請人自動是自己 |
| 修改 / 送審報銷單、增刪改明細 | **只有申請人本人**（主管沒有例外） |
| 核准 / 退回 / 拒絕 | 只有主管 |
| 看預支款列表 | 所有登入者，但只列出自己是領款人的；主管看全部 |
| 看預支款詳情（財務核對視角） | **主管，或這筆的領款人本人** |
| 建立 / 修改預支款、登記 / 修改 / 不採用結清紀錄 | 只有主管 |
| 報銷單引用某筆預支款 | 只有領款人本人（**不是授權，是模型規則**） |
| 專案列表 / 詳情 | 所有登入者（員工建報銷單要選專案），但頁上的相關報銷單與統計數字照報銷單的可見度過濾 |
| 建立 / 結案專案 | 只有主管 |

預支款收斂成一人一筆之後，可見度與可引用性就對齊了：員工在列表與詳情只看得到自己是領款人的那幾筆，報銷單下拉也只列得出同一批。主管仍然看得到全部，因為登記結清是主管的工作。

## 還沒做的

- **主管可以核准自己的報銷單。** `Approve()` 只檢查狀態，沒有比對「審核人 ≠ 申請人」——這正是上面那個 tradeoff 的實例：沒人寫那條檢查，就沒有任何一層會擋。卡點不在技術，是這間公司只有一位主管，直接擋掉他自己的單就永遠核不了。
- **預支款的領款人建立後無法修改。** 選錯人只能靠建立當下看清楚，系統沒有更正出口（也沒有刪除功能）。要不要開放修改、開放到什麼條件，還沒決定。
