# Money Value Object

金額目前一律用 `Money` 表示，不直接在 domain 裡傳 `decimal` 到處跑。

`Money` 會檢查金額不可為負數，而且最多只能有兩位小數。兩個 `Money` 是否相等，是看裡面的 `Amount` 值，不是看物件是不是同一個 instance。

在資料庫裡，EF Core 把 `Money.Amount` 對應成 PostgreSQL 的 `numeric(18,2)`，避免用 `float` / `double` 造成小數誤差。
