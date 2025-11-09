using Xunit;
using Microsoft.Win32.TaskScheduler;
using Crontab.Services;
using System.Reflection;

namespace Crontab.Tests;

public class TaskSchedulerServiceTests
{
    [Theory]
    [InlineData("@hourly", typeof(DailyTrigger))]
    [InlineData("hourly", typeof(DailyTrigger))]
    [InlineData("@daily", typeof(DailyTrigger))]
    [InlineData("daily", typeof(DailyTrigger))]
    [InlineData("@midnight", typeof(DailyTrigger))]
    [InlineData("@weekly", typeof(WeeklyTrigger))]
    [InlineData("weekly", typeof(WeeklyTrigger))]
    [InlineData("@monthly", typeof(MonthlyTrigger))]
    [InlineData("monthly", typeof(MonthlyTrigger))]
    [InlineData("@yearly", typeof(MonthlyTrigger))]
    [InlineData("@annually", typeof(MonthlyTrigger))]
    [InlineData("@reboot", typeof(BootTrigger))]
    [InlineData("boot", typeof(BootTrigger))]
    [InlineData("logon", typeof(LogonTrigger))]
    public void ParseSchedule_SpecialSchedules_ReturnsCorrectTriggerType(string schedule, Type expectedTriggerType)
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule(schedule);

        // Assert
        Assert.NotNull(trigger);
        Assert.IsType(expectedTriggerType, trigger);
    }

    [Fact]
    public void ParseSchedule_Hourly_ConfiguresCorrectRepetition()
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule("@hourly");

        // Assert
        Assert.IsType<DailyTrigger>(trigger);
        var dailyTrigger = (DailyTrigger)trigger;
        Assert.NotNull(dailyTrigger.Repetition);
        Assert.Equal(TimeSpan.FromHours(1), dailyTrigger.Repetition.Interval);
    }

    [Fact]
    public void ParseSchedule_Daily_ConfiguresCorrectInterval()
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule("@daily");

        // Assert
        Assert.IsType<DailyTrigger>(trigger);
        var dailyTrigger = (DailyTrigger)trigger;
        Assert.Equal(1, dailyTrigger.DaysInterval);
    }

    [Fact]
    public void ParseSchedule_Weekly_ConfiguresCorrectInterval()
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule("@weekly");

        // Assert
        Assert.IsType<WeeklyTrigger>(trigger);
        var weeklyTrigger = (WeeklyTrigger)trigger;
        Assert.Equal(1, weeklyTrigger.WeeksInterval);
    }

    [Fact]
    public void ParseSchedule_Monthly_ConfiguresCorrectSettings()
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule("@monthly");

        // Assert
        Assert.IsType<MonthlyTrigger>(trigger);
        var monthlyTrigger = (MonthlyTrigger)trigger;
        Assert.Equal(MonthsOfTheYear.AllMonths, monthlyTrigger.MonthsOfYear);
        Assert.Contains(1, monthlyTrigger.DaysOfMonth);
    }

    [Fact]
    public void ParseSchedule_Yearly_ConfiguresCorrectSettings()
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule("@yearly");

        // Assert
        Assert.IsType<MonthlyTrigger>(trigger);
        var monthlyTrigger = (MonthlyTrigger)trigger;
        Assert.Equal(MonthsOfTheYear.January, monthlyTrigger.MonthsOfYear);
        Assert.Contains(1, monthlyTrigger.DaysOfMonth);
    }

    [Fact]
    public void ParseSchedule_Annually_ConfiguresCorrectSettings()
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule("@annually");

        // Assert
        Assert.IsType<MonthlyTrigger>(trigger);
        var monthlyTrigger = (MonthlyTrigger)trigger;
        Assert.Equal(MonthsOfTheYear.January, monthlyTrigger.MonthsOfYear);
        Assert.Contains(1, monthlyTrigger.DaysOfMonth);
    }

    [Theory]
    [InlineData("0 0 * * *")]  // Midnight daily
    [InlineData("*/5 * * * *")]  // Every 5 minutes
    [InlineData("0 9-17 * * 1-5")]  // Workday hours
    [InlineData("30 2 1 * *")]  // First day of month
    [InlineData("0 0,12 * * *")]  // Twice daily
    public void ParseSchedule_StandardCronFormat_ParsesSuccessfully(string schedule)
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule(schedule);

        // Assert
        Assert.NotNull(trigger);
        // Standard cron formats should return a trigger from Trigger.FromCronFormat
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("notaschedule")]
    [InlineData("@invalid")]
    [InlineData("")]
    public void ParseSchedule_InvalidSchedule_ThrowsException(string schedule)
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeParseSchedule(schedule));
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Theory]
    [InlineData("@HOURLY", typeof(DailyTrigger))]
    [InlineData("DAILY", typeof(DailyTrigger))]
    [InlineData("@WeEkLy", typeof(WeeklyTrigger))]
    [InlineData("MONTHLY", typeof(MonthlyTrigger))]
    public void ParseSchedule_CaseInsensitive_ParsesCorrectly(string schedule, Type expectedTriggerType)
    {
        // Arrange & Act
        var trigger = InvokeParseSchedule(schedule);

        // Assert
        Assert.NotNull(trigger);
        Assert.IsType(expectedTriggerType, trigger);
    }

    /// <summary>
    /// Uses reflection to invoke the private ParseSchedule method for testing
    /// </summary>
    private Trigger InvokeParseSchedule(string schedule)
    {
        // Create a temporary TaskSchedulerService instance
        using var service = new TaskSchedulerService();

        // Get the private ParseSchedule method
        var parseScheduleMethod = typeof(TaskSchedulerService).GetMethod(
            "ParseSchedule",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (parseScheduleMethod == null)
        {
            throw new InvalidOperationException("Could not find ParseSchedule method");
        }

        // Invoke the method
        var result = parseScheduleMethod.Invoke(service, new object[] { schedule });

        if (result is not Trigger trigger)
        {
            throw new InvalidOperationException("ParseSchedule did not return a Trigger");
        }

        return trigger;
    }
}
