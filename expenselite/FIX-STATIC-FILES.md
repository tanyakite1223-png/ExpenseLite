# 修正單：靜態檔案被驗證擋住

## 症狀

任何頁面都沒有樣式。網址列出現：

```
localhost:5080/Account/Login?ReturnUrl=%2Fcss%2Fexpenselite.css
```

瀏覽器要載入 `/css/expenselite.css`，伺服器回一個「請先登入」的轉向——連未登入的登入頁自己都拿不到樣式，形成死結。

## 原因

`Program.cs` 有這段（設計本身是對的，不要拿掉）：

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

全站預設要求登入。而 `app.MapStaticAssets()` 註冊的是**端點**（endpoint），fallback policy 會套用到所有沒有明確標示的端點——包含靜態檔案端點。所以 CSS 被要求登入。

**注意**：把 `MapStaticAssets()` 移到 `UseAuthentication()` 之前沒有用。它不是中介軟體，是端點註冊，要等 `UseRouting()` 之後才匹配，所以它寫在哪一行都一樣會被 fallback policy 管到。這一步已經試過，無效。

## 修法

`Program.cs`，只改一行——在 `MapStaticAssets()` 後面接 `.AllowAnonymous()`：

```csharp
app.UseHttpsRedirection();
app.MapStaticAssets().AllowAnonymous();     // ← 加 .AllowAnonymous()
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
```

最後那個 `.WithStaticAssets()` 保留不動：

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
```

這樣靜態檔案端點被明確標示為不需驗證，fallback policy 不再套到它身上。全站預設要登入的設計完整保留，只有靜態檔案一個端點例外。

### 若 `.AllowAnonymous()` 編譯不過

某些版本 `MapStaticAssets()` 的回傳型別沒有這個擴充方法，改成：

```csharp
app.MapStaticAssets().WithMetadata(new AllowAnonymousAttribute());
```

`AllowAnonymousAttribute` 在 `Microsoft.AspNetCore.Authorization`，`Program.cs` 已經 using 了。

## 改完務必重啟

中介軟體與端點的順序是啟動時建立的，**熱重載改不了**。停掉 `dotnet run`（Ctrl+C）再重新啟動。

## 驗收

1. 直接開 `localhost:5080/css/expenselite.css` → 應該看到 CSS 內容，不是轉向登入頁。
2. 開 `localhost:5080/Account/Login` → 應該是**左右對開**版式：左邊大字標「ExpenseLite」加一條粗細雙線，右邊淺灰底的登入表單。若還是上下堆疊的無樣式畫面，樣式仍未載入。
3. 登入後看功能列 → 應該是**淡青底**，下方一條粗細雙線，目前所在模組的連結是青色。

---

# 另外兩件事（同一輪套用時發現的）

## 1. `expenselite/` 資料夾不可參與編譯

`D:\Workshops\Amber\ExpenseLite\expenselite\` 是樣板來源，裡面的 `.cshtml` 沒有 `_ViewImports`，被編譯會噴一堆 `CS0246 找不到型別`（`HomePageDto`、`LoginForm`、`ChangePasswordForm`…）。

**複製完就把整個 `expenselite/` 資料夾刪掉。** 內容都已經進專案了。

若要留著當參考，在 `ExpenseLite.csproj` 加排除：

```xml
<ItemGroup>
  <Content Remove="expenselite/**" />
  <None Include="expenselite/**" />
</ItemGroup>
```

## 2. 整份覆蓋會弄掉原檔 `@functions` 裡的方法

交付的 view 在 `@functions` 區塊只寫了新增的 mapper，其餘用註解標示「沿用原檔，未改動」。但整份覆蓋之後那些方法就不存在了，造成 `CS0103 名稱 'XXX' 不存在於目前的內容中`。

**修法**：用 git 從覆蓋前的版本取回這些方法，貼回各檔的 `@functions` 區塊。

```bash
git show HEAD:Web/Views/Users/Index.cshtml
```

需要補回的清單：

| 檔案 | 要補回的方法 |
| --- | --- |
| `Web/Views/Users/Index.cshtml` | `StatusText`、`LastSignInText` |
| `Web/Views/CashAdvances/Details.cshtml` | `ReconciliationText`、`SettlementTypeText`、`DisplayNote`、`DisplayDateTime`、`SettlementRecordStatusText` |
| `Web/Views/CashAdvances/EditSettlement.cshtml` | `SettlementTypeText` |
| `Web/Views/CashAdvances/VoidSettlement.cshtml` | `SettlementTypeText`、`DisplayNote` |
| `Web/Views/CashAdvances/Index.cshtml` | `ReconciliationText`、`ActionText`、`HasFilter`、`EmptyCashAdvanceMessage` |
| `Web/Views/ExpenseReports/Index.cshtml` | `StatusText`、`PaymentMethodText`、`ExpenseTypeText`、`DisplayProjectName`、`HasFilter`、`EmptyReportMessage` |
| `Web/Views/ExpenseReports/Details.cshtml` | `StatusText`、`PaymentMethodText`、`ExpenseTypeText`、`DisplayProjectName`、`DisplayCashAdvance`、`ReceiptTypeText`、`DisplayInvoiceNumber`、`ReviewActionText`、`DisplayReviewReason`、`NonEditableReasonText` |
| `Web/Views/ExpenseReports/Print.cshtml` | `ExpenseTypeText`、`PaymentMethodText`、`ReceiptTypeText`、`ReviewActionText` |
| `Web/Views/Projects/Index.cshtml` | `StatusText`、`EmptyProjectMessage` |
| `Web/Views/Projects/Details.cshtml` | `ProjectStatusText`、`ReportStatusText`、`PaymentMethodText` |

**這些是要補回的，不是要刪的。** 相對地，下面三個是**被新 mapper 取代、應該消失**的舊方法（整份覆蓋後自然不存在，不用補）：

- `ExpenseReports/Details.cshtml` 的 `StatusBadgeClass` → 已改用 `StatusClass`
- `Users/Index.cshtml` 的 `StatusBadgeClass` → 已改用 `StatusClass`
- `CashAdvances/Details.cshtml` 的 `SettlementRecordRowClass` → 已改用 `.el-settle-row--voided`

補完跑 `dotnet build`，應該 0 錯誤 0 警告。
