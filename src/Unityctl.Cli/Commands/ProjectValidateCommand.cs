using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ProjectValidateCommand
{
    public static void Execute(string project, bool json = false)
    {
        var request = CreateRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.ProjectValidate
        };
    }
}
