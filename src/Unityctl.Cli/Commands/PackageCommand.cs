using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PackageCommand
{
    public static void List(string project, bool json = false)
    {
        var request = CreateListRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Add(string project, string package, bool json = false)
    {
        var request = CreateAddRequest(package);
        CommandRunner.Execute(project, request, json);
    }

    public static void Remove(string project, string package, bool json = false)
    {
        var request = CreateRemoveRequest(package);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.PackageList,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateAddRequest(string package)
    {
        if (string.IsNullOrWhiteSpace(package))
            throw new ArgumentException("package must not be empty", nameof(package));

        return new CommandRequest
        {
            Command = WellKnownCommands.PackageAdd,
            Parameters = new JsonObject { ["package"] = package }
        };
    }

    internal static CommandRequest CreateRemoveRequest(string package)
    {
        if (string.IsNullOrWhiteSpace(package))
            throw new ArgumentException("package must not be empty", nameof(package));

        return new CommandRequest
        {
            Command = WellKnownCommands.PackageRemove,
            Parameters = new JsonObject { ["package"] = package }
        };
    }
}
