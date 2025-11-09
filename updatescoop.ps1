param(
    [Parameter(Mandatory = $true)]
    [string]
    $Version
)

# Look for the renamed Windows executable in the current directory
$searchPattern = "crontab-v$Version-win-x64.exe"
$crontabFile = Get-ChildItem -Path $PSScriptRoot -File | Where-Object { $_.Name -eq $searchPattern } | Select-Object -First 1

if (-not $crontabFile) {
    throw "Unable to locate $searchPattern in the current directory."
}

$filePath = $crontabFile.FullName
Write-Output "File found: $filePath, getting hash..."
$hash = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash
Write-Output "Hash: $hash"

$manifest = @{
    version = $Version
    architecture = @{
        '64bit' = @{
            url = "https://github.com/yetanotherchris/crontab/releases/download/v$Version/crontab-v$Version-win-x64.exe"
            bin = @("crontab.exe")
            hash = $hash
            extract_dir = ""
            pre_install = @("Rename-Item `"`$dir\crontab-v$Version-win-x64.exe`" `"crontab.exe`"")
        }
    }
    homepage = "https://github.com/yetanotherchris/crontab"
    license = "MIT License"
    description = "A command line tool that brings Unix crontab functionality to Windows Task Scheduler"
}

Write-Output "Creating crontab.json for version $Version..."
$manifest | ConvertTo-Json -Depth 5 | Out-File -FilePath "crontab.json" -Encoding utf8
