using System.Diagnostics;
using System.Text.Json;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Unityctl.Shared.Transport;

namespace Unityctl.Core.Transport;

/// <summary>
/// Batch transport: spawns Unity in batchmode for each command.
/// Extracted from BatchModeRunner.
/// </summary>
public sealed class BatchTransport : ITransport
{
    private readonly IPlatformServices _platform;
    private readonly UnityEditorDiscovery _discovery;
    private readonly string _projectPath;
    private readonly UnityProcessDetector _processDetector;

    public string Name => "batch";
    public TransportCapability Capabilities => TransportCapability.Command;

    public BatchTransport(IPlatformServices platform, UnityEditorDiscovery discovery, string projectPath)
    {
        _platform = platform;
        _discovery = discovery;
        _projectPath = Path.GetFullPath(projectPath);
        _processDetector = new UnityProcessDetector(_platform);
    }

    public async Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct = default)
    {
        if (_platform.IsProjectLocked(_projectPath))
        {
            var interactiveProcess = _processDetector.FindInteractiveProcessForProject(_projectPath);
            if (interactiveProcess != null)
            {
                return CommandResponse.Fail(StatusCode.ProjectLocked,
                    "Editor is running; IPC is not ready yet. Try `unityctl status --wait` before falling back to batch.");
            }

            var process = _processDetector.FindProcessForProject(_projectPath);
            if (process != null)
            {
                return CommandResponse.Fail(StatusCode.Busy,
                    $"A headless Unity process (pid {process.ProcessId}) is holding the project lock. Wait for it to exit before retrying batch commands.");
            }

            return CommandResponse.Fail(StatusCode.ProjectLocked,
                "Project lock is still held. Retry after the current Unity process exits or the stale lock clears.");
        }

        var editor = _discovery.FindEditorForProject(_projectPath);
        if (editor == null)
        {
            return CommandResponse.Fail(StatusCode.NotFound,
                $"No matching Unity Editor found for project at {_projectPath}");
        }

        var unityExe = _platform.GetUnityExecutablePath(editor.Location);
        if (!File.Exists(unityExe))
        {
            return CommandResponse.Fail(StatusCode.NotFound,
                $"Unity executable not found at {unityExe}");
        }

        if (string.IsNullOrEmpty(request.RequestId))
            request.RequestId = Guid.NewGuid().ToString("N");

        var requestPath = Path.Combine(Path.GetTempPath(), $"unityctl-req-{request.RequestId}.json");
        var responsePath = _platform.GetTempResponseFilePath();
        var logPath = Path.Combine(Path.GetTempPath(), $"unityctl-log-{request.RequestId}.log");

        try
        {
            var requestJson = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
            await File.WriteAllTextAsync(requestPath, requestJson, ct);

            var arguments = BuildArguments(_projectPath, request.Command, requestPath, responsePath, logPath);

            Console.Error.WriteLine($"[unityctl] Spawning Unity batchmode: {request.Command}");
            Console.Error.WriteLine($"[unityctl] Editor: {editor.Version} at {editor.Location}");

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

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Constants.BatchModeTimeoutMs);

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return CommandResponse.Fail(StatusCode.Busy,
                    $"Unity batchmode timed out after {Constants.BatchModeTimeoutMs / 1000}s");
            }

            if (File.Exists(responsePath))
            {
                var responseJson = await File.ReadAllTextAsync(responsePath, ct);
                var response = JsonSerializer.Deserialize(responseJson, UnityctlJsonContext.Default.CommandResponse);
                if (response != null)
                    return response;
            }

            var exitCode = process.ExitCode;
            var logTail = await TailLogAsync(logPath, 60);

            return CommandResponse.Fail(
                StatusCode.UnknownError,
                $"Unity exited with code {exitCode} but no response file was written.",
                string.IsNullOrEmpty(logTail) ? null : new List<string> { logTail });
        }
        finally
        {
            TryDelete(requestPath);
            TryDelete(responsePath);
        }
    }

    public IAsyncEnumerable<EventEnvelope>? SubscribeAsync(string channel, CancellationToken ct = default)
        => null; // Batch transport does not support streaming

    public async Task<bool> ProbeAsync(CancellationToken ct = default)
    {
        // Batch is always available if editor exists
        var editor = _discovery.FindEditorForProject(_projectPath);
        return await Task.FromResult(editor != null);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string BuildArguments(string projectPath, string command, string requestPath, string responsePath, string logPath)
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
