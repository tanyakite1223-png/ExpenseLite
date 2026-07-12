param(
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$RepoPgBin = Join-Path $Root '.devtools\postgresql-18.4\pgsql\bin'
$ScoopPgBin = Join-Path $env:USERPROFILE 'scoop\apps\postgresql\current\bin'
$PgBin = if (Test-Path -LiteralPath (Join-Path $RepoPgBin 'pg_ctl.exe')) {
    $RepoPgBin
}
else {
    $ScoopPgBin
}
$DataDir = Join-Path $Root '.localdb\postgres\data'
$PgCtl = Join-Path $PgBin 'pg_ctl.exe'

if ((Test-Path -LiteralPath $PgCtl) -and (Test-Path -LiteralPath (Join-Path $DataDir 'PG_VERSION'))) {
    & $PgCtl -D $DataDir -m fast -w stop
    if ($LASTEXITCODE -eq 0) {
        exit 0
    }
}

if (-not (Test-Path -LiteralPath $PgBin)) {
    Write-Host "PostgreSQL binaries not found."
    exit 0
}

$PidFile = Join-Path $DataDir 'postmaster.pid'
if (-not (Test-Path -LiteralPath $PidFile)) {
    Write-Host "No PostgreSQL postmaster.pid found for this repo."
    exit 0
}

$postmasterPid = [int](Get-Content -LiteralPath $PidFile -TotalCount 1)
$processes = Get-Process -Id $postmasterPid -ErrorAction SilentlyContinue

if (-not $processes) {
    Write-Host "No PostgreSQL process is running for this repo."
    exit 0
}

$processes | Stop-Process
Write-Host "Stopped PostgreSQL postmaster process for this repo."

