# CLAUDE.md — ExpenseLite(AI 協作練習・第一階段:Vibe Coding)

> 版本:v1.0(定版)
> 適用範圍:本 repo。與「ExpenseSystem 學習專案」、「ASP.NET Core 學習筆記」兩個 repo 互不相干,請勿跨 repo 操作。
> （`ExpenseLite` 為暫定專案名,Amber 可自行更換。）

---

## 1. 這個專案是什麼

- 這是 Amber 的「AI 協作開發練習・第一階段」,練習方式是 **vibe coding**:由 Amber 主導方向與決策,由你（AI）負責把功能實作出來、跑得起來。
- 假想目標:一間以人工作業（頂多 Excel 輔助）處理報銷的小型公司,做一套「員工墊款報銷」系統,把混亂的人工流程整理成可用的軟體。
- 第二階段會用 SDD（Spec-Driven Development,規格驅動開發）把同一套系統重做一次。所以本階段重點是「體驗 AI 協作、快速做出可運作的東西」,**不是追求架構完美,不要過度設計**。
- 系統「要做什麼功能、長什麼樣子」是 **留給 Amber 規劃** 的空間,本檔不預先規定系統規格,只給領域(報銷)與架構規範。
- 但本專案有一個刻意的設計:**透過 AI 協作,讓 Amber 接觸到她現階段自己不容易學到的「分層與領域模型架構」**。心智模型是——公司事先把架構規範寫在這份 CLAUDE.md 裡,你（AI）照規範實作並解講,Amber 負責 review。她一邊 vibe coding 規劃系統,一邊看到並讀懂優良架構。

---

## 2. 協作模式（重要)

- 這裡 **不是** Socratic 學習模式。不要反問、不要刻意保留答案、不要出題考她。Amber 給方向,你就把功能做出來。
- 但 **每次動完 code,用 1～3 句白話** 告訴她:你改了什麼、為什麼這樣做。目的是讓她保有「看得懂、能 review」的程度（她正在找工作,要能在面試講得出自己的系統),不是要你教學。
- 方向不明確時,先用一句話確認你打算怎麼做、得到她「好」之後再動手,不要默默做一大包再回報。

---

## 3. 技術堆疊（固定,不要擅自更換)

- 語言:C#
- 框架:ASP.NET Core（.NET 10),以 MVC 為主;需要時可加 Web API
- 前端:Server-side Razor + 必要的原生 HTML / CSS / JavaScript。**不要** 自行引入 React / Vue / Angular 等 SPA 框架
- ORM:EF Core
- 資料庫:開發階段用 SQL Server LocalDB（`(localdb)\MSSQLLocalDB`);未來若真的要提供給公司,再改用 SQL Server Express（屆時另行處理,現在不用管)
- 金額一律用 `Money` Value Object(內部對應 `decimal(18,2)`),不要用 `float` / `double`
- **不要** 引入雲端服務、Docker、訊息佇列等本階段用不到的東西(另見 §4.8 範圍清單)
- 要新增 NuGet 套件前,先告知 Amber 並說明用途,得到同意再裝

---

## 4. 架構規範(本專案的「公司規範」)

> 範圍刻意框在「Amber 現階段接得住、又有就業市場價值」這條線上。

### 4.1 分層

- 分層:**Controller(薄)→ Application Service → Domain → Infrastructure(EF Core)**
- 結構:**單一專案內分資料夾**(`Domain/`、`Application/`、`Infrastructure/`、`Web`),先不要拆成多個 `.csproj`
- 依賴方向:`Web → Application → Domain`;**`Domain` 不依賴任何人**(EF Core 等基礎設施屬於 `Infrastructure`)
- Controller 只負責:接 HTTP、model binding、呼叫 service、回傳結果。**不要** 在 Controller 直接碰 `DbContext`

### 4.2 邏輯該放哪(務必遵守,否則容易混亂)

判斷順序(由內而外):

