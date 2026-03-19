using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class CheckTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_check")]
    [Description("Check script compilation status")]
    public async Task<string> CheckAsync(
        [Description("Unity project path")] string project,
        [Description("Check type (default: compile)")] string type = "compile",
        CancellationToken cancellationToken = default)
    {
        var request = new CommandRequest
        {
            Command = WellKnownCommands.Check,
            Parameters = new JsonObject { ["type"] = type }
        };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
