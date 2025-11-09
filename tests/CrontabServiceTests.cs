using Xunit;
using Crontab.Services;

namespace Crontab.Tests;

public class CrontabServiceTests : IDisposable
{
    private readonly string _testCrontabPath;
    private readonly TestCrontabService _service;

    public CrontabServiceTests()
    {
        // Use a temporary file for testing
        _testCrontabPath = Path.Combine(Path.GetTempPath(), $"test_crontab_{Guid.NewGuid()}.txt");
        _service = new TestCrontabService(_testCrontabPath);
    }

    public void Dispose()
    {
        // Cleanup test file
        if (File.Exists(_testCrontabPath))
        {
            File.Delete(_testCrontabPath);
        }
    }

    #region Standard Cron Format Tests

    [Theory]
    [InlineData("0 0 * * * /usr/bin/backup.sh", "0 0 * * *", "/usr/bin/backup.sh", "")]
    [InlineData("*/5 * * * * C:\\scripts\\check.bat", "*/5 * * * *", "C:\\scripts\\check.bat", "")]
    [InlineData("0 9 * * 1-5 powershell.exe", "0 9 * * 1-5", "powershell.exe", "")]
    [InlineData("30 2 1 * * cmd.exe /c echo hello", "30 2 1 * *", "cmd.exe", "/c echo hello")]
    [InlineData("0 */2 * * * script.sh arg1 arg2", "0 */2 * * *", "script.sh", "arg1 arg2")]
    public void ParseCrontabLine_StandardCronFormat_ParsesCorrectly(string line, string expectedSchedule, string expectedCommand, string expectedArgs)
    {
        // Arrange
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal(expectedSchedule, entries[0].Schedule);
        Assert.Equal(expectedCommand, entries[0].Command);
        Assert.Equal(expectedArgs, entries[0].Arguments);
    }

    [Fact]
    public void ParseCrontabLine_ComplexScheduleWithRanges_ParsesCorrectly()
    {
        // Arrange
        var line = "0 9-17 * * 1-5 C:\\scripts\\workday.bat";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("0 9-17 * * 1-5", entries[0].Schedule);
        Assert.Equal("C:\\scripts\\workday.bat", entries[0].Command);
    }

    [Fact]
    public void ParseCrontabLine_ScheduleWithSteps_ParsesCorrectly()
    {
        // Arrange
        var line = "*/15 * * * * /usr/bin/monitor.sh";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("*/15 * * * *", entries[0].Schedule);
        Assert.Equal("/usr/bin/monitor.sh", entries[0].Command);
    }

    [Fact]
    public void ParseCrontabLine_ScheduleWithLists_ParsesCorrectly()
    {
        // Arrange
        var line = "0 0,12 * * * backup.sh";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("0 0,12 * * *", entries[0].Schedule);
        Assert.Equal("backup.sh", entries[0].Command);
    }

    #endregion

    #region Special Schedule Tests

    [Theory]
    [InlineData("@hourly /usr/bin/check.sh", "@hourly", "/usr/bin/check.sh", "")]
    [InlineData("@daily backup.bat", "@daily", "backup.bat", "")]
    [InlineData("@midnight C:\\scripts\\midnight.ps1", "@midnight", "C:\\scripts\\midnight.ps1", "")]
    [InlineData("@weekly /scripts/weekly.sh", "@weekly", "/scripts/weekly.sh", "")]
    [InlineData("@monthly report.bat", "@monthly", "report.bat", "")]
    [InlineData("@yearly /usr/bin/yearly.sh", "@yearly", "/usr/bin/yearly.sh", "")]
    [InlineData("@annually annual.bat", "@annually", "annual.bat", "")]
    [InlineData("@reboot startup.sh", "@reboot", "startup.sh", "")]
    public void ParseCrontabLine_SpecialSchedules_ParsesCorrectly(string line, string expectedSchedule, string expectedCommand, string expectedArgs)
    {
        // Arrange
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal(expectedSchedule, entries[0].Schedule);
        Assert.Equal(expectedCommand, entries[0].Command);
        Assert.Equal(expectedArgs, entries[0].Arguments);
    }

