using Unityctl.Cli.Infrastructure;
using Unityctl.Cli.Output;
using Unityctl.Cli.Platform;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class TestCommand
{
    public static void Execute(string project, string mode = "edit", string? filter = null, bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var runner = new BatchModeRunner(platform, discovery);

        var request = new CommandRequest
        {
            Parameters = new Dictionary<string, object?>
            {
                ["mode"] = mode,
                ["filter"] = filter
            }
        };

        var response = runner.ExecuteAsync(project, "test", request).GetAwaiter().GetResult();

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
