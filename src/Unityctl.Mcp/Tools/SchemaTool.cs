using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class SchemaTool
{
    [McpServerTool(Name = "unityctl_schema")]
    [Description("Return the machine-readable JSON schema of unityctl commands (tool discovery). Optionally filter to a single command.")]
    public string Schema(
        [Description("Filter to a specific command name (optional, returns all if omitted)")] string? command = null)
    {
        if (!string.IsNullOrWhiteSpace(command))
        {
            var matched = CommandCatalog.All
                .FirstOrDefault(c => c.Name.Equals(command, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                var errorObj = new System.Text.Json.Nodes.JsonObject { ["error"] = $"Unknown command: '{command}'" };
                return errorObj.ToJsonString();
            }

            return JsonSerializer.Serialize(matched, UnityctlJsonContext.Default.CommandDefinition);
        }

        var schema = new CommandSchema
        {
            Version = Unityctl.Shared.Constants.Version,
            Commands = CommandCatalog.All
        };
        return JsonSerializer.Serialize(schema, UnityctlJsonContext.Default.CommandSchema);
    }
}
