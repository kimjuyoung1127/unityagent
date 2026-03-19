using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class TestTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_test")]
    [Description("Run Unity tests (edit/play mode)")]
    public async Task<string> TestAsync(
        [Description("Unity project path")] string project,
        [Description("Test mode: edit or play (default: edit)")] string mode = "edit",
        [Description("Test name filter expression")] string? filter = null,
        [Description("Wait for test completion (default: true)")] bool wait = true,
        [Description("Timeout in seconds when waiting (default: 300)")] int timeout = 300,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["mode"] = mode,
            ["wait"] = wait,
            ["timeout"] = timeout
        };
        if (filter != null) parameters["filter"] = filter;

        var request = new CommandRequest
        {
            Command = WellKnownCommands.Test,
            Parameters = parameters
        };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
