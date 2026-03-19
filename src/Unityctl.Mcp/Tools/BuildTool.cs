using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class BuildTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_build")]
    [Description("Build project for target platform")]
    public async Task<string> BuildAsync(
        [Description("Unity project path")] string project,
        [Description("Build target (StandaloneWindows64, OSX, Linux, Android, iOS, WebGL)")] string target = "StandaloneWindows64",
        [Description("Output path for build artifacts")] string? output = null,
        [Description("Validate without building (preflight check only)")] bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var parameters = new JsonObject
        {
            ["target"] = target,
            ["outputPath"] = output
        };
        if (dryRun) parameters["dryRun"] = true;

        var request = new CommandRequest
        {
            Command = WellKnownCommands.Build,
            Parameters = parameters
        };
        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
