param(
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Resolve-PgEnv.ps1')
$Pg = Resolve-PgEnv

$PgCtl = Join-Path $Pg.Bin 'pg_ctl.exe'

# 優先用 pg_ctl 正常關閉（需要 data directory 已初始化）
if ((Test-Path -LiteralPath $PgCtl) -and (Test-Path -LiteralPath (Join-Path $Pg.DataDir 'PG_VERSION'))) {
    & $PgCtl -D $Pg.DataDir -m fast -w stop
    if ($LASTEXITCODE -eq 0) {
        exit 0
    }
}

# pg_ctl 失敗時的後備：只停「這個 data directory 的 postmaster」，用 postmaster.pid
# 對到確切的 PID，避免用路徑比對誤殺其他 cluster 的 postgres。
$pidFile = Join-Path $Pg.DataDir 'postmaster.pid'
if (-not (Test-Path -LiteralPath $pidFile)) {
    Write-Host "No PostgreSQL postmaster.pid found for this data directory ($($Pg.Name))."
    exit 0
}

$postmasterPid = [int](Get-Content -LiteralPath $pidFile -TotalCount 1)
$process = Get-Process -Id $postmasterPid -ErrorAction SilentlyContinue

if (-not $process) {
    Write-Host "No PostgreSQL process ($($Pg.Name)) is running for this repo."
    exit 0
}

$process | Stop-Process
Write-Host "Stopped PostgreSQL postmaster process ($($Pg.Name))."
