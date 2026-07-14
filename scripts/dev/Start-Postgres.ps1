param(
    [int]$Port = 5432
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Resolve-PgEnv.ps1')
$Pg = Resolve-PgEnv

$StdOut = Join-Path $Pg.LogDir 'postgres-stdout.log'
$StdErr = Join-Path $Pg.LogDir 'postgres-stderr.log'

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

if (-not (Test-Path -LiteralPath (Join-Path $Pg.DataDir 'PG_VERSION'))) {
    throw "PostgreSQL data directory not found at $($Pg.DataDir). Run initdb before starting PostgreSQL."
}

New-Item -ItemType Directory -Force -Path $Pg.LogDir | Out-Null

if (Test-TcpPort -HostName '127.0.0.1' -PortNumber $Port) {
    Write-Host "PostgreSQL is already accepting connections on 127.0.0.1:$Port."
    exit 0
}

$postgres = Join-Path $Pg.Bin 'postgres.exe'
$arguments = @('-D', "`"$($Pg.DataDir)`"", '-p', "$Port")
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
        Write-Host "PostgreSQL ($($Pg.Name)) started on 127.0.0.1:$Port. PID: $($process.Id)"
        exit 0
    }
}

Write-Error "PostgreSQL did not start. Check $StdErr for details."
