using System.Text.Json;
using Unityctl.Cli.Commands;
using Unityctl.Core.FlightRecorder;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class LogCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FlightLog _log;

    public LogCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-cli-test-{Guid.NewGuid():N}");
        _log = new FlightLog(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static FlightEntry MakeEntry(string op = "build", string level = "info") => new()
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Operation = op,
        Level = level,
        StatusCode = 0,
        DurationMs = 123,
        V = "0.2.0",
        Machine = "test"
    };

    private static string CaptureConsole(Action action)
    {
        var prev = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            action();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(prev);
        }
    }

    // ─── stats ────────────────────────────────────────────────────────────────

    [Fact]
    public void Stats_EmptyLog_PrintsZeroes()
    {
        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, stats: true));

        Assert.Contains("files:", output);
        Assert.Contains("entries:", output);
        Assert.Contains("size:", output);
    }

    [Fact]
    public void Stats_WithEntries_PrintsCorrectCounts()
    {
        _log.Record(MakeEntry());
        _log.Record(MakeEntry("test"));

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, stats: true));

        Assert.Contains("files:", output);
        Assert.Contains("entries:", output);
        // Should show 2 entries somewhere in output
        Assert.Contains("2", output);
    }

    // ─── prune ────────────────────────────────────────────────────────────────

    [Fact]
    public void Prune_PrintsResult()
    {
        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, prune: true));

        Assert.Contains("Pruned", output);
        Assert.Contains("freed", output);
    }

    [Fact]
    public void Prune_WithOldFile_ReportsDeletedCount()
    {
        // Create a 31-day-old file manually
        Directory.CreateDirectory(_tempDir);
        var oldDate = DateTimeOffset.UtcNow.AddDays(-31).ToString("yyyy-MM-dd");
        var oldFile = Path.Combine(_tempDir, $"flight-{oldDate}.ndjson");
        File.WriteAllText(oldFile, "{\"op\":\"build\"}\n");

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, prune: true));

        Assert.Contains("1", output); // 1 file deleted
    }

    // ─── json output ──────────────────────────────────────────────────────────

    [Fact]
    public void Json_EmptyLog_OutputsEmptyArray()
    {
        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, json: true));

        var trimmed = output.Trim();
        Assert.StartsWith("[", trimmed);
        Assert.EndsWith("]", trimmed);

        var entries = JsonSerializer.Deserialize<FlightEntry[]>(trimmed);
        Assert.NotNull(entries);
        Assert.Empty(entries);
    }

    [Fact]
    public void Json_WithEntries_OutputsValidJsonArray()
    {
        _log.Record(MakeEntry("build"));
        _log.Record(MakeEntry("test"));

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, last: 10, json: true));

        var trimmed = output.Trim();
        Assert.StartsWith("[", trimmed);

        var entries = JsonSerializer.Deserialize<FlightEntry[]>(trimmed);
        Assert.NotNull(entries);
        Assert.Equal(2, entries!.Length);
    }

    [Fact]
    public void Json_EntryHasExpectedFields()
    {
        _log.Record(MakeEntry("ping", "info"));

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, last: 10, json: true));

        Assert.Contains("\"op\"", output);
        Assert.Contains("\"ping\"", output);
        Assert.Contains("\"level\"", output);
    }

    // ─── default query ────────────────────────────────────────────────────────

    [Fact]
    public void Default_EmptyLog_PrintsNoEntriesMessage()
    {
        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log));

        Assert.Contains("No log entries found", output);
    }

    [Fact]
    public void Default_WithEntries_PrintsTable()
    {
        _log.Record(MakeEntry("build"));

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, last: 10));

        Assert.Contains("build", output);
    }

    [Fact]
    public void FilterByOp_OnlyShowsMatchingEntries()
    {
        _log.Record(MakeEntry("build"));
        _log.Record(MakeEntry("test"));
        _log.Record(MakeEntry("ping"));

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, op: "build", last: 10));

        Assert.Contains("build", output);
        Assert.DoesNotContain("test", output);
        Assert.DoesNotContain("ping", output);
    }

    [Fact]
    public void FilterByLevel_OnlyShowsMatchingEntries()
    {
        _log.Record(MakeEntry("build", "info"));
        _log.Record(MakeEntry("test", "error"));

        var output = CaptureConsole(() =>
            LogCommand.ExecuteCore(_log, level: "error", last: 10));

        Assert.Contains("error", output);
    }
}
