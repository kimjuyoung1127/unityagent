using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ScriptableObjectCommand
{
    public static void Find(string project, string? type = null, string? folder = null, int? limit = null, bool json = false)
    {
        var request = CreateFindRequest(type, folder, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string path, string? property = null, bool json = false)
    {
        var request = CreateGetRequest(path, property);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetProperty(string project, string path, string property, string value, bool json = false)
    {
        var request = CreateSetPropertyRequest(path, property, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateFindRequest(string? type = null, string? folder = null, int? limit = null)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(type)) parameters["type"] = type;
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptableObjectFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string path, string? property = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject { ["path"] = path };
        if (!string.IsNullOrWhiteSpace(property)) parameters["property"] = property;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptableObjectGet,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateSetPropertyRequest(string path, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptableObjectSetProperty,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["property"] = property,
                ["value"] = value
            }
        };
    }
}
