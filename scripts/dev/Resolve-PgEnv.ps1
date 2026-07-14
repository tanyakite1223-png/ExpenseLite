# Resolves the PostgreSQL bin / data directory for the current machine.
# Two developer machines are supported:
#   1. repo-local portable binaries (.devtools / .devdata)
#   2. scoop install (%USERPROFILE%\scoop\apps\postgresql\current)

$ErrorActionPreference = 'Stop'

function Resolve-PgEnv {
    $repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path

    $candidates = @(
        [pscustomobject]@{
            Name    = 'portable'
            Bin     = Join-Path $repoRoot '.devtools\postgresql-18.4\pgsql\bin'
            DataDir = Join-Path $repoRoot '.devdata\postgres'
            LogDir  = Join-Path $repoRoot '.devdata'
        }
        [pscustomobject]@{
            Name    = 'scoop'
            Bin     = Join-Path $env:USERPROFILE 'scoop\apps\postgresql\current\bin'
            DataDir = Join-Path $env:USERPROFILE 'scoop\apps\postgresql\current\data'
            LogDir  = Join-Path $env:USERPROFILE 'scoop\apps\postgresql\current\logs'
        }
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $candidate.Bin 'postgres.exe')) {
            return $candidate
        }
    }

    throw "PostgreSQL binaries not found. Expected repo .devtools\postgresql-18.4 or a scoop install. See docs/development/."
}
