# 帳號生命週期與使用者管理

這篇接在〈[登入與角色在 ExpenseLite 怎麼用](identity-and-authentication.md)〉後面。那篇談的是「登入的人是誰、怎麼流進 Domain」；這篇談的是**帳號本身**：怎麼產生、誰能改它、這些邏輯落在哪一層。

## 帳號從哪裡來：只有主管建得出來

```
主管在使用者管理頁建立 ──> Active ──主管停用──> Disabled
                             ^                     |
                             └─────主管啟用────────┘
```

系統裡產生帳號的地方只有兩個：**空系統時的 bootstrap**（見〈Seed 與 Bootstrap 的差別〉），以及**主管在使用者管理頁按「新增使用者」**。沒有第三條路。

### 推翻紀錄：原本有自助註冊（2026-08-08 拿掉）

2026-07-27 定案的流程是「員工自己註冊 → 停在 `Pending` → 主管啟用」，登入頁上有一個公開的註冊連結。拿掉的理由是一個從測試裡冒出來的實際問題：

**已經註冊過、還在等啟用的人忘記了，又註冊一次，畫面只會說「帳號「amber」已經有人用了」。** 它不會（也不該）告訴他「這是你自己上次送出的申請」——註冊頁對未登入的人透露帳號狀態，等於送出公司的員工名單。但代價是使用者的合理解讀變成「這個名字被別人佔走了」，於是換個帳號名再註冊一次，系統裡多出第二筆待啟用的申請，主管還不知道該啟用哪一筆。

公開服務對這題的標準解法（OWASP：不論重複與否都回同一句「已寄出確認信」，真相只放進信箱）在這裡走不通，**因為那個解法的前提是有 email 通道**——「不在畫面上講」的代價是必須「在別的地方講」，而本專案沒有 SMTP，§4.8 也把雲端服務鎖住了。照抄只會讓使用者卡在一個什麼都沒發生的畫面上。

改成主管建帳號之後，這個難題不是被解決，是**被取消**了：「帳號已經有人用了」這句話的觀眾從「任何連得到主機的陌生人」變成「主管」，而主管在使用者列表上本來就看得到全部帳號。對他講這句話沒有洩漏任何他不該知道的事，訊息可以留著原樣。

順帶的收穫是 `[AllowAnonymous]` 的入口從三個收成兩個——未登入的人只碰得到登入頁與 AccessDenied。**要新增對外開放的端點時，這個數字值得再數一次。**

代價也要記著：帳號的產生完全依賴 bootstrap 與主管，所以新環境沒設好 `Identity:SeedPassword` 就是真的沒有人進得去，沒有備援路徑。

## 帳號的兩個狀態

原本 `ApplicationUser` 上是一個 `IsActive` bool，2026-08-07 換成 `UserAccountStatus` 三態 `{ Pending, Active, Disabled }`，理由是 **bool 分不出「還沒被啟用過」和「用過但被停用」**，登入被擋下來時會講錯話。

自助註冊拿掉之後，`Pending` 也跟著拿掉了（2026-08-08），剩 `{ Active, Disabled }`。它唯一的來源就是員工自己註冊出來的帳號，沒有註冊就沒有東西會產生那個狀態，留著只會是個沒有人走的死狀態。當時確認過「先把帳號建好、過幾天才讓他登入」這個情境不會發生——實際流程是主管建好帳號、當場把帳號與預設密碼交給本人，本人再自己改密碼。

`status` 欄位存的是字串（`IdentityModelConfiguration` 裡的 `HasConversion<string>()`），所以**少一個列舉值不需要 migration**，schema 完全沒動。但要記著另一面：DB 裡若還留著 `'Pending'` 的資料列，程式讀出來會對不到列舉值而炸掉，連使用者列表都打不開。拿掉一個列舉值時，**schema 沒事不代表資料沒事**。

enum 放在 `Application/Identity`，不是 `Infrastructure`。理由跟 `ExpenseLiteRoles` 一樣：「這個帳號能不能用」是業務概念，Web 的畫面與 Infrastructure 的 Identity 實作都要用到它。

## 登入為什麼要「先驗密碼、再判狀態」

順序是有意義的，不是隨手寫的：

```csharp
// 1. 帳號不存在 → 「帳號或密碼不正確」
// 2. 密碼錯 → 「帳號或密碼不正確」（同一句話）
// 3. 密碼對、但狀態不是 Active → 「此帳號已停用」
// 4. 密碼對、狀態 Active → 發 cookie
```

如果把狀態判斷提到密碼前面，任何人都能在登入頁輸入一個帳號加隨便的密碼，靠回應的差異（「此帳號已停用」vs「帳號或密碼不正確」）判斷這個帳號**是否存在**。那是帳號列舉（account enumeration），等於免費送出公司的員工名單。

**註冊頁拿掉之後這一節仍然成立**，因為登入頁本身就是對外公開的。剩下一種狀態訊息不代表沒有差異可比對——有差異就推得出來。

把它擺在密碼之後，就只有本人（或已經知道密碼的人）看得到狀態訊息，這時候講清楚才是幫忙。

