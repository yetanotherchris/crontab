# crontab

A .NET 9 command-line tool that brings Unix `crontab` functionality to Windows Task Scheduler. It runs as your user to ensure no popup Windows are generated.

## Features

- Uses standard `crontab -l`, `crontab -e`, `crontab -r` commands
- Supports standard 5-field cron expressions (`* * * * *`)
- File-based editing with automatic sync to Windows Task Scheduler
- Stores crontab at `%USERPROFILE%\.crontab\crontab`

## Installation

### Install via Scoop (Windows)

```powershell
scoop bucket add crontab https://github.com/yetanotherchris/crontab
scoop install crontab
```

## Usage

### List your crontab

```bash
crontab -l
```

Output:
```
# Crontab file for Windows Task Scheduler
# Format: minute hour day month day-of-week command [arguments...]

0 9 * * * C:\scripts\backup.bat
*/15 * * * * powershell.exe -File C:\scripts\check.ps1
0 2 * * 1 C:\scripts\weekly-backup.bat
```

### Edit your crontab

```bash
crontab -e
```

Opens your crontab file in an editor. Uses `VISUAL` or `EDITOR` environment variable if set, otherwise defaults to notepad. Changes are synchronized with Windows Task Scheduler when you save and close.

### Remove your crontab

```bash
crontab -r
```

Removes your crontab file and all associated scheduled tasks.

## Crontab File Format

```
* * * * * command
│ │ │ │ │
│ │ │ │ └─ day of week (0–7, 0 and 7 = Sunday)
│ │ │ └─── month (1–12)
│ │ └───── day of month (1–31)
│ └─────── hour (0–23)
└───────── minute (0–59)
```

Special characters:
- `*` - Any value
- `*/n` - Every n units (e.g., `*/15` = every 15 minutes)
- `n-m` - Range (e.g., `1-5` = Monday through Friday)
- `n,m` - List (e.g., `1,15` = 1st and 15th)

Examples:
- `0 9 * * *` - Every day at 9:00 AM
- `*/15 * * * *` - Every 15 minutes
- `0 0 * * 0` - Every Sunday at midnight
- `30 14 1 * *` - At 2:30 PM on the 1st of every month
- `0 9 * * 1-5` - Weekdays at 9:00 AM
- `0 */2 * * *` - Every 2 hours

### Build from source

```bash
dotnet build
dotnet publish -c Release
```

### Run directly

```bash
dotnet run -- crontab [options]
```
