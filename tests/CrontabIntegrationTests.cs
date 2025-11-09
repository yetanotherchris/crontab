using Xunit;
using Crontab.Services;

namespace Crontab.Tests;

public class CrontabIntegrationTests
{
    [Fact]
    public void CrontabEntry_Properties_SetCorrectly()
    {
        // Arrange
        var entry = new CrontabEntry
        {
            TaskName = "test-task",
            Schedule = "0 0 * * *",
            Command = "backup.sh",
            Arguments = "arg1 arg2",
            OriginalLine = "0 0 * * * backup.sh arg1 arg2"
        };

        // Assert
        Assert.Equal("test-task", entry.TaskName);
        Assert.Equal("0 0 * * *", entry.Schedule);
        Assert.Equal("backup.sh", entry.Command);
        Assert.Equal("arg1 arg2", entry.Arguments);
        Assert.Equal("0 0 * * * backup.sh arg1 arg2", entry.OriginalLine);
    }

    [Fact]
    public void TaskInfo_Properties_SetCorrectly()
    {
        // Arrange
        var taskInfo = new TaskInfo
        {
            Name = "TestTask",
            Path = "\\Crontab\\TestTask",
            Enabled = true,
            State = "Ready",
            LastRunTime = DateTime.Now.AddHours(-1),
            NextRunTime = DateTime.Now.AddHours(1),
            Description = "Test task description"
        };

        // Assert
        Assert.Equal("TestTask", taskInfo.Name);
        Assert.Equal("\\Crontab\\TestTask", taskInfo.Path);
        Assert.True(taskInfo.Enabled);
        Assert.Equal("Ready", taskInfo.State);
        Assert.NotNull(taskInfo.Description);
    }
}
