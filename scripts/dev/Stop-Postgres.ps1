param(
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Resolve-PgEnv.ps1')
$Pg = Resolve-PgEnv

$PgCtl = Join-Path $Pg.Bin 'pg_ctl.exe'

if (Test-Path -LiteralPath $PgCtl) {
    & $PgCtl -D $Pg.DataDir -m fast -w stop
    if ($LASTEXITCODE -eq 0) {
        exit 0
    }
}

$binRoot = (Resolve-Path -LiteralPath $Pg.Bin).Path
$processes = Get-Process -Name postgres -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$binRoot*" }

if (-not $processes) {
    Write-Host "No PostgreSQL process ($($Pg.Name)) is running."
    exit 0
}

$processes | Stop-Process
Write-Host "Stopped PostgreSQL processes ($($Pg.Name))."
