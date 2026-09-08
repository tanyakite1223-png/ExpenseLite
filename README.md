# 報銷系統(ExpenseLite)

一套為小型公司(約 10 人規模)設計的員工墊款報銷系統,依據公司目前實際的報銷作業流程(原以人工/Excel 處理)進行需求分析與系統設計,取代原有人工流程。

> 本專案為求職作品集,實際交付使用的內部資料(公司名稱、真實金額、員工資訊等)已移除或以假資料替代。

## 專案背景

原有報銷流程仰賴人工彙整與 Excel 記錄,容易發生金額計算錯誤、審核進度不透明、單據遺失等問題。本系統目標是將報銷單建立、明細管理、審核流程(草稿 → 送審 → 退回 → 核准 → 駁回)整合為線上化系統,並保留清楚的審核軌跡。

## 技術棧

| 類別 | 技術 |
|---|---|
| 語言 / 框架 | C# / ASP.NET Core (.NET 10) / MVC |
| 前端 | Server-side Razor |
| ORM | Entity Framework Core |
| 資料庫 | PostgreSQL |
| 部署情境 | 內部主機部署,員工僅透過瀏覽器連線使用 |

## 架構設計

採用 ASP.NET Core MVC 搭配 DDD(領域驅動設計)戰術模式,以 Clean Architecture 精神分層:

```
Web (Controller)
   ↓
Application Service
   ↓
Domain
   ↑
Infrastructure (EF Core)
```

- **依賴方向單向**:`Web → Application → Domain`,`Domain` 層不依賴任何外部框架或資料庫技術
- **Controller 保持精簡**:只負責接收 HTTP 請求、model binding、呼叫 Service、回傳結果,不直接操作 `DbContext`

## 核心設計重點

### Rich Domain Model
報銷單的狀態轉換(草稿 → 送審 → 退回 → 核准 → 駁回)封裝為 Entity 方法,例如 `report.Submit()`、`report.Approve()`,並在方法內部擋掉不合法的狀態轉換,避免邏輯散落在外部 Service。

### Aggregate 設計
`報銷單(ExpenseReport)` 為 Aggregate Root,`報銷單明細(ExpenseDetail)` 為內部 Entity,不具獨立生命週期。所有明細操作統一透過 Root 進行(如 `report.AddDetail(...)`),由 Root 負責維護總額一致性等不變條件(invariant)。跨 Aggregate(如 Project、User)一律以 ID 參照,不直接持有整個物件。

### Value Object — Money
金額一律使用 `Money` 值物件處理,內部以 `decimal` 運算,對應資料庫 `numeric(18,2)`,並實作值相等(value equality)與基本驗證,避免財務數字使用 `float` / `double` 造成精度誤差。

### Repository Pattern
遵循「一個 Aggregate Root 對應一個 Repository」原則,例如 `IExpenseReportRepository` 載入報銷單時以 `Include` 一併帶出明細;明細不具備獨立的 Repository。

### DTO / ViewModel 分離
View 與 API 一律使用獨立的 DTO / ViewModel,不直接暴露 EF Entity,降低耦合並避免機敏欄位外洩。

## 資料夾結構

```
├─ Domain/           # 領域模型:Entity、Value Object、Domain Service
├─ Application/       # 應用服務:use case 編排、交易處理
├─ Infrastructure/    # EF Core、Repository 實作
├─ Web/               # Controller、View、ViewModel
└─ docs/architecture/ # 架構決策說明文件
```

## 開發角色

獨立負責需求訪談、系統功能規劃、架構決策與商業邏輯設計;開發過程中運用 AI 工具加速實作,並透過 Code Review 確保掌握每一段程式碼的設計原因與取捨。
