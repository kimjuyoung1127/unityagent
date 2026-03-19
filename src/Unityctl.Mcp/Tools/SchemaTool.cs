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
    [Description("Command schema discovery (filter by command or category)")]
    public string Schema(
        [Description("Filter to a specific command name")] string? command = null,
        [Description("Filter by category: query, action, meta, setup, discovery")] string? category = null)
    {
        if (!string.IsNullOrWhiteSpace(command))
        {
            var matched = CommandCatalog.All
                .FirstOrDefault(c => c.Name.Equals(command, StringComparison.OrdinalIgnoreCase)
                    || (c.CliName != null && c.CliName.Equals(command, StringComparison.OrdinalIgnoreCase)));

            if (matched is null)
            {
                var errorObj = new System.Text.Json.Nodes.JsonObject { ["error"] = $"Unknown command: '{command}'" };
                return errorObj.ToJsonString();
            }

            return JsonSerializer.Serialize(matched, UnityctlJsonContext.Default.CommandDefinition);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var filtered = CommandCatalog.All
                .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (filtered.Length == 0)
            {
                var errorObj = new System.Text.Json.Nodes.JsonObject { ["error"] = $"Unknown category: '{category}'. Valid: query, action, meta, setup, discovery" };
                return errorObj.ToJsonString();
            }

            var filteredSchema = new CommandSchema
            {
                Version = Unityctl.Shared.Constants.Version,
                Commands = filtered
            };
            return JsonSerializer.Serialize(filteredSchema, UnityctlJsonContext.Default.CommandSchema);
        }

        var schema = new CommandSchema
        {
            Version = Unityctl.Shared.Constants.Version,
            Commands = CommandCatalog.All
        };
        return JsonSerializer.Serialize(schema, UnityctlJsonContext.Default.CommandSchema);
    }
}
