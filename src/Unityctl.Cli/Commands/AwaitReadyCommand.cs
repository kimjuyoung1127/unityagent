using System.Diagnostics;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AwaitReadyCommand
{
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(1);

    public static void Execute(string? project = null, int timeout = 300, bool json = false)
    {
        if (!CommandRunner.TryResolveProject(project, out var resolvedProject, out var failureResponse))
        {
            CommandRunner.PrintResponse(failureResponse!, json);
            Environment.ExitCode = 1;
            return;
        }

        var response = ExecuteAsync(resolvedProject, timeout).GetAwaiter().GetResult();
        CommandRunner.PrintResponse(resolvedProject, response, json);
        Environment.ExitCode = CommandRunner.GetExitCode(response);
    }

    internal static async Task<CommandResponse> ExecuteAsync(
        string project,
        int timeoutSeconds,
        Func<string, Task<CommandResponse>>? statusAsync = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Func<string, bool>? isInteractiveEditorRunning = null,
        CancellationToken ct = default)
    {
        if (timeoutSeconds <= 0)
            return CommandResponse.Fail(StatusCode.InvalidParameters, "timeout must be a positive integer");

        statusAsync ??= CreateStatusProbeAsync;
        delayAsync ??= static (delay, cancellationToken) => Task.Delay(delay, cancellationToken);
        var platform = PlatformFactory.Create();
        var interactiveCheck = isInteractiveEditorRunning ?? (path => new UnityProcessDetector(platform).IsInteractiveEditorRunning(path));

        var sw = Stopwatch.StartNew();
        var attempt = 0;
        CommandResponse? lastResponse = null;

        while (sw.Elapsed < TimeSpan.FromSeconds(timeoutSeconds))
        {
            ct.ThrowIfCancellationRequested();
            attempt++;
            lastResponse = await statusAsync(project).ConfigureAwait(false);

            if (IsStableReady(lastResponse))
            {
                lastResponse.Data ??= new JsonObject();
                lastResponse.Data["attempts"] = attempt;
                lastResponse.Data["elapsedMs"] = sw.ElapsedMilliseconds;
                lastResponse.Data["recommendedNextCommand"] = "Proceed with build, check, or other IPC-backed commands.";
                return lastResponse;
            }

            if (!interactiveCheck(project))
                return CreateNoInteractiveEditorResponse(project, sw.ElapsedMilliseconds, attempt, lastResponse);

            await delayAsync(PollDelay, ct).ConfigureAwait(false);
        }

        return CreateTimeoutResponse(project, timeoutSeconds, sw.ElapsedMilliseconds, attempt, lastResponse);
    }

    internal static bool IsStableReady(CommandResponse response)
    {
        if (!response.Success || response.StatusCode != StatusCode.Ready)
            return false;

        var transport = response.Data?["target"]?["transport"]?.GetValue<string>();
        var isCompiling = response.Data?["isCompiling"]?.GetValue<bool>() ?? false;
        var isDomainReloading = response.Data?["isDomainReloading"]?.GetValue<bool>() ?? false;

        return string.Equals(transport, "ipc", StringComparison.OrdinalIgnoreCase)
               && !isCompiling
               && !isDomainReloading;
    }

    internal static CommandResponse CreateTimeoutResponse(
        string project,
        int timeoutSeconds,
        long elapsedMs,
        int attempts,
        CommandResponse? lastResponse)
    {
        var response = CommandResponse.Fail(
            StatusCode.Busy,
            $"Unity did not become IPC-ready and compile-stable within {timeoutSeconds}s.");

        response.Data = new JsonObject
        {
            ["attempts"] = attempts,
            ["elapsedMs"] = elapsedMs,
            ["recommendedNextCommand"] = $"unityctl doctor --project \"{project}\" --json"
        };

        if (lastResponse?.StatusCode != null)
            response.Data["lastStatusCode"] = (int)lastResponse.StatusCode;
        if (!string.IsNullOrWhiteSpace(lastResponse?.Message))
            response.Data["lastMessage"] = lastResponse.Message;
        if (lastResponse?.Data != null)
            response.Data["lastData"] = lastResponse.Data.DeepClone();

        return response;
    }

    internal static CommandResponse CreateNoInteractiveEditorResponse(
        string project,
        long elapsedMs,
        int attempts,
        CommandResponse? lastResponse)
    {
        var response = CommandResponse.Fail(
            StatusCode.Busy,
            "No interactive Unity Editor is running for this project, so await-ready cannot wait for IPC stability.");

        response.Data = new JsonObject
        {
            ["attempts"] = attempts,
            ["elapsedMs"] = elapsedMs,
            ["recommendedNextCommand"] = $"unityctl status --project \"{project}\" --json"
        };

        if (lastResponse?.StatusCode != null)
            response.Data["lastStatusCode"] = (int)lastResponse.StatusCode;
        if (!string.IsNullOrWhiteSpace(lastResponse?.Message))
            response.Data["lastMessage"] = lastResponse.Message;
        if (lastResponse?.Data != null)
            response.Data["lastData"] = lastResponse.Data.DeepClone();

        return response;
    }

    private static async Task<CommandResponse> CreateStatusProbeAsync(string project)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        return await executor.ExecuteAsync(
            project,
            new CommandRequest { Command = WellKnownCommands.Status },
            retry: false).ConfigureAwait(false);
    }
}
