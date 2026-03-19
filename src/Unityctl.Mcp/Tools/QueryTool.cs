using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class QueryTool(CommandExecutor executor)
{
    private static readonly HashSet<string> Allowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        // Exploration (was ExploreTool)
        WellKnownCommands.AssetFind,
        WellKnownCommands.AssetGetInfo,
        WellKnownCommands.AssetGetDependencies,
        WellKnownCommands.AssetReferenceGraph,
        WellKnownCommands.AssetGetLabels,
        WellKnownCommands.BuildSettingsGetScenes,
        WellKnownCommands.GameObjectFind,
        WellKnownCommands.GameObjectGet,
        WellKnownCommands.ComponentGet,
        WellKnownCommands.TagList,
        WellKnownCommands.LayerList,
        WellKnownCommands.ConsoleGetCount,
        WellKnownCommands.DefineSymbolsGet,
        WellKnownCommands.LightingGetSettings,
        WellKnownCommands.NavMeshGetSettings,
        WellKnownCommands.ScriptList,
        WellKnownCommands.PhysicsGetSettings,
        WellKnownCommands.PhysicsGetCollisionMatrix,
        // Scene (was SceneTool)
        WellKnownCommands.SceneSnapshot,
        WellKnownCommands.SceneHierarchy,
        WellKnownCommands.SceneDiff,
        // Screenshot (was ScreenshotTool)
        WellKnownCommands.Screenshot,
        // Additional reads
        WellKnownCommands.BuildProfileList,
        WellKnownCommands.BuildProfileGetActive,
        WellKnownCommands.PackageList,
        WellKnownCommands.ProjectSettingsGet,
        WellKnownCommands.MaterialGet
    };

    [McpServerTool(Name = "unityctl_query")]
    [Description("Query Unity Editor state (use unityctl_schema for params)")]
    public async Task<string> QueryAsync(
        [Description("Unity project path")] string project,
        [Description("Query command (e.g. asset-find, gameobject-find, scene-hierarchy)")] string command,
        [Description("Command parameters as JSON object")] string? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return JsonSerializer.Serialize(
                CommandResponse.Fail(StatusCode.InvalidParameters, "command must not be empty"),
                UnityctlJsonContext.Default.CommandResponse);

        if (!Allowlist.Contains(command))
            return JsonSerializer.Serialize(
                CommandResponse.Fail(
                    StatusCode.CommandNotFound,
                    $"Command '{command}' is not in the query allowlist. Allowed: {string.Join(", ", Allowlist.Order())}"),
                UnityctlJsonContext.Default.CommandResponse);

        JsonObject? parsedParameters = null;
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            try
            {
                var node = JsonNode.Parse(parameters);
                parsedParameters = node?.AsObject();
                if (parsedParameters is null)
                    return JsonSerializer.Serialize(
                        CommandResponse.Fail(StatusCode.InvalidParameters, "parameters must be a JSON object"),
                        UnityctlJsonContext.Default.CommandResponse);
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(
                    CommandResponse.Fail(StatusCode.InvalidParameters, $"Invalid JSON in parameters: {ex.Message}"),
                    UnityctlJsonContext.Default.CommandResponse);
            }
        }

        var request = new CommandRequest
        {
            Command = command,
            Parameters = parsedParameters
        };

        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
