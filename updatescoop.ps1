param (
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

# Find the published exe
$exePath = Get-ChildItem -Path $PSScriptRoot -Filter "crontab-v$Version-win-x64.exe" -Recurse | Select-Object -First 1

if (-not $exePath) {
    Write-Error "Could not find crontab-v$Version-win-x64.exe"
    exit 1
}

Write-Host "Found exe at: $($exePath.FullName)"

# Calculate SHA256 hash
$hash = (Get-FileHash -Path $exePath.FullName -Algorithm SHA256).Hash
Write-Host "SHA256 Hash: $hash"

# Create scoop manifest
$manifest = @{
    version = $Version
    architecture = @{
        "64bit" = @{
            url = "https://github.com/yetanotherchris/taskscheduler-ui/releases/download/v$Version/crontab-v$Version-win-x64.exe"
            bin = "crontab.exe"
            hash = $hash
        }
    }
    homepage = "https://github.com/yetanotherchris/taskscheduler-ui"
    license = "MIT License"
    description = "A command line tool that brings Unix crontab functionality to Windows Task Scheduler"
} | ConvertTo-Json -Depth 10

# Write to file
$outputPath = Join-Path $PSScriptRoot "crontab.json"
[System.IO.File]::WriteAllText($outputPath, $manifest, [System.Text.Encoding]::UTF8)

Write-Host "Scoop manifest written to: $outputPath"