    [Fact]
    public void ParseCrontabLine_SpecialScheduleWithArguments_ParsesCorrectly()
    {
        // Arrange
        var line = "@hourly powershell.exe -File C:\\scripts\\check.ps1 -Verbose";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("@hourly", entries[0].Schedule);
        Assert.Equal("powershell.exe", entries[0].Command);
        Assert.Equal("-File C:\\scripts\\check.ps1 -Verbose", entries[0].Arguments);
    }

    #endregion

    #region Command and Arguments Tests

    [Fact]
    public void ParseCrontabLine_CommandWithMultipleArguments_ParsesCorrectly()
    {
        // Arrange
        var line = "0 3 * * * rclone sync C:\\data remote:backup --log-file=C:\\logs\\rclone.log";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("rclone", entries[0].Command);
        Assert.Equal("sync C:\\data remote:backup --log-file=C:\\logs\\rclone.log", entries[0].Arguments);
    }

    [Fact]
    public void ParseCrontabLine_CommandWithQuotedArguments_ParsesCorrectly()
    {
        // Arrange
        var line = "0 9 * * * cmd.exe /c \"echo hello world\"";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("cmd.exe", entries[0].Command);
        Assert.Equal("/c echo hello world", entries[0].Arguments);
    }

    [Fact]
    public void ParseCrontabLine_CommandWithPathContainingSpaces_ParsesCorrectly()
    {
        // Arrange
        var line = "0 12 * * * \"C:\\Program Files\\MyApp\\app.exe\" --param value";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("C:\\Program Files\\MyApp\\app.exe", entries[0].Command);
        Assert.Equal("--param value", entries[0].Arguments);
    }

    [Fact]
    public void ParseCrontabLine_CommandOnly_NoArguments()
    {
        // Arrange
        var line = "0 0 * * * backup.bat";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("backup.bat", entries[0].Command);
        Assert.Equal("", entries[0].Arguments);
    }

    #endregion

    #region Empty Lines and Comments Tests

    [Fact]
    public void ReadCrontab_EmptyLines_AreIgnored()
    {
        // Arrange
        var content = @"0 0 * * * backup.sh

0 12 * * * lunch.sh

";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("backup.sh", entries[0].Command);
        Assert.Equal("lunch.sh", entries[1].Command);
    }

    [Fact]
    public void ReadCrontab_CommentLines_AreIgnored()
    {
        // Arrange
        var content = @"# This is a comment
0 0 * * * backup.sh
# Another comment
# Yet another comment
0 12 * * * lunch.sh";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("backup.sh", entries[0].Command);
        Assert.Equal("lunch.sh", entries[1].Command);
    }

