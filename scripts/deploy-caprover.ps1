# Deploys this repo to CapRover from Windows PowerShell.
# CapRover CLI is installed if missing (requires Node.js / npm).
#
# Dashboard (one-time per app):
#   Container HTTP Port = 8080
#   Force HTTPS = on
#   App env vars: TZ=Asia/Tehran, AllowedHosts, ConnectionStrings__DefaultConnection,
#   Database__MigrateOnStartup=true, Storage__S3__*, Seed__Admin__*
#   (TZ is also set in the Docker image; set it in App Configs to override/confirm.)
#
# Usage:
#   Copy .env.caprover.example → .env.caprover and set CAPROVER_* values
#   .\scripts\deploy-caprover.ps1
#   .\scripts\deploy-caprover.ps1 -Branch other-branch
#   .\scripts\deploy-caprover.ps1 -UseSavedLogin
#   $env:CAPROVER_URL = 'https://captain.example.com'
#   $env:CAPROVER_APP = 'mirka-cms'
#   $env:CAPROVER_APP_TOKEN = '...'
#   .\scripts\deploy-caprover.ps1

[CmdletBinding()]
param(
    [string]$AppName,
    [string]$CapRoverUrl,
    [string]$AppToken,
    [string]$Password,
    [string]$MachineName,
    [string]$Branch,
    [string]$EnvFile,
    [switch]$UseSavedLogin,
    [switch]$Tar
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$DefinitionPath = Join-Path $RepoRoot 'captain-definition'

function Import-DotEnvFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    foreach ($raw in Get-Content -LiteralPath $Path -Encoding UTF8) {
        $line = $raw.Trim()
        if (-not $line -or $line.StartsWith('#')) {
            continue
        }

        $eq = $line.IndexOf('=')
        if ($eq -lt 1) {
            continue
        }

        $key = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        if ($value.Length -ge 2) {
            $quote = $value[0]
            if (($quote -eq '"' -or $quote -eq "'") -and $value[$value.Length - 1] -eq $quote) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }

        if (-not $key -or [string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        $existing = [Environment]::GetEnvironmentVariable($key, 'Process')
        if (-not [string]::IsNullOrWhiteSpace($existing)) {
            continue
        }

        [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    }

    return $true
}

if ([string]::IsNullOrWhiteSpace($EnvFile)) {
    $EnvFile = Join-Path $RepoRoot '.env.caprover'
}

if (Import-DotEnvFile -Path $EnvFile) {
    Write-Host "Loaded $EnvFile"
}
else {
    Write-Host "No $EnvFile found. Copy .env.caprover.example to .env.caprover, or set CAPROVER_* in the shell."
}

if ([string]::IsNullOrWhiteSpace($AppName)) { $AppName = $env:CAPROVER_APP }
if ([string]::IsNullOrWhiteSpace($AppName)) { $AppName = 'mirka-cms' }
if ([string]::IsNullOrWhiteSpace($CapRoverUrl)) { $CapRoverUrl = $env:CAPROVER_URL }
if ([string]::IsNullOrWhiteSpace($AppToken)) { $AppToken = $env:CAPROVER_APP_TOKEN }
if ([string]::IsNullOrWhiteSpace($Password)) { $Password = $env:CAPROVER_PASSWORD }
if ([string]::IsNullOrWhiteSpace($MachineName)) { $MachineName = $env:CAPROVER_NAME }
if ([string]::IsNullOrWhiteSpace($Branch)) { $Branch = $env:CAPROVER_BRANCH }

if (-not [string]::IsNullOrWhiteSpace($CapRoverUrl)) {
    $CapRoverUrl = $CapRoverUrl.TrimEnd('/')
}

if (-not $UseSavedLogin) {
    if (-not [string]::IsNullOrWhiteSpace($AppToken)) {
        if ([string]::IsNullOrWhiteSpace($CapRoverUrl)) {
            throw 'CAPROVER_URL is required when deploying with CAPROVER_APP_TOKEN.'
        }
        if ($CapRoverUrl -match 'example\.com') {
            throw "CAPROVER_URL is still a placeholder. Set the real captain URL in $EnvFile (not .env.caprover.example)."
        }
        $Password = $null
        $MachineName = $null
    }
    elseif (-not [string]::IsNullOrWhiteSpace($Password)) {
        if ([string]::IsNullOrWhiteSpace($CapRoverUrl)) {
            throw 'CAPROVER_URL is required when deploying with CAPROVER_PASSWORD.'
        }
    }
    else {
        throw @"
CAPROVER_APP_TOKEN is empty, so CapRover CLI will ask for the machine password.
Put the app token in $EnvFile (copy from .env.caprover.example if needed).
Enable it in CapRover: Apps → $AppName → Deployment → Enable App Token.
"@
    }
}

function Write-CaptainDefinition {
    $json = @'
{
  "schemaVersion": 2,
  "dockerfilePath": "./Dockerfile"
}
'@
    Set-Content -Path $DefinitionPath -Value $json.Trim() -Encoding utf8
    Write-Host "Created $DefinitionPath"
}

if (-not (Test-Path -LiteralPath $DefinitionPath)) {
    Write-CaptainDefinition
}

$dockerfile = Join-Path $RepoRoot 'Dockerfile'
if (-not (Test-Path -LiteralPath $dockerfile)) {
    throw "Dockerfile not found at $dockerfile"
}

if (-not (Get-Command caprover -ErrorAction SilentlyContinue)) {
    if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
        throw 'caprover CLI is missing. Install Node.js, then run: npm install -g caprover'
    }
    Write-Host 'Installing caprover CLI...'
    npm install -g caprover
    if (-not (Get-Command caprover -ErrorAction SilentlyContinue)) {
        throw 'caprover CLI install finished but the caprover command is still not on PATH. Open a new terminal and retry.'
    }
}

$deployArgs = @('deploy')

if ($UseSavedLogin) {
    $deployArgs += '-d'
}
else {
    if ($CapRoverUrl) { $deployArgs += @('--caproverUrl', $CapRoverUrl) }
    if ($AppName) { $deployArgs += @('--caproverApp', $AppName) }
    if ($MachineName) { $deployArgs += @('--caproverName', $MachineName) }
    if ($AppToken) { $deployArgs += @('--appToken', $AppToken) }
    elseif ($Password) { $deployArgs += @('--caproverPassword', $Password) }
}

if (-not $Tar) {
    if ([string]::IsNullOrWhiteSpace($Branch)) {
        $git = Get-Command git -ErrorAction SilentlyContinue
        if (-not $git) {
            throw 'git is required to detect the current branch. Pass -Branch explicitly, or install git.'
        }

        Push-Location $RepoRoot
        try {
            $Branch = (& git rev-parse --abbrev-ref HEAD).Trim()
            if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($Branch) -or $Branch -eq 'HEAD') {
                throw 'Could not detect the current git branch (detached HEAD?). Pass -Branch explicitly.'
            }
        }
        finally {
            Pop-Location
        }
    }

    $deployArgs += @('--branch', $Branch)
}

$tarPath = $null
if ($Tar) {
    $tarPath = Join-Path ([System.IO.Path]::GetTempPath()) ("mirka-cms-caprover-{0}.tar" -f [guid]::NewGuid().ToString('N'))
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        throw 'git is required for -Tar deploy.'
    }
    Push-Location $RepoRoot
    try {
        & git archive --format=tar -o $tarPath HEAD
        if ($LASTEXITCODE -ne 0) {
            throw "git archive failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }
    $deployArgs += @('--tarFile', $tarPath)
}

Write-Host "Deploying from $RepoRoot"
if ($Branch) {
    Write-Host "Branch: $Branch"
}
if ($AppToken) {
    Write-Host "Auth: app token → $AppName @ $CapRoverUrl"
}
elseif ($UseSavedLogin) {
    Write-Host 'Auth: saved CapRover login (-d)'
}
else {
    Write-Host "Auth: machine password → $AppName @ $CapRoverUrl"
}
Write-Host 'Reminder: set CapRover Container HTTP Port to 8080.'

Push-Location $RepoRoot
try {
    & caprover @deployArgs
    if ($LASTEXITCODE -ne 0) {
        throw "caprover deploy failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
    if ($tarPath -and (Test-Path -LiteralPath $tarPath)) {
        Remove-Item -LiteralPath $tarPath -Force
    }
}
