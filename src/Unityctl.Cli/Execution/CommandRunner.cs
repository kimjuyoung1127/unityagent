using System.Diagnostics;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.FlightRecorder;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

public static class CommandRunner
{
    public static void Execute(string project, CommandRequest request, bool json = false, bool retry = false)
    {
        var exitCode = ExecuteAsync(project, request, json, retry).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteAsync(string project, CommandRequest request, bool json, bool retry)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var sw = Stopwatch.StartNew();
        var response = await executor.ExecuteAsync(project, request, retry: retry);
        sw.Stop();

        RecordEntry(project, request, response, sw.ElapsedMilliseconds);

        PrintResponse(response, json);
        return GetExitCode(response);
    }

    internal static void PrintResponse(CommandResponse response, bool json)
    {
        if (json)
        {
            JsonOutput.PrintResponse(response);
            return;
        }

        ConsoleOutput.PrintResponse(response);
        if (!response.Success)
            ConsoleOutput.PrintRecovery(response.StatusCode);
    }

    internal static int GetExitCode(CommandResponse response)
        => response.Success ? 0 : 1;

    private static void RecordEntry(
        string project,
        CommandRequest request,
        CommandResponse response,
        long durationMs)
    {
        try
        {
            var exitCode = GetExitCode(response);
            var entry = new FlightEntry
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = request.Command,
                Project = project,
                Transport = null, // Transport selection is opaque; filled in a future phase
                StatusCode = (int)response.StatusCode,
                DurationMs = durationMs,
                RequestId = response.RequestId,
                Level = response.Success ? "info" : "error",
                ExitCode = exitCode,
                Error = response.Success ? null : response.Message,
                Machine = Environment.MachineName,
                V = Constants.Version,
                Args = request.Parameters?.ToJsonString(),
                Sid = null
            };

            new FlightLog().Record(entry);
        }
        catch
        {
            // Flight recording should never crash the CLI
        }
    }
}
