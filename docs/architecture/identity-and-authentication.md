# 登入與角色在 ExpenseLite 怎麼用

本專案用 ASP.NET Core Identity 做登入與角色。這篇說明「東西放在哪一層、為什麼」，不是 Identity 的通用教學。

這篇的範圍是**認證**（authentication，「你是誰」）以及怎麼把操作者記進資料。**授權**（authorization，「你能不能做」）的落點在〈[授權規則放在哪一層](authorization.md)〉；**帳號本身**怎麼產生與維護（註冊、啟用、停用、改密碼）在〈[帳號生命週期與使用者管理](user-accounts.md)〉。

## 為什麼用 Identity 而不是自己做

密碼雜湊、登入 cookie、失敗鎖定這些東西做錯的代價很高，而且不做對也看不出來（能登入不代表安全）。Identity 是 .NET 內建、經過驗證的實作，所以這部分不自己寫。

代價是要接受它的資料表結構與一套慣例，並且多一個 NuGet 相依 `Microsoft.AspNetCore.Identity.EntityFrameworkCore`。

## 分層落點

這是本專案最需要注意的一點：**Identity 屬於 Infrastructure，Domain 不認識它。**

| 東西 | 放哪 | 為什麼 |
| --- | --- | --- |
| `ApplicationUser` | `Infrastructure/Identity` | 它繼承 `IdentityUser<Guid>`，直接依賴框架。放進 Domain 會讓 Domain 依賴 EF Core 與 ASP.NET Core，違反「Domain 不依賴任何人」。 |
| `ExpenseLiteRoles`（角色常數） | `Application/Identity` | 「員工 / 主管」是業務概念，不是 Identity 的實作細節。Web 的 `[Authorize]` 與 Infrastructure 的 bootstrap 都要用到，放 Application 兩邊都能取用。 |
| `UserAccountStatus`（帳號狀態） | `Application/Identity` | 同上，「這個帳號能不能用」是業務概念。詳見〈[帳號生命週期與使用者管理](user-accounts.md)〉。 |
| `ApplicationUserClaimsPrincipalFactory`、`ChineseIdentityErrorDescriber` | `Infrastructure/Identity` | 都是 Identity 的擴充點，屬於框架整合。 |
| 登入頁、`AccountController` | `Web` | 純粹是 HTTP 與畫面。發登入 cookie 也在這裡——那本來就是 Web 的事。 |

因為 Domain 不認識 `ApplicationUser`，報銷單之類的 aggregate 要記「是誰做的」時，**只存 `Guid` 形式的 UserId**，不持有使用者物件。這跟 §4.4「跨 aggregate 一律用 ID 參照」是同一個規則：使用者是另一個獨立的東西，不該被抓進報銷單的邊界裡。

## 「是誰做的」怎麼從登入者流進 Domain

報銷單的申請人、審核人，預支款結清的處理人、不採用處理人，以前都是表單上手動打的字串。現在改成自動帶入登入者，資料的流向是：

```
ClaimsPrincipal（登入 cookie）
  → Controller: User.ToCurrentUser()
  → Command: CurrentUser（Application 層的小 record）
  → Entity: report.Approve(reviewerUserId, reviewerName)
```

三個刻意的設計：

**1. Application 只認識 `CurrentUser`，不認識 `ClaimsPrincipal`。**
`CurrentUser(Guid UserId, string DisplayName, bool IsManager)` 是 Application 自己的型別。Controller 負責把 cookie 裡的身分翻譯成它——這正好是 Controller 該做的事（接 HTTP、model binding），所以 Controller 仍然是薄的。反過來如果讓 Application Service 直接讀 `HttpContext`，Application 就綁死在 Web 上了。（`IsManager` 只用在「同一份資料，主管看得比較多」這種判斷；純角色檢查仍留在 `[Authorize]`，理由見〈[授權規則放在哪一層](authorization.md)〉。）

**2. 同時存 UserId 和姓名，而且姓名是「快照」。**
每個地方都存兩個欄位，例如 `ApplicantUserId`（`Guid?`）和 `ApplicantName`（`string`）：

- **UserId** 是給程式用的：「只有申請人能修改自己的報銷單」就是靠它比對（見〈[授權規則放在哪一層](authorization.md)〉），之後的「員工只看自己的報銷單」也一樣。
- **姓名** 是給人看的歷史紀錄，**寫進去之後不會再跟著使用者改名而變動**。王小明改名成王大明，三年前那張報銷單上該顯示的仍然是當時的「王小明」。

如果只存 UserId、顯示時再去 join `users` 表，畫面會隨著人事異動而改寫歷史；如果只存姓名，程式就無法可靠地判斷「這是不是我的單」。所以兩個都要。