1. 屬於某個 entity 自己的規則 / invariant / 狀態轉換 → 放 **那個 entity**(rich domain,**預設優先**)
2. 純業務規則、但不屬於任何單一 entity(通常跨 entity)→ 放 **Domain Service**
3. 要碰基礎設施(repository / DB / 外部服務)、交易、use case 編排 → 放 **Application Service**(呼叫 entity / domain service 的方法,不重寫業務規則)

- Domain Service **開放使用**,但「不屬於單一 entity」這個門檻要把得住——能放進 entity 的就別外移,否則會養出貧血模型。
- 第一次引入 Domain Service 時,向 Amber 說明「為什麼是 domain service 而不是 application service」,並補一篇 `/docs/architecture/`。

### 4.3 Rich Domain Model

- 報銷單的狀態轉換(Draft → Submitted → Returned → Approved → Rejected)做成 **方法**,例如 `report.Submit()`、`report.Approve()`,並在方法內擋掉非法轉換
- 不要做成「貧血模型」(只有 getter/setter、邏輯全寫在外面 service)

### 4.4 Aggregate(最小核心版)

- `報銷單(ExpenseReport)` = **Aggregate Root**;`報銷單明細(ExpenseDetail)` = aggregate 內部 entity,**沒有獨立生命週期、不單獨存取**
- 所有明細操作一律走 root:`report.AddDetail(...)`、`report.RemoveDetail(...)`,**不要** `new ExpenseDetail()` 後各自存檔
- root 負責守 invariant:總額 = 明細加總、送審後明細鎖定、(其他規則由 Amber 定)
- **跨 aggregate 一律用 ID 參照**:`Project`、`User/員工` 是各自獨立的東西,明細只持有 `ProjectId`,**不要** 把整顆 Project / User 抓進來改
- 注意:EF Core 不會「強制」aggregate 邊界,這是 **靠約定維持的紀律**——對外只把 `DbSet<ExpenseReport>` 當入口、明細只透過 navigation 走、repository 只認 root。要在教學文件裡向 Amber 講清楚這點(是紀律,不是框架魔法)

### 4.5 Value Object

- `Money` 當旗艦範例:驗證(不可負、兩位小數)、值相等(value equality)
- EF Core 用 **owned type** 對應到報銷單 / 明細的金額欄位

### 4.6 Repository

- 規則:**一個 aggregate root 一個 repository**
- 所以有 `IExpenseReportRepository`(載入報銷單時用 `Include` 把明細一起帶出來);**明細不會有自己的 repository**(從來不會單獨載入 / 儲存一筆明細)
- 在教學文件裡 **老實註明 tradeoff**:EF Core 的 `DbContext` 本身已經是 Unit of Work + Repository,這裡再包一層是為了「對持久化的抽象 + 配 DI」這個就業常見題型,讓 Amber 學到的是「有取捨的 pattern」而不是教條

### 4.7 DTO / ViewModel 與 Entity 分離

- **不要** 把 EF entity 直接丟給 View 或 API(避免 over-posting、耦合、機敏欄位外洩)

### 4.8 範圍清單(滾動式,可由 Amber 解鎖)

**目前鎖住(踩到要先解鎖,不要自己用):**

- Domain Event、跨 aggregate 最終一致性、Saga
- CQRS、MediatR
- 多 `.csproj` 的 Clean Architecture / Onion 分專案
- 雲端服務、Docker、訊息佇列
- aggregate 大小 / 效能調校那類進階考量

**解鎖機制:**

- 當你(AI)判斷現況真的需要、或採用後會明顯更好的清單內項目時,**不要自己加進去**。先停下來:
  1. 說明這是什麼、為什麼現況需要它、tradeoff 是什麼
  2. 視 Amber 目前的理解狀況,判斷她是否需要先補一點前置概念(需要的話,先問她要先補概念、還是先繞過)
  3. 請 Amber 決定要不要解鎖
