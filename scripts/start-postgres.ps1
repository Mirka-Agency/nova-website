# Starts local PostgreSQL 18 without needing the Windows service (no Admin required).
# Usage: ./scripts/start-postgres.ps1

$ErrorActionPreference = "Stop"

$pgRoot = "C:\Program Files\PostgreSQL\18"
$pgCtl = Join-Path $pgRoot "bin\pg_ctl.exe"
$dataDir = Join-Path $pgRoot "data"
$logDir = Join-Path $dataDir "log"
$logFile = Join-Path $logDir "pg-start.log"

if (-not (Test-Path $pgCtl)) {
    Write-Error "pg_ctl not found at $pgCtl. Is PostgreSQL 18 installed?"
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$status = & $pgCtl status -D $dataDir 2>&1 | Out-String
if ($status -match "server is running") {
    Write-Host "PostgreSQL is already running."
    exit 0
}

Write-Host "Starting PostgreSQL..."
& $pgCtl start -D $dataDir -l $logFile -w
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to start PostgreSQL. See log: $logFile"
}

Write-Host "PostgreSQL started on localhost:5432"
