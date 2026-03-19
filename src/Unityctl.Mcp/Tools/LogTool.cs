using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.FlightRecorder;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class LogTool
{
    [McpServerTool(Name = "unityctl_log")]
    [Description("Query flight recorder logs")]
    public string Log(
        [Description("Number of recent entries to show (default: 20)")] int last = 20,
        [Description("Filter by operation name (build, test, check, etc.)")] string? op = null,
        [Description("Filter by level (info, warn, error)")] string? level = null)
    {
        var flightLog = new FlightLog();
        var query = new FlightQuery
        {
            Last = last,
            Op = op,
            Level = level
        };
        var entries = flightLog.Query(query);
        return JsonSerializer.Serialize(entries, UnityctlJsonContext.Default.FlightEntryArray);
    }
}
