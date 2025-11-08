# TaskScheduler Cron

A .NET 9 command-line tool for managing Windows Task Scheduler with cron-like functionality. This tool provides a familiar cron-style interface for Windows Task Scheduler, making it easy to create, list, view, and delete scheduled tasks.

## Features

- **List all tasks** - View all scheduled tasks in a formatted table (like `crontab -l`)
- **View task details** - Get detailed information about a specific task
- **Create tasks** - Create new scheduled tasks using cron syntax or simple schedules
- **Delete tasks** - Remove scheduled tasks with confirmation
- **Cross-platform CLI** - Uses System.CommandLine for modern command-line parsing
- **Rich terminal output** - Powered by Spectre.Console for beautiful terminal UI

## Installation

### Build from source

```bash
dotnet build
dotnet publish -c Release
```

### Run directly

```bash
dotnet run -- [command] [options]
```

## Usage

### List all scheduled tasks

```bash
taskscheduler-cron list
# or
taskscheduler-cron ls
```

This displays a table with all scheduled tasks including their state, enabled status, last run time, and next run time.

### View a specific task

```bash
taskscheduler-cron view "TaskName"
```

Shows detailed information about a task including:
- Task state and status
- Description
- Triggers (schedule information)
- Actions (what the task executes)

### Create a new task

```bash
# Using cron syntax
taskscheduler-cron create "MyTask" "notepad.exe" -s "0 9 * * *" -d "Open notepad at 9 AM daily"

# Using simple schedule format
taskscheduler-cron create "BackupTask" "C:\backup.bat" -s "daily" -d "Daily backup"

# With arguments
taskscheduler-cron create "MyScript" "powershell.exe" -s "hourly" -a "-File C:\script.ps1" -d "Run script hourly"
```

**Schedule formats:**

**Cron format** (5 fields: minute hour day month day-of-week):
- `0 9 * * *` - Every day at 9:00 AM
- `*/15 * * * *` - Every 15 minutes
- `0 0 * * 0` - Every Sunday at midnight
- `30 14 1 * *` - At 2:30 PM on the 1st of every month

**Simple format:**
- `daily` - Run once per day
- `hourly` - Run every hour
- `weekly` - Run once per week
- `monthly` - Run on the 1st of every month
- `boot` - Run at system startup
- `logon` - Run at user logon

### Delete a task

```bash
# With confirmation prompt
taskscheduler-cron delete "TaskName"

# Force delete without confirmation
taskscheduler-cron delete "TaskName" --force
# or
taskscheduler-cron rm "TaskName" -f
```

## Command Reference

| Command | Aliases | Description |
|---------|---------|-------------|
| `list` | `ls` | List all scheduled tasks |
| `view <name>` | - | View detailed information about a task |
| `create <name> <command>` | - | Create a new scheduled task |
| `delete <name>` | `rm` | Delete a scheduled task |

### Create Command Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--schedule` | `-s` | Yes | Schedule in cron or simple format |
| `--args` | `-a` | No | Arguments to pass to the command |
| `--description` | `-d` | No | Description of the task |

### Delete Command Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--force` | `-f` | No | Force deletion without confirmation |

## Examples

### Create a daily backup task

```bash
taskscheduler-cron create "DailyBackup" "C:\scripts\backup.bat" -s "0 2 * * *" -d "Daily backup at 2 AM"
```

### Create a task that runs every 15 minutes

```bash
taskscheduler-cron create "StatusCheck" "C:\scripts\check-status.ps1" -s "*/15 * * * *" -a "-ExecutionPolicy Bypass" -d "Check system status every 15 minutes"
```

### Create a task that runs at system boot

```bash
taskscheduler-cron create "StartupTask" "C:\scripts\startup.bat" -s "boot" -d "Run at system startup"
```

### List all tasks and view details

```bash
# List all tasks
taskscheduler-cron list

# View specific task
taskscheduler-cron view "DailyBackup"
```

### Delete a task

```bash
# With confirmation
taskscheduler-cron delete "DailyBackup"

# Without confirmation
taskscheduler-cron delete "DailyBackup" --force
```

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
│   ├── ListCommand.cs      # List all tasks
│   ├── ViewCommand.cs      # View task details
│   ├── CreateCommand.cs    # Create new tasks
│   └── DeleteCommand.cs    # Delete tasks
├── Services/
│   └── TaskSchedulerService.cs  # Task Scheduler wrapper
├── Program.cs              # Entry point with DI setup
└── TaskSchedulerCron.csproj    # Project file
```

## Architecture

The application follows these patterns from [tiny-city](https://github.com/yetanotherchris/tiny-city):

- **Command pattern** - Each command is a separate class with a `CreateCommand()` method
- **Dependency injection** - Uses Microsoft.Extensions.DependencyInjection
- **Service layer** - Business logic in service classes
- **System.CommandLine** - For parsing commands and options
- **Spectre.Console** - For rich terminal output

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Credits

- [TaskScheduler Library](https://github.com/dahall/taskscheduler) by David Hall
- Inspired by [tiny-city](https://github.com/yetanotherchris/tiny-city) project structure