- **解鎖與否由 Amber 決定。** 她同意後,你把該項從「目前鎖住」搬到下方「已解鎖紀錄」,記下日期與理由,並補一篇 `/docs/architecture/`。
- 這是雙方同意的「範圍 / 學習深度」滾動調整,不是你單方修改規範。routine 的解鎖搬移 **不用動版本號**;只有規則本身或結構性調整才遞增版本。

**已解鎖紀錄:**

- (目前無)

### 4.9 架構教學目錄 `/docs/architecture/`

- 每當你 **引入或用到一個架構 pattern**(含解鎖的項目),就在 `/docs/architecture/` 新增 / 更新一篇 **短** 文
- 重點寫「**本專案怎麼用、為什麼這樣用**」,而不是通用理論
- 範例篇名:〈為什麼 Controller 不直接碰 DbContext〉〈Money 這個 VO 在做什麼〉〈Service 跟 Entity 的分工(含 Domain Service)〉〈報銷單為什麼是 Aggregate Root、明細為什麼沒有自己的 repository〉
- 目的:讓這些文件變成 Amber 貼著程式碼、可以直接拿去面試講的架構教材

---

## 5. 版控規範（git)

- 平常在 `main` 上工作即可;Amber 若想練分支,再開 feature branch
- **git 指令一律由 Amber 自己下**(`git add` / `commit` / `push` 等),這是她要練的手感,不要代勞
- 你的角色:**在一個實作段落告一段落時,建議她可以 commit 了,並依照下列格式提供一段現成的 commit message** 給她貼用
- Commit message 格式:`<type>: <繁體中文描述>`,type 用 `feat` / `fix` / `refactor` / `docs` / `chore` / `test` / `style`
  - 例:`feat: 新增報銷單建立功能`、`fix: 修正金額加總錯誤`
- 一次 commit 對應一件事,不要把不相關的改動混在同一個 commit

---

## 6. Session 交接(handoff)

> 每個 Claude Code session 互相獨立、沒有記憶,本專案靠 `.claude/current-handoff.md` 接力。
> 注意:vibe coding 會跨多個 session,**「在對的時機找到漂亮斷點、收乾淨、提出交接」本身是 Amber 要練的技能**,不是你默默處理的後台工作。

- **Session 開始**:先讀 `.claude/current-handoff.md` 掌握現況(已完成什麼、架構狀態、下一步)。若檔案不存在,視為全新專案,照 §1 開始。
- **開發過程中**:當一段相關的開發自然告一段落、且處於乾淨狀態(能 build、沒有做一半的破碎改動)時,**主動點出「這裡是個乾淨斷點,若要收工 / 交接,現在是好時機」**。但 **要不要在此斷,由 Amber 決定**——不要自己宣布 session 結束。
- **Amber 決定交接時**:幫她 **草擬** handoff 內容,交給她 review、視情況調整(讓她從中學會一份好 handoff 長怎樣);把被覆蓋的舊內容歸檔到 `.claude/handoff-archive/handoff-YYYYMMDD-HHmm.md`。
- handoff 更新好後,提醒 Amber 一起 commit(指令她自己下,見 §5)。
- 內容聚焦 **系統開發現況**(專案現況 + 架構狀態,含「已落地哪些 pattern / 有無偏離規範」),**不是** Amber 的學習進度。

---

## 7. 安全與不可逆操作

- 以下動作 **一定要先停下來、明確提醒 Amber 風險** 再讓她決定:刪除檔案、drop / reset 資料庫、`git reset --hard`、force push、改寫 git 歷史、刪除分支
- 發現安全問題(寫死的密碼或連線字串、SQL injection 風險、機敏資料外洩等)要 **當下立刻提醒**,不要等
- 不確定某個操作會不會造成不可逆後果時,先說明再讓她決定

---

## 8. 語言

- 對話用繁體中文
- 技術術語保留英文並視需要附中文,例如:migration（資料庫遷移)、Controller（控制器)、Aggregate Root(聚合根)
