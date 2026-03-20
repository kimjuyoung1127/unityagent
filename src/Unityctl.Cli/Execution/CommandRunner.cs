using System.Diagnostics;
using Unityctl.Cli.Output;
using Unityctl.Cli.Commands;
using Unityctl.Core.Diagnostics;
using Unityctl.Core.Discovery;
using Unityctl.Core.EditorRouting;
using Unityctl.Core.FlightRecorder;
using Unityctl.Core.Platform;
using Unityctl.Core.Sessions;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

public static class CommandRunner
{
    public static void Execute(string? project, CommandRequest request, bool json = false, bool retry = false)
    {
        var exitCode = ExecuteAsync(project, request, json, retry).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteAsync(string? project, CommandRequest request, bool json, bool retry)
    {
        if (!TryResolveProject(project, out var resolvedProject, out var failureResponse))
        {
            PrintResponse(string.Empty, failureResponse!, json);
            return GetExitCode(failureResponse!);
        }

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        SessionManager? sessionManager = null;
        string? sessionId = null;
        try
        {
            sessionManager = new SessionManager(new NdjsonSessionStore());
            var session = await sessionManager.StartAsync(
                request.Command,
                resolvedProject,
                pipeName: Constants.GetPipeName(resolvedProject));
            sessionId = session.Id;
        }
        catch
        {
            // Session tracking must never crash the CLI
        }

        var sw = Stopwatch.StartNew();
        var response = await executor.ExecuteAsync(resolvedProject, request, retry: retry);
        sw.Stop();

        if (sessionManager != null && sessionId != null)
        {
            try
            {
                if (response.Success)
                    await sessionManager.CompleteAsync(sessionId, response.Data);
                else
                    await sessionManager.FailAsync(sessionId, response.Message ?? "Command failed");
            }
            catch
            {
                // Session tracking must never crash the CLI
            }
        }

        RecordEntry(resolvedProject, request, response, sw.ElapsedMilliseconds, sessionId);

        PrintResponse(resolvedProject, response, json);
        return GetExitCode(response);
    }

    internal static bool TryResolveProject(
        string? project,
        out string resolvedProject,
        out CommandResponse? failureResponse,
        EditorSelectionStore? selectionStore = null)
    {
        failureResponse = null;
        resolvedProject = string.Empty;

        if (!string.IsNullOrWhiteSpace(project))
        {
            resolvedProject = Path.GetFullPath(project);
            return true;
        }

        var selection = (selectionStore ?? new EditorSelectionStore()).GetCurrent();
        if (selection == null)
        {
            failureResponse = CommandResponse.Fail(
                StatusCode.InvalidParameters,
                "No project specified and no current editor selection. Run `unityctl editor select --project <path>` or pass --project.");
            return false;
        }

        var versionFilePath = Path.Combine(selection.ProjectPath, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFilePath))
        {
            failureResponse = CommandResponse.Fail(
                StatusCode.InvalidParameters,
                $"Current editor selection is stale: {selection.ProjectPath}. Re-run `unityctl editor select --project <path>`.");
            return false;
        }

        resolvedProject = selection.ProjectPath;
        return true;
    }

    internal static void PrintResponse(string project, CommandResponse response, bool json)
    {
        if (json)
        {
            JsonOutput.PrintResponse(response);
            return;
        }

        ConsoleOutput.PrintResponse(response);
        if (!response.Success)
        {
            PrintEditorLogDiagnostics(response.StatusCode);
            PrintDoctorDiagnostics(project, response);
            ConsoleOutput.PrintRecovery(response.StatusCode);
        }
    }

    internal static void PrintResponse(CommandResponse response, bool json)
        => PrintResponse(string.Empty, response, json);

    internal static int GetExitCode(CommandResponse response)
        => response.Success ? 0 : 1;

    /// <summary>
    /// For ProjectLocked / Busy failures, read Unity Editor.log and print any compile errors.
    /// Helps diagnose IPC failures caused by plugin compilation issues.
    /// </summary>
    private static void PrintEditorLogDiagnostics(StatusCode statusCode)
    {
        if (statusCode is not (StatusCode.ProjectLocked or StatusCode.Busy))
            return;

        var diagnostics = EditorLogDiagnostics.GetRecentDiagnostics();
        if (diagnostics != null)
            Console.Error.WriteLine(diagnostics);
    }

    private static void PrintDoctorDiagnostics(string project, CommandResponse response)
    {
        if (string.IsNullOrWhiteSpace(project))
            return;

        if (!DoctorCommand.ShouldAutoDiagnose(response))
            return;

        Console.Error.WriteLine(DoctorCommand.RenderAutoDiagnosis(project));
    }

    private static void RecordEntry(
        string project,
        CommandRequest request,
        CommandResponse response,
        long durationMs,
        string? sessionId = null)
    {
        try
        {
            var exitCode = GetExitCode(response);
            var entry = new FlightEntry
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = request.Command,
                Project = Constants.NormalizeProjectPath(project),
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
                Sid = sessionId
            };

            new FlightLog().Record(entry);
        }
        catch
        {
            // Flight recording should never crash the CLI
        }
    }
}
