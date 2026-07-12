# PostgreSQL Windows 10 Portable 開發環境筆記

> 本筆記記錄 ExpenseLite **桌機** 這台開發機的 PostgreSQL 準備流程（repo 內 portable binaries）。
> 筆電那台是 `scoop install postgresql`,見 [postgresql-windows-scoop.md](postgresql-windows-scoop.md)。
> 兩台的資料庫名稱、帳號、port 一致,EF Core migration 與連線字串可共用。
> 正式公司環境會是一台 Mac 內部主機,不是這份 Windows portable 設定。

## 目前結果

- PostgreSQL 版本:18.4
- 來源:EDB PostgreSQL Windows x86-64 binaries
- binaries 位置:`.devtools/postgresql-18.4/`
- data directory:`.devdata/postgres/`
- host:`127.0.0.1`
- port:`5432`
- database:`expenselite_dev`
- application user:`expenselite_app`
- `.devtools/` 與 `.devdata/` 已放進 `.gitignore`,不要 commit 本機工具與資料庫資料。

明文密碼不要寫進 repo。需要連線時,在本機 shell 設定 `PGPASSWORD` 或之後改用 ASP.NET Core user secrets。

## 為什麼用 portable binaries

一開始嘗試用 Chocolatey 安裝 PostgreSQL:

```powershell
choco install postgresql18 --params "'/Password:<本機開發密碼> /Port:5432'" -y
```

但目前 shell 沒有 Windows 管理員權限,Chocolatey 無法寫入 `C:\ProgramData\chocolatey\lib`,所以沒有使用系統安裝版。

改用 EDB 官方提供的 zip binaries,放在 repo 的 `.devtools/` 裡,不註冊 Windows service,用目前使用者啟動本機開發 DB。

官方下載頁:

```text
https://www.enterprisedb.com/download-postgresql-binaries
```

本次使用的 Windows x64 zip:

```text
https://get.enterprisedb.com/postgresql/postgresql-18.4-1-windows-x64-binaries.zip
```

## 從零準備流程

以下指令都在 repo root 執行。

### 1. 下載 zip

```powershell
New-Item -ItemType Directory -Force -Path .devtools\downloads | Out-Null

curl.exe -L -C - `
  -o .devtools\downloads\postgresql-18.4-1-windows-x64-binaries.zip `
  https://get.enterprisedb.com/postgresql/postgresql-18.4-1-windows-x64-binaries.zip
```

`-C -` 是續傳;下載中斷時可以再跑同一個指令。

### 2. 解壓縮

```powershell
New-Item -ItemType Directory -Force -Path .devtools\postgresql-18.4 | Out-Null

tar -xf .devtools\downloads\postgresql-18.4-1-windows-x64-binaries.zip `
  -C .devtools\postgresql-18.4
```

解壓後應該會有:

```text
.devtools/postgresql-18.4/pgsql/bin/psql.exe
.devtools/postgresql-18.4/pgsql/bin/postgres.exe
.devtools/postgresql-18.4/pgsql/bin/initdb.exe
```

### 3. 初始化 data directory

```powershell
$Root = (Resolve-Path -LiteralPath '.').Path
$PgBin = Join-Path $Root '.devtools\postgresql-18.4\pgsql\bin'
$DataDir = Join-Path $Root '.devdata\postgres'
$PwFile = Join-Path $env:TEMP 'expenselite_pg_pw.txt'

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $DataDir) | Out-Null
Set-Content -LiteralPath $PwFile -Value '<本機開發密碼>' -NoNewline

& (Join-Path $PgBin 'initdb.exe') `
  -D $DataDir `
  -U postgres `
  -A scram-sha-256 `
  --pwfile $PwFile `
  --encoding UTF8

Remove-Item -LiteralPath $PwFile -Force
```

初始化完成後,`.devdata/postgres/PG_VERSION` 應該存在。

### 4. 啟動 PostgreSQL

本專案已提供腳本:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dev\Start-Postgres.ps1
```

這個腳本會直接啟動 `.devtools/postgresql-18.4/pgsql/bin/postgres.exe`。
在目前 Codex shell 環境裡,`pg_ctl start` 會遇到 Windows restricted token 問題,所以啟動腳本不用 `pg_ctl start`。

### 5. 建立 application user 與 database

```powershell
$Root = (Resolve-Path -LiteralPath '.').Path
$PgBin = Join-Path $Root '.devtools\postgresql-18.4\pgsql\bin'
$env:PGPASSWORD = '<postgres 管理者密碼>'

& (Join-Path $PgBin 'psql.exe') `
  -h 127.0.0.1 `
  -p 5432 `
  -U postgres `
  -d postgres
```

進入 `psql` 後執行:

```sql
DO $do$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'expenselite_app') THEN
        CREATE ROLE expenselite_app LOGIN PASSWORD '<本機開發密碼>';
    ELSE
        ALTER ROLE expenselite_app WITH LOGIN PASSWORD '<本機開發密碼>';
    END IF;
END
$do$;

SELECT 'CREATE DATABASE expenselite_dev OWNER expenselite_app'
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'expenselite_dev')
\gexec

\connect expenselite_dev

ALTER SCHEMA public OWNER TO expenselite_app;
GRANT ALL ON SCHEMA public TO expenselite_app;
```

離開:

```sql
\q
```

### 6. 驗證 app user 權限

```powershell
$env:PGPASSWORD = '<本機開發密碼>'

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dev\Connect-Postgres.ps1 `
  -Command 'select current_database() as database, current_user as user_name; create table if not exists __permission_check(id integer); drop table __permission_check;'
```

看到 `expenselite_dev` / `expenselite_app`,而且 `CREATE TABLE`、`DROP TABLE` 成功,代表 EF Core migration 之後有基本建表權限。

## 日常使用

啟動:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dev\Start-Postgres.ps1
```

連線:

```powershell
$env:PGPASSWORD = '<本機開發密碼>'
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dev\Connect-Postgres.ps1
```

停止:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dev\Stop-Postgres.ps1
```

## ASP.NET Core 之後會用的連線字串

先用 template,不要把真密碼 commit 進 repo:

```text
Host=127.0.0.1;Port=5432;Database=expenselite_dev;Username=expenselite_app;Password=<本機開發密碼>
```

正式接 ASP.NET Core 時,優先把本機密碼放在 user secrets 或本機環境變數,不要寫死在 `appsettings.json`。

## 注意事項

- `.devtools/` 與 `.devdata/` 是本機開發用,已被 git ignore。
- 這不是正式部署方式。正式 Mac 內部主機需要另外處理 macOS PostgreSQL 安裝、ASP.NET Core runtime、HTTPS 憑證、自動啟動與備份。
- 如果 `powershell` 會出現 `oh-my-posh` 找不到的錯誤,用 `-NoProfile` 執行腳本即可避開本機 PowerShell profile 雜訊。
- 如果 port `5432` 被其他 PostgreSQL 佔用,可以用 `Start-Postgres.ps1 -Port <其他port>` 啟動,但 ASP.NET Core 連線字串也要同步改。
