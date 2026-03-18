using System.Text.Json;
using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

public static class WatchCommand
{
    public static void Execute(
        string project,
        string channel = "all",
        string format = "text",
        bool noColor = false)
    {
        var exitCode = ExecuteAsync(project, channel, format, noColor).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteAsync(
        string project,
        string channel,
        string format,
        bool noColor)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var ct = cts.Token;
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        int backoffMs = 1000;
        const int MaxBackoffMs = 30_000;

        while (!ct.IsCancellationRequested)
        {
            var events = executor.WatchAsync(project, channel, ct);
            if (events == null)
            {
                Console.Error.WriteLine("[unityctl] Watch requires a running Unity Editor with IPC.");
                return 1;
            }

            try
            {
                Console.Error.WriteLine($"[unityctl] Connecting to '{channel}' stream...");
                bool connected = false;

                await foreach (var evt in events.WithCancellation(ct))
                {
                    if (!connected)
                    {
                        Console.Error.WriteLine(
                            $"[unityctl] Connected — streaming '{channel}' events. Press Ctrl+C to stop.");
                        connected = true;
                        backoffMs = 1000; // reset backoff on successful connection
                    }

                    PrintEvent(evt, format, noColor);
                }

                // IAsyncEnumerable completed normally (server closed or disconnected)
                if (ct.IsCancellationRequested) break;

                Console.Error.WriteLine(
                    $"[unityctl] Disconnected — reconnecting in {backoffMs / 1000}s...");
                await Task.Delay(backoffMs, ct).ConfigureAwait(false);
                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested) break;
                Console.Error.WriteLine(
                    $"[unityctl] Connection error: {ex.Message} — reconnecting in {backoffMs / 1000}s...");
                try
                {
                    await Task.Delay(backoffMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                backoffMs = Math.Min(backoffMs * 2, MaxBackoffMs);
            }
        }

        Console.Error.WriteLine("[unityctl] Watch stopped.");
        return 0;
    }

    /// <summary>
    /// Formats and writes a single event to stdout.
    /// Exposed as internal for unit testing.
    /// </summary>
    internal static void PrintEvent(EventEnvelope evt, string format, bool noColor)
    {
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(evt, UnityctlJsonContext.Default.EventEnvelope));
            return;
        }

        // Text mode: [HH:mm:ss.fff] [channel/eventType]  message
        var ts = FormatTimestamp(evt.Timestamp);
        var tag = $"[{evt.Channel}/{evt.EventType}]";
        var message = ExtractMessage(evt);

        if (noColor)
        {
            Console.WriteLine($"{ts} {tag,-30} {message}");
        }
        else
        {
            var color = GetSpectreColor(evt);
            var console = ConsoleOutput.CreateOut();
            var escapedTag = Markup.Escape(tag);
            var escapedMsg = Markup.Escape(message);
            console.MarkupLine($"[dim]{Markup.Escape(ts)}[/] [{color}]{escapedTag,-30}[/] {escapedMsg}");
        }
    }

    /// <summary>Returns the console color for the given event. Exposed for testing.</summary>
    internal static ConsoleColor GetEventColor(EventEnvelope evt)
    {
        if (evt.Channel.StartsWith("_", StringComparison.Ordinal))
            return ConsoleColor.DarkGray;

        return evt.EventType switch
        {
            "Error" or "Exception" or "Assert" => ConsoleColor.Red,
            "Warning" => ConsoleColor.Yellow,
            _ => ConsoleColor.White
        };
    }

    /// <summary>
    /// Extracts the human-readable message from an event.
    /// Tries payload["message"] first, falls back to EventType.
    /// Exposed for testing.
    /// </summary>
    internal static string ExtractMessage(EventEnvelope evt)
    {
        if (evt.Payload?.TryGetPropertyValue("message", out var node) == true)
            return node?.GetValue<string>() ?? evt.EventType;

        return evt.EventType;
    }

    private static string GetSpectreColor(EventEnvelope evt)
    {
        if (evt.Channel.StartsWith("_", StringComparison.Ordinal))
            return "grey";

        return evt.EventType switch
        {
            "Error" or "Exception" or "Assert" => "red",
            "Warning" => "yellow",
            _ => "white"
        };
    }

    private static string FormatTimestamp(long unixMs)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixMs)
            .ToLocalTime()
            .ToString("HH:mm:ss.fff");
    }
}
