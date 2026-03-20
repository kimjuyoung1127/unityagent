using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PingCommand
{
    public static void Execute(string? project = null, bool json = false)
    {
        var request = new CommandRequest { Command = WellKnownCommands.Ping };
        CommandRunner.Execute(project, request, json);
    }
}