實作上用 `SignInManager.CheckPasswordSignInAsync` 而不是 `PasswordSignInAsync`：前者**只驗密碼、不發 cookie**，但一樣會累計失敗次數，所以登入失敗鎖定沒有因此失效。狀態通過之後才自己呼叫 `SignInAsync` 發 cookie。

## 為什麼是 `IUserAccountStore`，而不是把方法加進 `IUserDirectory`

兩個介面都在 `Application/Identity` 定義、`Infrastructure/Identity` 實作，形狀完全一樣（依賴反轉）。分成兩個是因為**能力範圍差太多**：

| | `IUserDirectory` | `IUserAccountStore` |
| --- | --- | --- |
| 用途 | 查「系統裡有哪些人」 | 建立與維護帳號 |
| 能做的事 | 唯讀，只回姓名 | 改狀態、改角色、改密碼 |
| 誰在用 | 報銷單、預支款畫下拉選單 | 只有使用者管理 |

如果全部塞進 `IUserDirectory`，那「只是想畫一個領款人下拉」的 `CashAdvanceAppService` 也會一併拿到重設別人密碼的能力。介面切得剛好，就是在限制「拿到這個東西的人最多能做什麼」。

## 「至少要有一位啟用中的主管」為什麼落在 Application Service

照 CLAUDE.md §4.2 的判斷順序，這是一條**純業務規則、而且跨多個對象**（要看整份名單才知道自己是不是最後一個），照理說該是 Domain Service。但它最後放在 `UserAccountAppService`，原因是：

**本專案的使用者刻意不是 Domain 的一員。** `ApplicationUser` 繼承 Identity 的 `IdentityUser<Guid>`、住在 Infrastructure；Domain 只用 `Guid` 參照人（見〈[登入與角色](identity-and-authentication.md)〉）。要把這條規則放進 Domain，就得先把「使用者」整個搬進 Domain 成為一個 aggregate，那個代價遠大於一條規則帶來的好處。

所以這是一個**誠實的例外，不是偷懶**：規則落在「需要 `IUserAccountStore` 才判斷得出來」的那一層。判斷方式很直白——

```csharp
var activeManagers = accounts
    .Where(x => x.Status == Active && x.Role == Manager)
    .ToList();

if (activeManagers.Count == 1 && activeManagers[0].UserId == userId)
{
    throw new DomainRuleViolationException(...);
}
```

沒有這道防線的後果不是「資料錯了」，是**全公司登得進來但沒有人能核准報銷單、也沒有人能啟用別人的帳號，而且沒有後門可以救**。

兩個入口都要擋：停用最後一位主管、以及把最後一位主管降成員工。少擋一個就等於沒擋。

## 緊急存取帳號（break-glass account）

「至少一位啟用中的主管」擋得住「主管被拿掉」，但擋不住另一種鎖死：**唯一的主管忘記密碼**。重設密碼需要主管權限，他自己又進不來，於是整間公司卡在門外，系統裡沒有任何救援路徑。

業界對這個問題的標準答案叫 **break-glass account**（緊急存取帳號，字面意思是消防箱那種「打破玻璃」）。Microsoft Entra ID 官方建議每個租戶保留兩個這種帳號並排除在條件式存取政策之外，AWS 的 root account 也是同一個概念：開完戶就鎖起來，日常一律用別的身分。

本專案的作法是讓 bootstrap 建立的 `Admin` 擔任這個角色，規則有兩條：

- **不能停用**（`UserAccountAppService.DisableAsync`）
- **不能降成員工**（`UserAccountAppService.SetRoleAsync`）

判斷依據是帳號上的 `IsProtected` 旗標，**不是比對帳號名稱叫不叫 `Admin`**。名字是可以改的、也可能有人另外註冊一個同名帳號，靠字串認很脆；旗標是資料，跟著那一列走。

這個旗標**只有 bootstrap 建立第一個帳號時會設**，UI 上沒有任何地方能開關它。所以增減緊急帳號是刻意的手動動作：

```sql
-- 把某個既有的主管帳號也變成緊急存取帳號
UPDATE users SET is_protected = true WHERE user_name = 'someone';

-- 讓 Admin 退場（請先確認還有另一個 is_protected = true 的啟用中主管）
UPDATE users SET is_protected = false WHERE user_name = 'Admin';
```

新增或撤掉一個緊急帳號本來就不該是點兩下就完成的事，所以現階段刻意不做成 UI 功能。如果之後覺得需要，加一個動作不難。

**注意保護的是「進得來」，不是「不能被換掉」。** 緊急帳號的密碼隨時可以被任何主管用「重設密碼」換掉——那正是密碼外流時的處置方式。不能停用只是保證這條路不會被關上。

### 告警：怎麼知道它被用過

緊急帳號最該被回答的問題是「它被用過嗎」。這套系統沒有 SMTP、也沒有外部告警服務（§4.8 把雲端服務鎖住了），所以告警分成三路留在系統內：

