using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PrefabCommand
{
    public static void Create(string project, string target, string path, bool json = false)
    {
        var request = CreateCreateRequest(target, path);
        CommandRunner.Execute(project, request, json);
    }

    public static void Unpack(string project, string id, string mode = "outermost", bool json = false)
    {
        var request = CreateUnpackRequest(id, mode);
        CommandRunner.Execute(project, request, json);
    }

    public static void Apply(string project, string id, bool json = false)
    {
        var request = CreateApplyRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    public static void Edit(string project, string path, string property, string value, string? childPath = null, bool json = false)
    {
        var request = CreateEditRequest(path, property, value, childPath);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateRequest(string target, string path)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("target must not be empty", nameof(target));
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.PrefabCreate,
            Parameters = new JsonObject
            {
                ["target"] = target,
                ["path"] = path
            }
        };
    }

    internal static CommandRequest CreateUnpackRequest(string id, string mode = "outermost")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        var parameters = new JsonObject { ["id"] = id };
        if (!string.IsNullOrEmpty(mode)) parameters["mode"] = mode;

        return new CommandRequest
        {
            Command = WellKnownCommands.PrefabUnpack,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateApplyRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.PrefabApply,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateEditRequest(string path, string property, string value, string? childPath)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var parameters = new JsonObject
        {
            ["path"] = path,
            ["property"] = property,
            ["value"] = value
        };
        if (!string.IsNullOrEmpty(childPath)) parameters["childPath"] = childPath;

        return new CommandRequest
        {
            Command = WellKnownCommands.PrefabEdit,
            Parameters = parameters
        };
    }
}
