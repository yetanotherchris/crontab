# Crontab Tests

This directory contains comprehensive unit tests for the crontab project.

## Test Coverage

### CrontabServiceTests.cs
Tests for the CrontabService class that handles parsing and managing crontab entries.

#### Standard Cron Format Tests
- Parsing of standard 5-field cron expressions (e.g., `0 0 * * * command`)
- Complex schedules with ranges (e.g., `0 9-17 * * 1-5`)
- Schedules with step values (e.g., `*/15 * * * *`)
- Schedules with lists (e.g., `0 0,12 * * *`)

#### Special Schedule Tests
- `@hourly` - Run every hour
- `@daily` / `@midnight` - Run once a day at midnight
- `@weekly` - Run once a week
- `@monthly` - Run once a month
- `@yearly` / `@annually` - Run once a year
- `@reboot` - Run at startup

#### Command and Arguments Tests
- Commands with multiple arguments
- Commands with quoted arguments
- Commands with paths containing spaces
- Commands without arguments

#### Empty Lines and Comments Tests
- Empty lines are properly ignored
- Comment lines (starting with `#`) are ignored
- Whitespace-only lines are ignored
- Mixed comments and valid entries

#### Edge Cases
- Non-existent crontab files
- Empty crontab files
- Invalid format entries (too few fields)
- Leading and trailing whitespace
- Multiple consecutive spaces
- Tab characters as separators

#### Multiple Entries Tests
- Multiple valid entries in one file
- Real-world crontab file examples

#### Task Name Generation Tests
- Unique task names for different entries
- Consistent task names for identical entries

#### Write and Clear Tests
- Writing crontab entries to file
- Clearing/deleting crontab files

### TaskSchedulerServiceTests.cs
Tests for the TaskSchedulerService class that integrates with Windows Task Scheduler.

#### Schedule Parsing Tests
- All special schedule formats (`@hourly`, `@daily`, etc.)
- Correct trigger type generation (DailyTrigger, WeeklyTrigger, etc.)
- Correct trigger configuration for each schedule type
- Standard cron format parsing via Windows Task Scheduler
- Invalid schedule format handling
- Case-insensitive parsing

#### Integration Tests
- CrontabEntry properties
- TaskInfo properties

## Running Tests

### Local Development
```bash
dotnet test
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### In GitHub Actions
Tests are automatically run as part of the build workflow in `.github/workflows/build-release.yml`.

## Test Framework
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **coverlet** - Code coverage tool

## Notes
- Tests use reflection to test private methods where necessary (e.g., `ParseSchedule` in TaskSchedulerService)
- Tests create temporary files for crontab testing to avoid affecting the actual user's crontab
- Tests are designed to run on Windows (due to TaskScheduler dependency) but the CrontabService tests are platform-independent
