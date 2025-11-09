using Crontab.Services;

namespace Crontab.Tests;

/// <summary>
/// Test wrapper for CrontabService that allows us to specify a custom path
/// </summary>
public class TestCrontabService : ICrontabService
{
    private readonly string _testPath;
    private readonly CrontabServiceAccessor _accessor;

    public TestCrontabService(string testPath)
    {
        _testPath = testPath;
        _accessor = new CrontabServiceAccessor();
    }

    public string GetCrontabFilePath() => _testPath;

    public IEnumerable<CrontabEntry> ReadCrontab()
    {
        if (!File.Exists(_testPath))
        {
            return Enumerable.Empty<CrontabEntry>();
        }

        var entries = new List<CrontabEntry>();
        var lines = File.ReadAllLines(_testPath);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var entry = _accessor.ParseCrontabLine(trimmed);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    public void WriteCrontab(IEnumerable<CrontabEntry> entries)
    {
        var lines = new List<string>
        {
            "# Crontab file for Windows Task Scheduler",
            "# Format: minute hour day month day-of-week command [arguments...]",
            ""
        };

        foreach (var entry in entries)
        {
            lines.Add($"{entry.Schedule} {entry.Command} {entry.Arguments}".Trim());
        }

        File.WriteAllLines(_testPath, lines);
    }

    public void ClearCrontab()
    {
        if (File.Exists(_testPath))
        {
            File.Delete(_testPath);
        }
    }

    public string GetCrontabContent()
    {
        if (!File.Exists(_testPath))
        {
            return string.Empty;
        }

        return File.ReadAllText(_testPath);
    }

    public void OpenEditor()
    {
        throw new NotImplementedException("Editor not supported in tests");
    }
}
