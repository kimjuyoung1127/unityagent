using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class TestCommand
{
    public static void Execute(
        string project,
        string mode = "edit",
        string? filter = null,
        bool wait = true,
        int timeout = 300,
        bool json = false)
    {
        var exitCode = ExecuteAsync(project, mode, filter, wait, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteAsync(
        string project,
        string mode,
        string? filter,
        bool wait,
        int timeout,
        bool json)
    {
        var isPlayMode = mode.Equals("play", StringComparison.OrdinalIgnoreCase)
                         || mode.Equals("playmode", StringComparison.OrdinalIgnoreCase);

        // PlayMode + --wait: force no-wait with warning
        if (isPlayMode && wait)
        {
            Console.Error.WriteLine(
                "[unityctl] Warning: PlayMode tests cause domain reload — --wait is not supported. Running with --no-wait.");
            wait = false;
        }

        var request = new CommandRequest
        {
            Command = WellKnownCommands.Test,
            Parameters = new JsonObject
            {
                ["mode"] = mode,
                ["filter"] = filter
            }
        };

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;

        if (wait)
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.TestResult,
                timeoutSeconds: timeout);
        }
        else
        {
            response = await executor.ExecuteAsync(project, request);
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }
}
