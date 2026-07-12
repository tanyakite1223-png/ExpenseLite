param(
    [string]$Database = 'expenselite_dev',
    [string]$User = 'expenselite_app',
    [int]$Port = 5432,
    [string]$Command
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Resolve-PgEnv.ps1')
$Pg = Resolve-PgEnv

$Psql = Join-Path $Pg.Bin 'psql.exe'

if (-not (Test-Path -LiteralPath $Psql)) {
    throw "psql.exe not found at $Psql."
}

if (-not $env:PGPASSWORD) {
    $env:PGPASSWORD = Read-Host 'PostgreSQL password'
}

if ($Command) {
    & $Psql -h 127.0.0.1 -p $Port -U $User -d $Database -c $Command
}
else {
    & $Psql -h 127.0.0.1 -p $Port -U $User -d $Database
}