UserId 是 nullable，因為導入登入機制之前建立的舊資料沒有帳號可以對應——這是「這筆資料早於登入功能」的誠實表達，不是可有可無。

**3. 補回歷史紀錄的人名時，不要覆蓋原本的人。**
`CashAdvanceSettlementRecord.Update()` 刻意**不接**處理人參數：處理人是當初登記這筆結清的人，之後別人來更正金額或備註都不會換掉它，理由同上——歷史不該被改寫。代價是「這次是誰改的」目前只有 `UpdatedAt` 時間、沒有名字；真的需要時再加 `UpdatedByUserId`。

**4. 「操作者」自動帶入登入者，「當事人」不會。**
預支款的**領款人**是「錢給了誰」，不是「誰在操作系統」——主管很可能替員工建立預支款，所以它是表單上的一個使用者下拉，不是自動帶入。分辨這兩者是這一段的重點。

但「不自動帶入登入者」不等於「只能存字串」：領款人一樣存 `PayeeUserId` + `PayeeName` 姓名快照，只是 UserId 來自主管挑的人而不是登入者。沒有 UserId 就無法判斷「這筆預支款是不是我的」，連下拉過濾與「員工只看自己的預支款」都做不到。

## 「系統裡有哪些人」怎麼查：`IUserDirectory`

要畫「領款人」下拉，Application 得知道有哪些使用者——但它不認識 `UserManager<ApplicationUser>`（那是 Infrastructure 的東西）。所以在 `Application/Identity` 定義介面、在 `Infrastructure/Identity` 實作：

```csharp
public interface IUserDirectory
{
    Task<IReadOnlyList<UserOptionDto>> ListSelectableAsync(CancellationToken ct = default);
    Task<UserOptionDto?> FindByIdAsync(Guid userId, CancellationToken ct = default);
}
```

這跟 repository 是同一個依賴反轉（dependency inversion）的形狀：**介面由使用它的那一層定義，實作往外推。** 依賴方向仍然是 `Web → Application → Domain`，Infrastructure 只是在 DI 容器裡把實作插進來。

`FindByIdAsync` 不只是為了查一個人，它撐住了一條安全規則：**建立預支款的命令只帶 `PayeeUserId`，不帶姓名。** 姓名快照由 Application 自己查出來，不接受 Web 層送進來的字串——否則有人竄改表單就能讓紀錄上的名字跟帳號對不上。

兩個方法的狀態過濾也刻意不同：`ListSelectableAsync` 只列 `Active` 的帳號（不該把錢指派給尚待啟用或離職的人），`FindByIdAsync` 不過濾（帳號停用之後，歷史紀錄上的人還是得查得到）。

要**改**帳號（啟用、停用、換角色、換密碼）走的是另一個介面 `IUserAccountStore`，刻意不跟這個唯讀的目錄混在一起，理由見〈[帳號生命週期與使用者管理](user-accounts.md)〉。

## 資料表命名

Identity 預設會建 `AspNetUsers`、`AspNetRoles` 這種 PascalCase 表名，本專案其他資料表都是 snake_case。`Infrastructure/Persistence/IdentityModelConfiguration.cs` 把它們改名成 `users`、`roles`、`user_roles` 等，欄位也轉成 snake_case，避免同一個資料庫出現兩套命名慣例（PostgreSQL 遇到 PascalCase 還得加引號才查得到）。

## 顯示名稱為什麼放在 claim

`ApplicationUser.DisplayName` 在登入時被寫進 cookie 的 claim（見 `ApplicationUserClaimsPrincipalFactory`）。每一頁要顯示「誰登入了」就直接從 claim 讀，不用再查一次 `users` 表。

**Tradeoff**：cookie 是登入當下的快照，所以使用者改名後要重新登入才會看到新名字。以本專案的規模（10 人內、名字幾乎不會變）這個代價可以接受；如果哪天改名很頻繁，就要改成每次查 DB 或縮短 cookie 有效期。

## 全站預設要登入

`Program.cs` 設了 fallback policy：

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

意思是「沒有標任何授權屬性的端點，一律要求登入」，例外要自己標 `[AllowAnonymous]`（登入頁、錯誤頁）。

為什麼用這個而不是每個 Controller 加 `[Authorize]`：**預設關起來，漏掉的後果是「多擋一個頁面」；預設開著，漏掉的後果是「機敏資料裸奔」**。以後新增 Controller 忘記加註記時，前者只是要多加一行，後者是資安漏洞。

## 初始密碼不進版控

第一個主管帳號的密碼從設定值 `Identity:SeedPassword` 讀（實務上放在 user secrets），沒設就跳過建立帳號並留下 log。帳號名稱與 email 寫在 `IdentityBootstrapper` 裡是可以的——那不是機密；密碼不行。

同樣的原則也適用於連線字串：repo 裡不放任何真實密碼。
