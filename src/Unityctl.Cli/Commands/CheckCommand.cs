using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class CheckCommand
{
    public static void Execute(string? project = null, string type = "compile", bool json = false)
    {
        var request = new CommandRequest
        {
            Command = WellKnownCommands.Check,
            Parameters = new JsonObject
            {
                ["type"] = type
            }
        };

        CommandRunner.Execute(project, request, json);
    }
}
