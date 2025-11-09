using Xunit;
using Crontab.Services;
using Shouldly;

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
        entry.TaskName.ShouldBe("test-task");
        entry.Schedule.ShouldBe("0 0 * * *");
        entry.Command.ShouldBe("backup.sh");
        entry.Arguments.ShouldBe("arg1 arg2");
        entry.OriginalLine.ShouldBe("0 0 * * * backup.sh arg1 arg2");
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
        taskInfo.Name.ShouldBe("TestTask");
        taskInfo.Path.ShouldBe("\\Crontab\\TestTask");
        taskInfo.Enabled.ShouldBeTrue();
        taskInfo.State.ShouldBe("Ready");
        taskInfo.Description.ShouldNotBeNull();
    }
}
