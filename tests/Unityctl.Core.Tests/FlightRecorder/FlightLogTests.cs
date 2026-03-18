using System.Text.Json;
using Unityctl.Core.FlightRecorder;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.FlightRecorder;

public sealed class FlightLogTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FlightLog _log;

    public FlightLogTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-test-{Guid.NewGuid():N}");
        _log = new FlightLog(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static FlightEntry MakeEntry(
        string op = "build",
        string level = "info",
        string? project = null,
        long? timestampMs = null)
        => new()
        {
            Timestamp = timestampMs ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Operation = op,
            Level = level,
            Project = project,
            StatusCode = 0,
            DurationMs = 100,
            V = "0.2.0",
            Machine = "test-machine"
        };

    // ─── Record ───────────────────────────────────────────────────────────────

    [Fact]
    public void Record_CreatesFileWithCorrectName()
    {
        _log.Record(MakeEntry());

        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var expected = Path.Combine(_tempDir, $"flight-{today}.ndjson");
        Assert.True(File.Exists(expected));
    }

    [Fact]
    public void Record_AppendsValidNdjson()
    {
        _log.Record(MakeEntry("build"));
        _log.Record(MakeEntry("test"));
        _log.Record(MakeEntry("ping"));

        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");
        var lines = File.ReadAllLines(filePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        Assert.Equal(3, lines.Length);
        foreach (var line in lines)
        {
            var entry = JsonSerializer.Deserialize<FlightEntry>(line);
            Assert.NotNull(entry);
        }
    }

    [Fact]
    public void Record_NeverThrows_WhenPathIsFile()
    {
        // Point log dir at an existing FILE so CreateDirectory will fail
        var tempFile = Path.GetTempFileName();
        try
        {
            var badLog = new FlightLog(tempFile);
            badLog.Record(MakeEntry()); // must not throw
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public async Task ConcurrentRecord_NoCorruption()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                for (var i = 0; i < 10; i++)
                    _log.Record(MakeEntry());
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_tempDir, $"flight-{today}.ndjson");
        var lines = File.ReadAllLines(filePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        Assert.Equal(100, lines.Length);
        foreach (var line in lines)
        {
            var entry = JsonSerializer.Deserialize<FlightEntry>(line);
            Assert.NotNull(entry);
        }
    }

    // ─── Query ────────────────────────────────────────────────────────────────

    [Fact]
    public void Query_FilterByOp()
    {
        _log.Record(MakeEntry("build"));
        _log.Record(MakeEntry("test"));
        _log.Record(MakeEntry("build"));

        var results = _log.Query(new FlightQuery { Op = "build", Last = 10 });

        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("build", e.Operation));
    }

    [Fact]
    public void Query_FilterByLevel()
    {
        _log.Record(MakeEntry("build", "info"));
        _log.Record(MakeEntry("test", "error"));
        _log.Record(MakeEntry("ping", "info"));

        var results = _log.Query(new FlightQuery { Level = "error", Last = 10 });

        Assert.Single(results);
        Assert.Equal("error", results[0].Level);
    }

    [Fact]
    public void Query_Last_LimitsResults()
    {
        for (var i = 0; i < 10; i++)
            _log.Record(MakeEntry());

        var results = _log.Query(new FlightQuery { Last = 3 });

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Query_DefaultLast_Returns20()
    {
        for (var i = 0; i < 25; i++)
            _log.Record(MakeEntry());

        var results = _log.Query(new FlightQuery());

        Assert.Equal(20, results.Count);
    }

    [Fact]
    public void Query_ReturnsNewestFirst()
    {
        var now = DateTimeOffset.UtcNow;
        _log.Record(MakeEntry("first", timestampMs: now.AddMinutes(-5).ToUnixTimeMilliseconds()));
        _log.Record(MakeEntry("second", timestampMs: now.ToUnixTimeMilliseconds()));

        var results = _log.Query(new FlightQuery { Last = 10 });

        Assert.Equal(2, results.Count);
        // Newest (second) should come before oldest (first)
        Assert.Equal("second", results[0].Operation);
        Assert.Equal("first", results[1].Operation);
    }

    [Fact]
    public void Query_FilterBySince()
    {
        var now = DateTimeOffset.UtcNow;
        _log.Record(MakeEntry("old", timestampMs: now.AddHours(-2).ToUnixTimeMilliseconds()));
        _log.Record(MakeEntry("new", timestampMs: now.ToUnixTimeMilliseconds()));

        var results = _log.Query(new FlightQuery
        {
            Since = now.AddHours(-1),
            Last = 10
        });

        Assert.Single(results);
        Assert.Equal("new", results[0].Operation);
    }

    [Fact]
    public void Query_EmptyDirectory_ReturnsEmpty()
    {
        var results = _log.Query(new FlightQuery { Last = 10 });
        Assert.Empty(results);
    }

    // ─── Prune ────────────────────────────────────────────────────────────────

    [Fact]
    public void Prune_RemovesOldFiles()
    {
        // Manually create a file named 31 days ago
        var oldDate = DateTimeOffset.UtcNow.AddDays(-31).ToString("yyyy-MM-dd");
        var oldFile = Path.Combine(_tempDir, $"flight-{oldDate}.ndjson");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(oldFile, JsonSerializer.Serialize(MakeEntry()) + "\n");

        // Today's file should survive
        _log.Record(MakeEntry());

        var result = _log.Prune();

        Assert.Equal(1, result.DeletedFiles);
        Assert.True(result.FreedBytes > 0);
        Assert.False(File.Exists(oldFile));
    }

    [Fact]
    public void Prune_EmptyDirectory_ReturnsZero()
    {
        var result = _log.Prune();

        Assert.Equal(0, result.DeletedFiles);
        Assert.Equal(0, result.FreedBytes);
    }

    [Fact]
    public void Prune_RecentFile_IsNotDeleted()
    {
        _log.Record(MakeEntry());

        var result = _log.Prune();

        Assert.Equal(0, result.DeletedFiles);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        Assert.True(File.Exists(Path.Combine(_tempDir, $"flight-{today}.ndjson")));
    }

    // ─── GetStats ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetStats_ReturnsCorrectCounts()
    {
        _log.Record(MakeEntry("build"));
        _log.Record(MakeEntry("test"));
        _log.Record(MakeEntry("ping"));

        var stats = _log.GetStats();

        Assert.Equal(1, stats.FileCount);
        Assert.Equal(3, stats.EntryCount);
        Assert.True(stats.TotalBytes > 0);
        Assert.NotNull(stats.NewestDate);
        Assert.NotNull(stats.OldestDate);
    }

    [Fact]
    public void GetStats_EmptyDirectory_ReturnsZeroes()
    {
        var stats = _log.GetStats();

        Assert.Equal(0, stats.FileCount);
        Assert.Equal(0, stats.EntryCount);
        Assert.Equal(0L, stats.TotalBytes);
        Assert.Null(stats.OldestDate);
        Assert.Null(stats.NewestDate);
    }

    [Fact]
    public void GetStats_MultipleFiles_CountsAll()
    {
        // Create two files for different dates
        Directory.CreateDirectory(_tempDir);
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        var line = JsonSerializer.Serialize(MakeEntry()) + "\n";
        File.WriteAllText(Path.Combine(_tempDir, $"flight-{yesterday}.ndjson"), line + line);
        File.WriteAllText(Path.Combine(_tempDir, $"flight-{today}.ndjson"), line);

        var stats = _log.GetStats();

        Assert.Equal(2, stats.FileCount);
        Assert.Equal(3, stats.EntryCount);
        Assert.Equal(yesterday, stats.OldestDate);
        Assert.Equal(today, stats.NewestDate);
    }
}
