# ASP.NET Core 本機啟動

本專案不把 PostgreSQL 密碼寫進 repo。第一次啟動前，請在 repo root 設定 user secrets：

```powershell
dotnet user-secrets set "ConnectionStrings:ExpenseLite" "Host=127.0.0.1;Port=5432;Database=expenselite_dev;Username=expenselite_app;Password=<本機開發密碼>"
```

日常啟動流程：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dev\Start-Postgres.ps1
dotnet ef database update
dotnet run
```

如果沒有設定 `ConnectionStrings:ExpenseLite`，ASP.NET Core 啟動時會直接提示缺少連線字串。
