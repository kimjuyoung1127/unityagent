using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ShaderCommand
{
    public static void Find(string project, string? filter = null, bool includeBuiltin = false, int? limit = null, bool json = false)
    {
        var request = CreateFindRequest(filter, includeBuiltin, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void GetProperties(string project, string name, bool json = false)
    {
        var request = CreateGetPropertiesRequest(name);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateFindRequest(string? filter = null, bool includeBuiltin = false, int? limit = null)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(filter)) parameters["filter"] = filter;
        if (includeBuiltin) parameters["includeBuiltin"] = true;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.ShaderFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetPropertiesRequest(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.ShaderGetProperties,
            Parameters = new JsonObject { ["name"] = name }
        };
    }
}
