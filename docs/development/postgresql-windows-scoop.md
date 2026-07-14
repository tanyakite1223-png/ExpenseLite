# PostgreSQL Windows（scoop）開發環境筆記

> 這份是「筆電」這台開發機的設定。桌機那台是 repo 內 portable binaries，見
> [postgresql-windows-portable.md](postgresql-windows-portable.md)。
> 兩台的資料庫名稱、帳號、port 都一樣，所以 EF Core migration 與連線字串可以共用。

## 目前結果

- PostgreSQL 版本:18.4
- 來源:`scoop install postgresql`
- binaries 位置:`%USERPROFILE%\scoop\apps\postgresql\current\bin`
- data directory:`%USERPROFILE%\scoop\apps\postgresql\current\data`
- host:`127.0.0.1`
- port:`5432`
- database:`expenselite_dev`
- application user:`expenselite_app`
- 認證方式:`scram-sha-256`（與桌機一致）

明文密碼不要寫進 repo。需要連線時,在本機 shell 設定 `PGPASSWORD`,ASP.NET Core 則用 user secrets。

## 從零準備流程

### 1. 安裝

```powershell
scoop install postgresql
```

### 2. 初始化 data directory

```powershell
initdb -D "$env:USERPROFILE\scoop\apps\postgresql\current\data" -U postgres
```

`initdb` 完成後,`data\PG_VERSION` 應該存在。

> scoop 的 `initdb` 預設會產生 `trust` 的 `pg_hba.conf`(本機連線免密碼)。
> 下面第 4 步會改成 `scram-sha-256`,讓行為跟桌機一致。

### 3. 啟動

```powershell
pg_ctl start -D "$env:USERPROFILE\scoop\apps\postgresql\current\data"
```

或用 repo 腳本(會自動判斷這台是 scoop 還是 portable):

```powershell
.\scripts\dev\Start-Postgres.ps1
```

### 4. 建立 application user、database,並改為密碼認證

先用 `psql` 以 `postgres` 連進去(此時還是 trust,不用密碼):

```powershell
psql -h 127.0.0.1 -p 5432 -U postgres -d postgres
```

在 `psql` 內執行(`<本機開發密碼>` 換成自己的):

```sql
CREATE ROLE expenselite_app LOGIN PASSWORD '<本機開發密碼>';
ALTER ROLE postgres WITH PASSWORD '<本機開發密碼>';
CREATE DATABASE expenselite_dev OWNER expenselite_app;

\connect expenselite_dev

ALTER SCHEMA public OWNER TO expenselite_app;
GRANT ALL ON SCHEMA public TO expenselite_app;
\q
```

接著把 `data\pg_hba.conf` 裡所有 `trust` 改成 `scram-sha-256`,然後 reload:

```powershell
pg_ctl reload -D "$env:USERPROFILE\scoop\apps\postgresql\current\data"
```

### 5. 驗證

```powershell
$env:PGPASSWORD = '<本機開發密碼>'
.\scripts\dev\Connect-Postgres.ps1 -Command 'select current_database(), current_user; create table if not exists __permission_check(id integer); drop table __permission_check;'
```

看到 `expenselite_dev` / `expenselite_app`,而且 `CREATE TABLE` / `DROP TABLE` 成功,代表 EF Core migration 之後有建表權限。
另外把 `$env:PGPASSWORD` 清掉後再連一次,應該要被拒絕(`no password supplied`)——代表密碼認證真的生效了。

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

- scoop 的 PostgreSQL 不註冊成 Windows service,每次開機要自己啟動。
- data directory 在 scoop 的 app 目錄下,`scoop update postgresql` 到新的 major 版本時要注意資料遷移。
- 這不是正式部署方式。正式 Mac 內部主機需要另外處理 macOS PostgreSQL 安裝、ASP.NET Core runtime、HTTPS 憑證、自動啟動與備份。
