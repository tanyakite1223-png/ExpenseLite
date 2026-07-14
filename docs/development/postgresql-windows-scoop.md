# PostgreSQL Windows（scoop）開發環境筆記

> 這份是「用 scoop 安裝 PostgreSQL」的 Windows 開發機設定,桌機與筆電目前都採這個方式。
> repo 內的 portable binaries（`.devtools/`）是更早的做法,見
> [postgresql-windows-portable.md](postgresql-windows-portable.md),現在當作備援 / 參考。
> 兩種方式的資料庫名稱、帳號、port 都一樣,EF Core migration 與連線字串可以共用。

## 目前結果

- PostgreSQL 版本:18.4
- 來源:`scoop install postgresql`
- binaries 位置:`%USERPROFILE%\scoop\apps\postgresql\current\bin`
- data directory:`%USERPROFILE%\scoop\apps\postgresql\current\data`
  - 這是 junction,實際落在 `%USERPROFILE%\scoop\persist\postgresql\data`(在**專案外**,`scoop update` 換版本也不會弄丟)
- host:`127.0.0.1`
- port:`5432`
- database:`expenselite_dev`(encoding `UTF8`、locale `C`)
- application user:`expenselite_app`
- 認證方式:`scram-sha-256`

明文密碼不要寫進 repo。需要連線時,在本機 shell 設定 `PGPASSWORD`,ASP.NET Core 則用 user secrets。

## 從零準備流程

### 1. 安裝（會自動初始化 data directory）

```powershell
scoop install postgresql
```

scoop 的 postgresql 安裝腳本**會自動 `initdb`**:資料目錄初始化在 `...\current\data`(junction 到 `...\persist\postgresql\data`)、預設 `trust` 認證、locale `C`、superuser `postgres` 密碼空白。所以**不需要自己再跑 `initdb`**(對已初始化的目錄再跑會失敗)。裝完 `data\PG_VERSION` 應該已存在。

### 2. 啟動

```powershell
.\scripts\dev\Start-Postgres.ps1
```

腳本用 `Resolve-PgEnv.ps1` 自動判斷這台是 scoop 還是 portable。（也可以手動 `pg_ctl start -D "$env:USERPROFILE\scoop\apps\postgresql\current\data"`。)

### 3. 建立 application user 與 database

此時還是 `trust`,用 `postgres` 連進去不用密碼:

```powershell
psql -h 127.0.0.1 -p 5432 -U postgres -d postgres
```

在 `psql` 內執行(先不設密碼,下一步再設,避免明文密碼留在指令歷史):

```sql
CREATE ROLE expenselite_app LOGIN;

CREATE DATABASE expenselite_dev
    OWNER expenselite_app
    TEMPLATE template0
    ENCODING 'UTF8'
    LC_COLLATE 'C'
    LC_CTYPE 'C';

\connect expenselite_dev
ALTER SCHEMA public OWNER TO expenselite_app;
GRANT ALL ON SCHEMA public TO expenselite_app;
```

> 用 `template0` 才能指定跟 cluster 預設不同的 encoding / locale;`UTF8` 讓中文安全,`C` locale 讓排序是穩定的 byte order。

### 4. 設定密碼（隱藏輸入,不留明文）

還在同一個 `psql` 裡,用 `\password`(會提示兩次、隱藏輸入,並自動用 scram 雜湊):

```
\password expenselite_app
\password postgres
\q
```

`expenselite_app` 請填跟 ASP.NET Core user secrets 連線字串**同一組**密碼,app 才不用改設定。

### 5. 改成密碼認證（scram）

把 `data\pg_hba.conf` 裡所有 `trust` 改成 `scram-sha-256`,然後 reload:

```powershell
pg_ctl reload -D "$env:USERPROFILE\scoop\apps\postgresql\current\data"
```

### 6. 驗證

```powershell
$env:PGPASSWORD = '<本機開發密碼>'
.\scripts\dev\Connect-Postgres.ps1 -Command 'select current_database(), current_user; create table if not exists __permission_check(id integer); drop table __permission_check;'
```

看到 `expenselite_dev` / `expenselite_app`、`CREATE TABLE` / `DROP TABLE` 成功,代表有建表權限。
把 `$env:PGPASSWORD` 清掉後再連一次應該被拒絕(`no password supplied`),代表密碼認證真的生效。
最完整的驗證是直接跑 `dotnet ef database update`——能套用 migration 就代表整條連線鏈(密碼、權限、UTF8)都通了。

## 日常使用

```powershell
.\scripts\dev\Start-Postgres.ps1     # 啟動
.\scripts\dev\Connect-Postgres.ps1   # 連線（會問密碼）
.\scripts\dev\Stop-Postgres.ps1      # 停止
```

## 連線字串

```text
Host=127.0.0.1;Port=5432;Database=expenselite_dev;Username=expenselite_app;Password=<本機開發密碼>
```

真密碼放 user secrets 或本機環境變數,不要寫死在 `appsettings.json`。

## 注意事項

- scoop 的 PostgreSQL 不註冊成 Windows service,每次開機要自己啟動(用 `Start-Postgres.ps1`)。
- data directory 是 junction 到 `...\persist\postgresql\data`,`scoop update postgresql` 換版本時資料會保留;但跨 major 版本升級仍建議先備份。
- 這不是正式部署方式。正式 Mac 內部主機需要另外處理 macOS PostgreSQL 安裝、ASP.NET Core runtime、HTTPS 憑證、自動啟動與備份。
