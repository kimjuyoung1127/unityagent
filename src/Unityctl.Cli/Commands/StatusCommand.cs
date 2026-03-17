using Unityctl.Cli.Infrastructure;
using Unityctl.Cli.Output;
using Unityctl.Cli.Platform;

namespace Unityctl.Cli.Commands;

public static class StatusCommand
{
    public static void Execute(string project, bool wait = false, bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var runner = new BatchModeRunner(platform, discovery);
        var retry = new RetryPolicy();

        var task = wait
            ? retry.ExecuteWithRetryAsync(() => runner.ExecuteAsync(project, "status"))
            : runner.ExecuteAsync(project, "status");

        var response = task.GetAwaiter().GetResult();

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