    [Fact]
    public void ReadCrontab_WhitespaceOnlyLines_AreIgnored()
    {
        // Arrange
        var content = @"0 0 * * * backup.sh


0 12 * * * lunch.sh";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public void ReadCrontab_MixedCommentsAndEmptyLines_ParsesValidEntriesOnly()
    {
        // Arrange
        var content = @"# Crontab file
# Run backups

0 0 * * * backup.sh

# Check status every hour
@hourly check.sh

# End of file";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("backup.sh", entries[0].Command);
        Assert.Equal("check.sh", entries[1].Command);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void ReadCrontab_NonExistentFile_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");
        var service = new TestCrontabService(nonExistentPath);

        // Act
        var entries = service.ReadCrontab().ToList();

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void ReadCrontab_EmptyFile_ReturnsEmptyList()
    {
        // Arrange
        File.WriteAllText(_testCrontabPath, "");

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void ParseCrontabLine_InvalidFormat_TooFewFields_Ignored()
    {
        // Arrange
        var content = @"0 0 * * * valid.sh
0 0 invalid.sh
0 12 * * * alsovalid.sh";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("valid.sh", entries[0].Command);
        Assert.Equal("alsovalid.sh", entries[1].Command);
    }

    [Fact]
    public void ParseCrontabLine_InvalidFormat_OnlyCommand_Ignored()
    {
        // Arrange
        var content = @"0 0 * * * valid.sh
justcommand
0 12 * * * alsovalid.sh";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("valid.sh", entries[0].Command);
        Assert.Equal("alsovalid.sh", entries[1].Command);
    }

    [Fact]
    public void ParseCrontabLine_LeadingAndTrailingWhitespace_Trimmed()
    {
        // Arrange
        var content = "   0 0 * * * backup.sh   ";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("backup.sh", entries[0].Command);
    }

    [Fact]
    public void ParseCrontabLine_MultipleConsecutiveSpaces_ParsedCorrectly()
    {
        // Arrange
        var line = "0  0  *  *  *  backup.sh";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("0 0 * * *", entries[0].Schedule);
        Assert.Equal("backup.sh", entries[0].Command);
    }

    [Fact]
    public void ParseCrontabLine_TabCharacters_ParsedCorrectly()
    {
        // Arrange
        var line = "0\t0\t*\t*\t*\tbackup.sh";
        File.WriteAllText(_testCrontabPath, line);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Single(entries);
        Assert.Equal("0 0 * * *", entries[0].Schedule);
        Assert.Equal("backup.sh", entries[0].Command);
    }

    #endregion

    #region Multiple Entries Tests

    [Fact]
    public void ReadCrontab_MultipleValidEntries_AllParsed()
    {
        // Arrange
        var content = @"0 0 * * * backup.sh
0 12 * * * lunch.sh
*/15 * * * * monitor.sh
@hourly check.sh
@daily report.sh";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(5, entries.Count);
        Assert.Equal("backup.sh", entries[0].Command);
        Assert.Equal("lunch.sh", entries[1].Command);
        Assert.Equal("monitor.sh", entries[2].Command);
        Assert.Equal("check.sh", entries[3].Command);
        Assert.Equal("report.sh", entries[4].Command);
    }

    [Fact]
    public void ReadCrontab_RealWorldExample_ParsesCorrectly()
    {
        // Arrange
        var content = @"# Crontab file for Windows Task Scheduler
# Format: minute hour day month day-of-week command [arguments...]

# Run backup every day at 3 AM
0 3 * * * C:\scripts\backup.bat

# Sync to cloud storage daily at 3 AM
0 3 * * * rclone sync C:\data remote:s3-backup --log-file=C:\logs\rclone.log

# Check status every 15 minutes
*/15 * * * * powershell.exe -File C:\scripts\status.ps1

# Weekly report on Monday at 9 AM
0 9 * * 1 C:\scripts\weekly-report.bat

# Hourly health check
@hourly C:\scripts\healthcheck.bat
";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(5, entries.Count);

        Assert.Equal("0 3 * * *", entries[0].Schedule);
        Assert.Equal("C:\\scripts\\backup.bat", entries[0].Command);

        Assert.Equal("0 3 * * *", entries[1].Schedule);
        Assert.Equal("rclone", entries[1].Command);
        Assert.Contains("remote:s3-backup", entries[1].Arguments);

        Assert.Equal("*/15 * * * *", entries[2].Schedule);
        Assert.Equal("powershell.exe", entries[2].Command);

        Assert.Equal("0 9 * * 1", entries[3].Schedule);
        Assert.Equal("C:\\scripts\\weekly-report.bat", entries[3].Command);

        Assert.Equal("@hourly", entries[4].Schedule);
        Assert.Equal("C:\\scripts\\healthcheck.bat", entries[4].Command);
    }

    #endregion

    #region Task Name Generation Tests

    [Fact]
    public void ParseCrontabLine_GeneratesUniqueTaskNames()
    {
        // Arrange
        var content = @"0 0 * * * backup.sh
0 12 * * * backup.sh
0 0 * * * backup.sh arg1";
        File.WriteAllText(_testCrontabPath, content);

        // Act
        var entries = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(3, entries.Count);
        Assert.NotEqual(entries[0].TaskName, entries[1].TaskName);
        Assert.NotEqual(entries[0].TaskName, entries[2].TaskName);
        Assert.NotEqual(entries[1].TaskName, entries[2].TaskName);
    }

    [Fact]
    public void ParseCrontabLine_SameContent_GeneratesSameTaskName()
    {
        // Arrange
        var line = "0 0 * * * backup.sh";
        File.WriteAllText(_testCrontabPath, line);
        var entries1 = _service.ReadCrontab().ToList();

        // Clear and re-parse
        File.WriteAllText(_testCrontabPath, line);
        var entries2 = _service.ReadCrontab().ToList();

        // Assert
        Assert.Equal(entries1[0].TaskName, entries2[0].TaskName);
    }

    #endregion

    #region Write and Clear Tests

    [Fact]
    public void WriteCrontab_CreatesFileWithEntries()
    {
        // Arrange
        var entries = new List<CrontabEntry>
        {
            new CrontabEntry
            {
                Schedule = "0 0 * * *",
                Command = "backup.sh",
                Arguments = ""
            },
            new CrontabEntry
            {
                Schedule = "@hourly",
                Command = "check.sh",
                Arguments = "arg1"
            }
        };

        // Act
        _service.WriteCrontab(entries);

        // Assert
        Assert.True(File.Exists(_testCrontabPath));
        var content = File.ReadAllText(_testCrontabPath);
        Assert.Contains("0 0 * * * backup.sh", content);
        Assert.Contains("@hourly check.sh arg1", content);
    }

    [Fact]
    public void ClearCrontab_DeletesFile()
    {
        // Arrange
        File.WriteAllText(_testCrontabPath, "0 0 * * * test.sh");

        // Act
        _service.ClearCrontab();

        // Assert
        Assert.False(File.Exists(_testCrontabPath));
    }

    [Fact]
    public void ClearCrontab_NonExistentFile_DoesNotThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.txt");
        var service = new TestCrontabService(nonExistentPath);

        // Act & Assert
        var exception = Record.Exception(() => service.ClearCrontab());
        Assert.Null(exception);
    }

    #endregion
}

/// <summary>
/// Helper class to access private methods of CrontabService for testing
/// </summary>
public class CrontabServiceAccessor
{
    public CrontabEntry? ParseCrontabLine(string line)
    {
        // Re-implement the parsing logic for testing
        var parts = SplitCrontabLine(line);

        if (parts.Length < 2)
        {
            return null;
        }

        var isSpecialSchedule = parts[0].StartsWith('@');

        string schedule;
        string command;
        string arguments;

        if (isSpecialSchedule)
        {
            schedule = parts[0];
            command = parts[1];
            arguments = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : string.Empty;
        }
        else
        {
            if (parts.Length < 6)
            {
                return null;
            }

            schedule = string.Join(" ", parts.Take(5));
            command = parts[5];
            arguments = parts.Length > 6 ? string.Join(" ", parts.Skip(6)) : string.Empty;
        }

        var taskName = GenerateTaskName(schedule, command, arguments);

        return new CrontabEntry
        {
            TaskName = taskName,
            Schedule = schedule,
            Command = command,
            Arguments = arguments,
            OriginalLine = line
        };
    }

    private string[] SplitCrontabLine(string line)
    {
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts.ToArray();
    }

    private string GenerateTaskName(string schedule, string command, string arguments)
    {
        var content = $"{schedule}|{command}|{arguments}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content));
        var hashString = Convert.ToHexString(hash).Substring(0, 8);

        var commandName = Path.GetFileNameWithoutExtension(command);
        return $"cron-{commandName}-{hashString}".Replace(" ", "-");
    }
}
