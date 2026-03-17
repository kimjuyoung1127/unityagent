using System.Diagnostics;
using System.Text.Json;
using Unityctl.Cli.Platform;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Infrastructure;

/// <summary>
/// Spawns Unity in batchmode with --response-file protocol.
/// Handles request/response file management, process lifecycle, and log tailing.
/// </summary>
public sealed class BatchModeRunner
{
    private readonly IPlatformServices _platform;
    private readonly UnityEditorDiscovery _discovery;

    public BatchModeRunner(IPlatformServices platform, UnityEditorDiscovery discovery)
    {
        _platform = platform;
        _discovery = discovery;
    }

    /// <summary>
    /// Execute a command via Unity batchmode.
    /// </summary>
    public async Task<CommandResponse> ExecuteAsync(
        string projectPath,
        string command,
        CommandRequest? request = null,
        int timeoutMs = Constants.BatchModeTimeoutMs,
        CancellationToken ct = default)
    {
        projectPath = Path.GetFullPath(projectPath);

        // Check project lock
        if (_platform.IsProjectLocked(projectPath))
        {
            return CommandResponse.Fail(StatusCode.ProjectLocked,
                "Unity project is locked by another process. Close the running Editor first.");
        }

        // Find editor
        var editor = _discovery.FindEditorForProject(projectPath);
        if (editor == null)
        {
            return CommandResponse.Fail(StatusCode.NotFound,
                $"No matching Unity Editor found for project at {projectPath}");
        }

        var unityExe = _platform.GetUnityExecutablePath(editor.Location);
        if (!File.Exists(unityExe))
        {
            return CommandResponse.Fail(StatusCode.NotFound,
                $"Unity executable not found at {unityExe}");
        }

        // Prepare request/response files
        request ??= new CommandRequest();
        request.Command = command;
        if (string.IsNullOrEmpty(request.RequestId))
            request.RequestId = Guid.NewGuid().ToString("N");

        var requestPath = Path.Combine(Path.GetTempPath(), $"unityctl-req-{request.RequestId}.json");
        var responsePath = _platform.GetTempResponseFilePath();
        var logPath = Path.Combine(Path.GetTempPath(), $"unityctl-log-{request.RequestId}.log");

        try
        {
            // Write request file
            var requestJson = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
            await File.WriteAllTextAsync(requestPath, requestJson, ct);

            // Build Unity arguments
            var arguments = BuildArguments(projectPath, command, requestPath, responsePath, logPath);

            Console.Error.WriteLine($"[unityctl] Spawning Unity batchmode: {command}");
            Console.Error.WriteLine($"[unityctl] Editor: {editor.Version} at {editor.Location}");

            // Spawn Unity process
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = unityExe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();

            // Wait with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return CommandResponse.Fail(StatusCode.Busy,
                    $"Unity batchmode timed out after {timeoutMs / 1000}s");
            }

            // Read response file
            if (File.Exists(responsePath))
            {
                var responseJson = await File.ReadAllTextAsync(responsePath, ct);
                var response = JsonSerializer.Deserialize(responseJson, UnityctlJsonContext.Default.CommandResponse);
                if (response != null)
                {
                    return response;
                }
            }

            // Fallback: response file doesn't exist or parse failed
            var exitCode = process.ExitCode;
            var logTail = await TailLogAsync(logPath, 60);

            return CommandResponse.Fail(
                exitCode == 0 ? StatusCode.UnknownError : StatusCode.UnknownError,
                $"Unity exited with code {exitCode} but no response file was written.",
                string.IsNullOrEmpty(logTail) ? null : new List<string> { logTail });
        }
        finally
        {
            // Cleanup temp files
            TryDelete(requestPath);
            TryDelete(responsePath);
            // Keep log file for debugging (user can delete manually)
        }
    }

    private string BuildArguments(string projectPath, string command, string requestPath, string responsePath, string logPath)
    {
        return string.Join(" ",
            "-batchmode",
            "-nographics",
            $"-projectPath \"{projectPath}\"",
            $"-logFile \"{logPath}\"",
            $"-executeMethod {Constants.BatchEntryMethod}",
            "--",
            $"--unityctl-command {command}",
            $"--unityctl-request \"{requestPath}\"",
            $"--unityctl-response \"{responsePath}\"");
    }

    private static async Task<string> TailLogAsync(string logPath, int lines)
    {
        if (!File.Exists(logPath)) return string.Empty;
        try
        {
            var allLines = await File.ReadAllLinesAsync(logPath);
            var tail = allLines.Skip(Math.Max(0, allLines.Length - lines));
            return string.Join(Environment.NewLine, tail);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
