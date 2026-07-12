param(
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$RepoPgBin = Join-Path $Root '.devtools\postgresql-18.4\pgsql\bin'
$ScoopPgBin = Join-Path $env:USERPROFILE 'scoop\apps\postgresql\current\bin'
$PgBin = if (Test-Path -LiteralPath (Join-Path $RepoPgBin 'postgres.exe')) {
    $RepoPgBin
}
else {
    $ScoopPgBin
}
$DataDir = Join-Path $Root '.localdb\postgres\data'
$LogDir = Join-Path $Root '.localdb\postgres'
$StdOut = Join-Path $LogDir 'postgres-stdout.log'
$StdErr = Join-Path $LogDir 'postgres-stderr.log'

function Test-TcpPort {
    param(
        [string]$HostName,
        [int]$PortNumber
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $async = $client.BeginConnect($HostName, $PortNumber, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne(500)) {
            return $false
        }

        $client.EndConnect($async)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $PgBin 'postgres.exe'))) {
    throw "PostgreSQL binaries not found. Install PostgreSQL with Scoop or place portable binaries under .devtools."
}

if (-not (Test-Path -LiteralPath (Join-Path $DataDir 'PG_VERSION'))) {
    throw "PostgreSQL data directory not found under .localdb. Run initdb before starting PostgreSQL."
}

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

if (Test-TcpPort -HostName '127.0.0.1' -PortNumber $Port) {
    Write-Host "PostgreSQL is already accepting connections on 127.0.0.1:$Port."
    exit 0
}

$postgres = Join-Path $PgBin 'postgres.exe'
$arguments = @('-D', "`"$DataDir`"", '-p', "$Port")
$process = Start-Process -FilePath $postgres `
    -ArgumentList $arguments `
    -WindowStyle Hidden `
    -RedirectStandardOutput $StdOut `
    -RedirectStandardError $StdErr `
    -PassThru

for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Milliseconds 500
    if ($process.HasExited) {
        break
    }

    if (Test-TcpPort -HostName '127.0.0.1' -PortNumber $Port) {
        Write-Host "PostgreSQL started on 127.0.0.1:$Port. PID: $($process.Id)"
        exit 0
    }
}

Write-Error "PostgreSQL did not start. Check $StdErr for details."

