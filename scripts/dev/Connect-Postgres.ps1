param(
    [string]$Database = 'expenselite_dev',
    [string]$User = 'expenselite_app',
    [int]$Port = 5432,
    [string]$Command
)

$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$RepoPgBin = Join-Path $Root '.devtools\postgresql-18.4\pgsql\bin'
$ScoopPgBin = Join-Path $env:USERPROFILE 'scoop\apps\postgresql\current\bin'
$PgBin = if (Test-Path -LiteralPath (Join-Path $RepoPgBin 'psql.exe')) {
    $RepoPgBin
}
else {
    $ScoopPgBin
}
$Psql = Join-Path $PgBin 'psql.exe'

if (-not (Test-Path -LiteralPath $Psql)) {
    throw "psql.exe not found. Install PostgreSQL with Scoop or place portable binaries under .devtools."
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
