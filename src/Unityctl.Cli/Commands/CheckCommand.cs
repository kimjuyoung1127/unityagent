using Unityctl.Cli.Infrastructure;
using Unityctl.Cli.Output;
using Unityctl.Cli.Platform;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class CheckCommand
{
    public static void Execute(string project, string type = "compile", bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var runner = new BatchModeRunner(platform, discovery);

        var request = new CommandRequest
        {
            Parameters = new Dictionary<string, object?>
            {
                ["type"] = type
            }
        };

        var response = runner.ExecuteAsync(project, "check", request).GetAwaiter().GetResult();

        if (json)
            JsonOutput.PrintResponse(response);
        else
        {
            ConsoleOutput.PrintResponse(response);
            if (!response.Success)
                ConsoleOutput.PrintRecovery(response.StatusCode);
        }

        Environment.Exit(response.Success ? 0 : 1);
    }
}
