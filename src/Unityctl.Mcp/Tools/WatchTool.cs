using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class WatchTool(CommandExecutor executor)
{
    /// <summary>
    /// MCP does not support true streaming push. This tool collects events
    /// for a fixed window and returns them as a JSON batch.
    /// </summary>
    [McpServerTool(Name = "unityctl_watch")]
    [Description("Collect real-time editor events (poll mode)")]
    public async Task<string> WatchAsync(
        [Description("Unity project path")] string project,
        [Description("Event channel: console, hierarchy, compilation, all (default: all)")] string channel = "all",
        [Description("Collection window in seconds (default: 5)")] int windowSeconds = 5,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(windowSeconds));

        var events = new List<EventEnvelope>();
        try
        {
            var stream = executor.WatchAsync(project, channel, cts.Token);
            if (stream == null)
                return JsonSerializer.Serialize(events, UnityctlJsonContext.Default.EventEnvelopeArray);

            await foreach (var evt in stream.WithCancellation(cts.Token))
                events.Add(evt);
        }
        catch (OperationCanceledException)
        {
            // Window expired — return what we collected
        }

        return JsonSerializer.Serialize(events, UnityctlJsonContext.Default.EventEnvelopeArray);
    }
}
