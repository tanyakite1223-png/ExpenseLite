param(
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$PgBin = Join-Path $Root '.devtools\postgresql-18.4\pgsql\bin'
$DataDir = Join-Path $Root '.devdata\postgres'
$PgCtl = Join-Path $PgBin 'pg_ctl.exe'

if (Test-Path -LiteralPath $PgCtl) {
    & $PgCtl -D $DataDir -m fast -w stop
    if ($LASTEXITCODE -eq 0) {
        exit 0
    }
}

$postgresRoot = (Resolve-Path -LiteralPath $PgBin).Path
$processes = Get-Process -Name postgres -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$postgresRoot*" }

if (-not $processes) {
    Write-Host "No portable PostgreSQL process is running for this repo."
    exit 0
}

$processes | Stop-Process
Write-Host "Stopped portable PostgreSQL processes for this repo."

