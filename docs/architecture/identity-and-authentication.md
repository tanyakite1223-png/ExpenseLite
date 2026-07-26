# 登入與角色在 ExpenseLite 怎麼用

本專案用 ASP.NET Core Identity 做登入與角色。這篇說明「東西放在哪一層、為什麼」，不是 Identity 的通用教學。

## 為什麼用 Identity 而不是自己做

密碼雜湊、登入 cookie、失敗鎖定這些東西做錯的代價很高，而且不做對也看不出來（能登入不代表安全）。Identity 是 .NET 內建、經過驗證的實作，所以這部分不自己寫。

代價是要接受它的資料表結構與一套慣例，並且多一個 NuGet 相依 `Microsoft.AspNetCore.Identity.EntityFrameworkCore`。

## 分層落點

這是本專案最需要注意的一點：**Identity 屬於 Infrastructure，Domain 不認識它。**

| 東西 | 放哪 | 為什麼 |
| --- | --- | --- |
| `ApplicationUser` | `Infrastructure/Identity` | 它繼承 `IdentityUser<Guid>`，直接依賴框架。放進 Domain 會讓 Domain 依賴 EF Core 與 ASP.NET Core，違反「Domain 不依賴任何人」。 |
| `ExpenseLiteRoles`（角色常數） | `Application/Identity` | 「員工 / 主管」是業務概念，不是 Identity 的實作細節。Web 的 `[Authorize]` 與 Infrastructure 的種子資料都要用到，放 Application 兩邊都能取用。 |
| `ApplicationUserClaimsPrincipalFactory` | `Infrastructure/Identity` | 它是 Identity 的擴充點，屬於框架整合。 |
| 登入頁、`AccountController` | `Web` | 純粹是 HTTP 與畫面。 |

因為 Domain 不認識 `ApplicationUser`，報銷單之類的 aggregate 要記「是誰做的」時，**只存 `Guid` 形式的 UserId**，不持有使用者物件。這跟 §4.4「跨 aggregate 一律用 ID 參照」是同一個規則：使用者是另一個獨立的東西，不該被抓進報銷單的邊界裡。

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

初始帳號的密碼從設定值 `Identity:SeedPassword` 讀（實務上放在 user secrets），沒設就跳過建立帳號並留下 log。帳號名稱與 email 寫在 `IdentitySeeder` 裡是可以的——那不是機密；密碼不行。

同樣的原則也適用於連線字串：repo 裡不放任何真實密碼。
