using System.Text.Json.Serialization;
using Unityctl.Shared.Protocol;

namespace Unityctl.Shared.Commands;

public static class CommandCatalog
{
    public static readonly CommandDefinition Init = Define(
        "init",
        "Install the unityctl plugin into a Unity project",
        "setup",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("source", "string", "Custom local plugin source path", required: false));

    public static readonly CommandDefinition EditorList = Define(
        "editor list",
        "Discover installed Unity editors",
        "discovery",
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Status = Define(
        WellKnownCommands.Status,
        "Check Unity editor and project status",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("wait", "bool", "Retry until editor responds", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Ping = Define(
        WellKnownCommands.Ping,
        "Verify unityctl connectivity to a Unity project",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Build = Define(
        WellKnownCommands.Build,
        "Build a Unity project for a target platform",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("target", "string", "Build target (StandaloneWindows64, OSX, Linux, Android, iOS, WebGL)", required: false),
        Parameter("output", "string", "Output path for build artifacts", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Test = Define(
        WellKnownCommands.Test,
        "Start Unity tests (EditMode or PlayMode)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("mode", "string", "Test mode: edit or play", required: false),
        Parameter("filter", "string", "Test name filter", required: false),
        Parameter("wait", "bool", "Wait for test completion (default: true, disabled for PlayMode)", required: false),
        Parameter("timeout", "int", "Timeout in seconds when waiting (default: 300)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Check = Define(
        WellKnownCommands.Check,
        "Check whether Unity scripts compiled successfully",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("type", "string", "Check type: compile", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Tools = Define(
        "tools",
        "List all available tools with parameters",
        "meta",
        Parameter("json", "bool", "Output as machine-readable JSON", required: false));

    public static readonly CommandDefinition Log = Define(
        "log",
        "Query and manage command execution logs",
        "query",
        Parameter("last", "int", "Show last N entries (default: 20)", required: false),
        Parameter("tail", "bool", "Follow log file in real-time", required: false),
        Parameter("op", "string", "Filter by operation (build, test, etc)", required: false),
        Parameter("level", "string", "Filter by level (info, warn, error)", required: false),
        Parameter("since", "string", "Filter since date/time (ISO 8601)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false),
        Parameter("prune", "bool", "Apply retention policy (30 days / 50 MB)", required: false),
        Parameter("stats", "bool", "Show log statistics", required: false));

    public static CommandDefinition[] All { get; } =
    [
        Init,
        EditorList,
        Ping,
        Status,
        Build,
        Test,
        Check,
        Tools,
        Log
    ];

    private static CommandDefinition Define(
        string name,
        string description,
        string category,
        params CommandParameterDefinition[] parameters)
    {
        return new CommandDefinition
        {
            Name = name,
            Description = description,
            Category = category,
            Parameters = parameters
        };
    }

    private static CommandParameterDefinition Parameter(
        string name,
        string type,
        string description,
        bool required)
    {
        return new CommandParameterDefinition
        {
            Name = name,
            Type = type,
            Description = description,
            Required = required
        };
    }
}

public sealed class CommandDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public CommandParameterDefinition[] Parameters { get; set; } = [];
}

public sealed class CommandParameterDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}