1. **伺服器 log**：`AccountController` 在緊急帳號登入成功時寫一筆 `LogWarning`，內容含帳號與時間。
2. **畫面橫幅**：登入身分是緊急帳號時，`_Layout` 每一頁都掛一條紅色提醒。判斷依據是登入時寫進 cookie 的 `protected_account` claim——**這個 claim 只用來畫畫面，不做權限判斷**，因為 claim 是登入當下的快照，權限要看資料庫。
3. **最後登入時間**：`users.last_signed_in_at` 在每次登入成功時更新，使用者管理頁列出來。緊急帳號那一列如果出現沒人記得的時間，就是該換密碼的訊號。

第 3 點是三者裡最重要的——log 沒人看、橫幅只有當事人看得到，只有列表上的時間是**別人**事後查得到的。

**老實記著兩件事**：這只是「留下痕跡」，不是即時告警，沒有人主動被通知；而且系統沒有完整的登入稽核（誰在什麼時候登入過幾次、從哪裡），只有每個帳號最後一次的時間。真的需要完整稽核軌跡，那是另一個題目。

## 錯誤為什麼用 `UserAccountResult` 而不是例外

其他 Application Service 遇到違反規則就丟 `DomainRuleViolationException`。帳號這一塊多了一個 `UserAccountResult(bool Succeeded, IReadOnlyList<string> Errors)`，理由有兩個：

1. **一次可能有好幾條錯誤**。建立帳號時帳號重複、Email 重複、密碼太短可以同時發生，Identity 本來就回一整包。用例外表達只能丟第一條。
2. **不讓 `IdentityResult` 漏進 Application**。那是 Infrastructure 的型別，跟 `ApplicationUser` 一樣不該往上跑。

分界線是：**使用者自己能修正的（密碼太短、帳號重複、舊密碼打錯）走 `UserAccountResult`，Controller 把它放進 ModelState 顯示在表單上；違反業務規則的（最後一位主管、找不到帳號）仍然丟例外。**

順帶一提，Identity 內建的錯誤訊息是英文的。`ChineseIdentityErrorDescriber` 是 Identity 官方的擴充點，只換句子、不改任何驗證規則。

## 為什麼沒有「刪除使用者」

系統只能停用帳號，**沒有刪除功能，而且是刻意的**。

理由跟「跨 aggregate 用 ID 參照」是同一件事的反面：報銷單、預支款、審核紀錄、結清紀錄上都存著 `Guid` 形式的 UserId，而且**沒有外鍵**（只有 Identity 自己的表對 `users` 有 FK）。刪掉一個人，資料庫不會擋、也不會連帶刪除任何東西——那些 Guid 就只是變成指向一個不存在的人。

後果很具體：姓名快照還在，畫面看起來一切正常，但「這張單是不是我的」永遠判斷不出來了。那個人名下的草稿沒有人能修改或送審（`EnsureCanBeEditedBy` 比對申請人，主管沒有例外），他名下的預支款也再也不能被任何新報銷單引用。

停用才是對的模型：**不能登入，但歷史查得到**。`IUserDirectory` 兩個方法的過濾差異（`ListSelectableAsync` 只列 Active、`FindByIdAsync` 不過濾）就是為這件事設計的。

如果日後有人想加一個刪除按鈕，先回頭看這一段。

## Seed 與 Bootstrap 的差別

原本的 `IdentitySeeder` 每次開機都確保 `manager`、`employee` 兩個 demo 帳號存在。現在改成 `IdentityBootstrapper`：

- **角色**還是每次開機都確認（沒有角色，`[Authorize(Roles = ...)]` 會全部失效）。
- **帳號**只在 `users` 表**完全沒有人**時建一個主管 `Admin`，密碼取自 `Identity:SeedPassword`。只要表裡有人就整段跳過。

差別在於 seed 是「維持某個狀態」，bootstrap 是「讓一個空系統有辦法被登入第一次」。之後的帳號一律由主管建立。

當初選 bootstrap 而不是「第一個註冊的人自動變主管」，是因為 bootstrap 在第一次啟動時就完成，那時還沒有人連得進來，**不存在搶註冊的空窗期**；行為也可預測。自助註冊拿掉之後那個替代方案自然也不存在了，但這個理由仍然是這段程式碼為什麼長這樣的出處。

## 老實記著的兩個限制

**1. 停用 / 改角色不是立刻生效。**
改完之後會呼叫 `UpdateSecurityStampAsync`，讓對方手上那張登入 cookie 失效。但 Identity 是**每隔一段時間**才重新驗證 cookie（`SecurityStampValidationInterval` 預設 30 分鐘），所以最慢會延遲到那時候。以本專案的情境（停用通常是離職，不是緊急封鎖）可以接受；真的需要即時，就要縮短這個間隔，代價是每次驗證都多一次 DB 查詢。

**2. 使用者列表是 N+1 查詢。**
`IdentityUserAccountStore.ListAllAsync` 先撈所有使用者，再對每個人查一次角色。十人內的公司這點成本無所謂，換來的是最直白的答案（「這個人實際上掛了什麼角色」）。人數真的變多，就改成 join `user_roles` 一次撈完——跟報銷單列表的 in-memory 過濾是同一類「知道它在那裡、現在不划算改」的取捨。
