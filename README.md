# TaskScheduler Cron

A .NET 9 command-line tool that brings Unix `crontab` functionality to Windows Task Scheduler. This tool replicates the familiar `crontab` interface for managing scheduled tasks on Windows.

## Features

- **True crontab interface** - Uses `crontab -l`, `crontab -e`, `crontab -r` commands
- **Cron syntax support** - Standard 5-field cron expressions (`* * * * *`)
- **File-based editing** - Edit jobs in a text file, just like Unix crontab
- **Automatic sync** - Changes automatically sync with Windows Task Scheduler
- **No named tasks** - Just schedule + command, exactly like cron

## Installation

### Install via Scoop (Windows)

```powershell
scoop install https://raw.githubusercontent.com/yetanotherchris/taskscheduler-ui/main/crontab.json
```

### Build from source

```bash
dotnet build
dotnet publish -c Release
```

### Run directly

```bash
dotnet run -- crontab [options]
```

## Usage

This tool works exactly like Unix `crontab`:

### List your crontab

```bash
crontab -l
```

Displays the current crontab file (all scheduled jobs).

### Edit your crontab

```bash
crontab -e
```

Opens your crontab file in an editor (notepad on Windows, or $EDITOR if set). After saving and closing the editor, changes are automatically synchronized with Windows Task Scheduler.

### Remove your crontab

```bash
crontab -r
```

Removes your crontab file and all associated scheduled tasks. Requires confirmation.

## Crontab File Format

The crontab file uses standard Unix cron format:

```
# minute hour day month day-of-week command [arguments...]
0 9 * * * C:\scripts\backup.bat
*/15 * * * * powershell.exe -File C:\scripts\check.ps1
0 2 * * 1 C:\scripts\weekly-backup.bat
30 14 1 * * notepad.exe C:\notes.txt
```

**Format:** `minute hour day month day-of-week command [arguments...]`

**Fields:**
- **minute** - 0-59
- **hour** - 0-23
- **day** - 1-31
- **month** - 1-12
- **day-of-week** - 0-7 (0 or 7 is Sunday)

**Special characters:**
- `*` - Any value
- `*/n` - Every n units (e.g., `*/15` in minute field = every 15 minutes)
- `n-m` - Range (e.g., `1-5` = Monday through Friday)
- `n,m` - List (e.g., `1,15` = 1st and 15th)

**Examples:**
- `0 9 * * *` - Every day at 9:00 AM
- `*/15 * * * *` - Every 15 minutes
- `0 0 * * 0` - Every Sunday at midnight
- `30 14 1 * *` - At 2:30 PM on the 1st of every month
- `0 9 * * 1-5` - Weekdays at 9:00 AM
- `0 */2 * * *` - Every 2 hours

## Command Reference

| Command | Description |
|---------|-------------|
| `crontab -l` | List all cron jobs (display crontab file) |
| `crontab -e` | Edit crontab file in text editor |
| `crontab -r` | Remove all cron jobs and crontab file |

## Examples

### List current cron jobs

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

### Edit crontab

```bash
crontab -e
```

This opens notepad (or your configured editor) with the crontab file. Add your jobs:

```
# Daily backup at 2 AM
0 2 * * * C:\scripts\backup.bat

# Check status every 15 minutes
*/15 * * * * powershell.exe -File C:\scripts\check-status.ps1

# Weekly report on Monday at 9 AM
0 9 * * 1 C:\scripts\weekly-report.bat

# Monthly cleanup on the 1st at midnight
0 0 1 * * C:\scripts\cleanup.bat
```

Save and close the file. The tool will automatically sync these jobs with Windows Task Scheduler.

### Remove all cron jobs

```bash
crontab -r
```

This will prompt for confirmation, then remove your crontab file and all associated tasks.

## How It Works

1. **Crontab file** - Stored at `%USERPROFILE%\.crontab`
2. **Task names** - Automatically generated from job content (you don't see them)
3. **Synchronization** - When you edit the crontab:
   - New jobs are created in Task Scheduler
   - Modified jobs are updated
   - Removed jobs are deleted from Task Scheduler
4. **Prefix** - All tasks are prefixed with `cron-` in Task Scheduler

## Requirements

- .NET 9.0 or later
- Windows (Task Scheduler is Windows-specific)
- Administrator privileges may be required for certain operations

## Technology Stack

- **.NET 9.0** - Latest .NET runtime
- **TaskScheduler** (dahall/taskscheduler) - Windows Task Scheduler wrapper
- **System.CommandLine** - Modern command-line parsing
- **Spectre.Console** - Rich terminal UI
- **Microsoft.Extensions.DependencyInjection** - Dependency injection

## Project Structure

```
taskscheduler-ui/
├── Commands/
│   └── CrontabCommand.cs       # Main crontab command (-l, -e, -r)
├── Services/
│   ├── CrontabService.cs       # Manages crontab file
│   └── TaskSchedulerService.cs # Windows Task Scheduler wrapper
├── Program.cs                  # Entry point with DI setup
└── TaskSchedulerCron.csproj    # Project file
```

## Architecture

The application follows these patterns from [tiny-city](https://github.com/yetanotherchris/tiny-city):

- **Command pattern** - CrontabCommand handles all operations
- **Dependency injection** - Uses Microsoft.Extensions.DependencyInjection
- **Service layer** - Separate services for crontab file and Task Scheduler
- **File-based config** - Crontab file at `%USERPROFILE%\.crontab`
- **Auto-sync** - File changes automatically sync to Task Scheduler

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Credits

- [TaskScheduler Library](https://github.com/dahall/taskscheduler) by David Hall
- Inspired by [tiny-city](https://github.com/yetanotherchris/tiny-city) project structure
