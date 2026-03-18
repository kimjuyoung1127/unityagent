using System.Text.Json;
using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Core.FlightRecorder;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

public static class LogCommand
{
    public static void Execute(
        int? last = null,
        bool tail = false,
        string? op = null,
        string? level = null,
        string? since = null,
        bool json = false,
        bool prune = false,
        bool stats = false)
    {
        ExecuteCore(new FlightLog(), last, tail, op, level, since, json, prune, stats);
    }

    internal static void ExecuteCore(
        FlightLog log,
        int? last = null,
        bool tail = false,
        string? op = null,
        string? level = null,
        string? since = null,
        bool json = false,
        bool prune = false,
        bool stats = false)
    {
        if (stats)
        {
            PrintStats(log);
            return;
        }

        if (prune)
        {
            var pruneResult = log.Prune();
            Console.WriteLine($"Pruned {pruneResult.DeletedFiles} file(s), freed {pruneResult.FreedBytes:N0} bytes.");
            return;
        }

        if (tail)
        {
            TailLog(log, last ?? 20);
            return;
        }

        DateTimeOffset? sinceDate = null;
        if (since != null && DateTimeOffset.TryParse(since, out var parsedSince))
            sinceDate = parsedSince;

        var query = new FlightQuery
        {
            Op = op,
            Level = level,
            Since = sinceDate,
            Last = last ?? 20
        };

        var entries = log.Query(query);

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                entries.ToArray(),
                UnityctlJsonContext.Default.FlightEntryArray));
            return;
        }

        if (entries.Count == 0)
        {
            Console.WriteLine("No log entries found.");
            return;
        }

        var console = ConsoleOutput.CreateOut();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Timestamp")
            .AddColumn("Operation")
            .AddColumn("Level")
            .AddColumn("Code")
            .AddColumn(new TableColumn("Duration").RightAligned())
            .AddColumn("Project");

        foreach (var entry in entries)
            AddEntryRow(table, entry);

        console.Write(table);
    }

    private static void PrintStats(FlightLog log)
    {
        var s = log.GetStats();
        var console = ConsoleOutput.CreateOut();

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn();

        grid.AddRow("[grey]files:[/]", $"{s.FileCount}");
        grid.AddRow("[grey]entries:[/]", $"{s.EntryCount}");
        grid.AddRow("[grey]size:[/]", $"{s.TotalBytes:N0} bytes");
        if (s.OldestDate != null) grid.AddRow("[grey]oldest:[/]", s.OldestDate);
        if (s.NewestDate != null) grid.AddRow("[grey]newest:[/]", s.NewestDate);

        console.Write(grid);
    }

    private static void TailLog(FlightLog log, int initialLines)
    {
        var console = ConsoleOutput.CreateOut();

        // Print initial entries in chronological order (oldest first)
        var entries = log.Query(new FlightQuery { Last = initialLines });
        if (entries.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Timestamp")
                .AddColumn("Operation")
                .AddColumn("Level")
                .AddColumn("Code")
                .AddColumn(new TableColumn("Duration").RightAligned())
                .AddColumn("Project");

            for (var i = entries.Count - 1; i >= 0; i--)
                AddEntryRow(table, entries[i]);

            console.Write(table);
        }

        var lastTs = entries.Count > 0
            ? entries[0].Timestamp
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancel;

        void OnCancel(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cts.Cancel();
        }

        Console.Error.WriteLine("[Watching for new log entries. Press Ctrl+C to stop.]");
        try
        {
            while (!cts.IsCancellationRequested)
            {
                cts.Token.WaitHandle.WaitOne(1000);
                if (cts.IsCancellationRequested) break;

                var since = DateTimeOffset.FromUnixTimeMilliseconds(lastTs + 1);
                var newEntries = log.Query(new FlightQuery { Since = since, Last = 100 });

                // Print in chronological order
                for (var i = newEntries.Count - 1; i >= 0; i--)
                {
                    PrintEntry(newEntries[i]);
                    if (newEntries[i].Timestamp > lastTs)
                        lastTs = newEntries[i].Timestamp;
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= OnCancel;
        }
    }

    private static void AddEntryRow(Table table, Unityctl.Shared.Protocol.FlightEntry entry)
    {
        var ts = DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss");
        var levelColor = entry.Level switch
        {
            "error" => "red",
            "warn" => "yellow",
            _ => "green"
        };

        table.AddRow(
            new Text(ts),
            new Markup($"[cyan]{Markup.Escape(entry.Operation ?? "")}[/]"),
            new Markup($"[{levelColor}]{Markup.Escape(entry.Level ?? "")}[/]"),
            new Text($"{entry.StatusCode}"),
            new Text($"{entry.DurationMs}ms"),
            new Text(entry.Project ?? ""));
    }

    private static void PrintEntry(Unityctl.Shared.Protocol.FlightEntry entry)
    {
        var ts = DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine(
            $"{ts}  {entry.Operation,-10}  {entry.Level,-5}  {entry.StatusCode}  {entry.DurationMs,6}ms  {entry.Project}");
    }
}
