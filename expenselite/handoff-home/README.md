# 交接單：首頁「等你處理的事」

給 Claude Code 的實作指示。畫面已經寫好，缺的是後端資料。

## 現況

`HomeController.Index()` 回傳 `View()`，沒有 model。`Views/Home/Index.cshtml` 是十行的佔位頁。

## 要做的事，共四步

### 1. 覆蓋兩個檔案（已寫好，不需要改）

- `Views/Home/Index.cshtml` ← 用本資料夾的 `Index.cshtml`
- `wwwroot/css/expenselite.css` ← 用本資料夾的 `expenselite.css`

view 裡不含任何 `style=""`，樣式全在 CSS。**不要在 view 裡加行內樣式**，需要新的視覺就在 CSS 的「ExpenseLite 應用層」加 `.el-*` class。

### 2. 新增 DTO

放在 `Application/Home/HomePageDto.cs`：

```csharp
namespace ExpenseLite.Application.Home;

public sealed record HomePageDto(
    string DisplayName,
    bool IsManager,
    HomeTodoDto? Lede,
    IReadOnlyList<HomeTodoDto> Items);

/// <param name="Count">數字，畫面上是大字。</param>
/// <param name="What">「張報銷單等你審核」——接在數字後面唸得通的句子。</param>
/// <param name="Why">一句補充，讓人知道急不急。</param>
/// <param name="Url">點過去的目標，含篩選參數。</param>
/// <param name="NeedsAttention">true 會轉洋紅：作廢沒收拾、退回要改這類。</param>
public sealed record HomeTodoDto(
    int Count,
    string What,
    string Why,
    string Url,
    bool NeedsAttention);
```

### 3. 新增 `HomeAppService`

放在 `Application/Home/HomeAppService.cs`。注入 `IExpenseReportRepository` 與 `ICashAdvanceRepository`。

**排序規則：Lede 是清單裡最該先做的一件，其餘按下面的順序放進 Items。**

主管（`viewer.IsManager`）看到的項目，依序：

| 項目 | 條件 | Why 的內容 | NeedsAttention |
| --- | --- | --- | --- |
| 等你審核 | `Status == Submitted` 且 `ApplicantUserId != viewer.UserId` | 最久那張等了幾天（`SubmittedAt` 或最後一筆 ReviewRecord 的時間） | false |
| 作廢沒收拾 | 預支款上 `VoidedRelatedReportCount > 0` 且該預支款未結清 | 「已核准報銷」已扣掉，但結清紀錄要手動處理 | **true** |
| 可最終結清 | `RemainingSettlementAmount > 0` 且 `!HasInProgressReports` | 相關報銷單都已核准或拒絕，算式已定案 | false |
| 仍在流程中 | `HasInProgressReports` | 核對金額只是暫估，先不要結清 | false |

員工看到的，依序：

| 項目 | 條件 | Why 的內容 | NeedsAttention |
| --- | --- | --- | --- |
| 被退回要改 | 自己的單且 `Status == Returned` | 最後一筆 ReviewRecord 的 Reason | **true** |
| 草稿還沒送審 | 自己的單且 `Status == Draft` | 有幾張缺明細送不出去 | false |
| 預支款還沒用完 | `PayeeUserId == viewer.UserId` 且 `RemainingSettlementAmount > 0` | 用途 + 尚可報銷金額 | false |

**Count 為 0 的項目不要放進清單**，不要顯示「0 張等你審核」。全部為 0 時 `Lede` 傳 `null`，`Items` 傳空集合——view 會顯示「目前沒有等你處理的事」。

**核對金額不要自己重算。** `CashAdvanceAppService` 裡已經有 `BuildSettlementSummary` 那套算式（差額 = 已核准報銷 − 預支金額，正數公司補付、負數員工繳回）。把它抽成可共用的內部方法，或讓 `HomeAppService` 走 `CashAdvanceAppService.ListPageAsync` 拿 `CashAdvanceListItemDto`（那個 DTO 已經帶 `RemainingSettlementAmount`、`HasInProgressReports`、`ReconciliationStatus`）。**不要複製一份算式出來**，兩處會漂移。

`VoidedRelatedReportCount` 目前只在 `GetDetailsAsync` 算，`CashAdvanceListItemDto` 沒帶。需要的話把 `GetVoidedRelatedReportCountsAsync` 的結果也餵進 `MapListItem`。

### 4. 改 `HomeController.Index`

```csharp
public async Task<IActionResult> Index(CancellationToken cancellationToken)
{
    var page = await _home.GetPageAsync(CurrentUser(), cancellationToken);
    return View(page);
}
```

`CurrentUser()` 的取法跟其他 controller 一致。記得在 `Program.cs` 註冊 `HomeAppService`。

## Url 怎麼給

用 `Url.Action` 產生，帶上篩選參數，讓人點過去就看到那批單子：

- 等你審核 → `ExpenseReports/Index?status=Submitted`
- 被退回 → `ExpenseReports/Index?status=Returned`
- 草稿 → `ExpenseReports/Index?status=Draft`
- 預支款相關 → `CashAdvances/Index`，或單筆時直接指到 `CashAdvances/Details/{id}`

單筆的時候指到那一筆，比指到列表好。

## 不要做的事

- 不要加圖表、不要加「本月總支出」這類統計數字。這頁的定位是待辦，不是儀表板。
- 不要為了填版面湊項目。清單空著是正常狀態。
- 不要在 view 裡寫 `style=""`。
- 不要動 CSS 檔案上半部的 Broadsheet token 與元件，那是設計系統原檔。

## 驗收

1. 主管登入，有 4 張別人送審的單 → 頭條顯示「4 張報銷單等你審核」。
2. 主管自己送的單不算進去。
3. 有預支款帶作廢單未收拾 → 該列數字與標題是洋紅。
4. 員工登入 → 看到自己的退回單當頭條，帶主管的退回原因。
5. 全部處理完 → 顯示「目前沒有等你處理的事」，只留右側新增入口。
6. 每一列點下去要到得了正確的篩選結果。

## 設計稿

`ExpenseLite 畫面設計.dc.html` 裡的 **5a** 區塊。用瀏覽器打開，右上可切主管／員工視角。那是設計參考，不是要複製的程式碼——要落地的是本資料夾的 Razor view。
