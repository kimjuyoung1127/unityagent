using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class CameraCommand
{
    public static void List(string project, bool includeInactive = false, bool json = false)
    {
        var request = CreateListRequest(includeInactive);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string id, bool json = false)
    {
        var request = CreateGetRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest(bool includeInactive = false)
    {
        var parameters = new JsonObject();
        if (includeInactive) parameters["includeInactive"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.CameraList,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.CameraGet,
            Parameters = new JsonObject { ["id"] = id }
        };
    }
}
