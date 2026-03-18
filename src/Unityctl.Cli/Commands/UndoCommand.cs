using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class UndoCommand
{
    public static void Undo(string project, bool json = false)
    {
        var request = CreateUndoRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Redo(string project, bool json = false)
    {
        var request = CreateRedoRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateUndoRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.Undo,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateRedoRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.Redo,
            Parameters = new JsonObject()
        };
    }
}
