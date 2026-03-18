using System.Diagnostics;
using System.Text.Json.Nodes;
using Unityctl.Core.FlightRecorder;
using Unityctl.Core.Sessions;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

/// <summary>
/// Polls for async command completion after receiving an Accepted response.
/// Uses delegate injection for testability.
/// </summary>
public static class AsyncCommandRunner
{
    private const int InitialDelayMs = 500;
    private const int PollIntervalMs = 1000;

    /// <summary>
    /// Execute a command and poll for completion if it returns Accepted.
    /// </summary>
    /// <param name="project">Unity project path.</param>
    /// <param name="request">The initial command request.</param>
    /// <param name="executor">Delegate that sends a command and returns a response.</param>
    /// <param name="timeoutSeconds">Maximum seconds to wait for completion.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<CommandResponse> ExecuteAsync(
        string project,
        CommandRequest request,
        Func<string, CommandRequest, CancellationToken, Task<CommandResponse>> executor,
        string pollCommand = WellKnownCommands.TestResult,
        int timeoutSeconds = 300,
        StatusCode timeoutStatusCode = StatusCode.TestFailed,
        string? timeoutMessage = null,
        CancellationToken ct = default)
    {
        SessionManager? sessionManager = null;
        string? sessionId = null;
        try
        {
            sessionManager = new SessionManager(new NdjsonSessionStore());
            var session = await sessionManager.StartAsync(request.Command, project, ct: ct);
            sessionId = session.Id;
        }
        catch
        {
            // Session tracking must never crash the CLI
        }

        var sw = Stopwatch.StartNew();
        var response = await executor(project, request, ct);

        if (response.StatusCode != StatusCode.Accepted)
        {
            sw.Stop();
            await TryRecordSessionResultAsync(sessionManager, sessionId, response, ct);
            RecordEntry(project, request, response, sw.ElapsedMilliseconds, sessionId);
            return response;
        }

        // Extract requestId from response
        var requestId = response.RequestId;
        if (string.IsNullOrEmpty(requestId))
        {
            // Try from data as fallback
            requestId = response.Data?["requestId"]?.GetValue<string>();
        }

        if (string.IsNullOrEmpty(requestId))
            return response;

        // Poll loop
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var pollRequest = new CommandRequest
        {
            Command = pollCommand,
            Parameters = new JsonObject
            {
                ["requestId"] = requestId
            }
        };

        try
        {
            await Task.Delay(InitialDelayMs, linkedCts.Token);

            while (!linkedCts.Token.IsCancellationRequested)
            {
                var pollResponse = await executor(project, pollRequest, linkedCts.Token);

                if (pollResponse.StatusCode != StatusCode.Accepted)
                {
                    sw.Stop();
                    await TryRecordSessionResultAsync(sessionManager, sessionId, pollResponse, linkedCts.Token);
                    RecordEntry(project, request, pollResponse, sw.ElapsedMilliseconds, sessionId);
                    return pollResponse;
                }

                await Task.Delay(PollIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            sw.Stop();
            var timeoutResponse = CommandResponse.Fail(
                timeoutStatusCode,
                timeoutMessage ?? $"Command execution timed out after {timeoutSeconds}s");
            if (sessionManager != null && sessionId != null)
            {
                try { await sessionManager.TimeoutAsync(sessionId, ct); } catch { }
            }
            RecordEntry(project, request, timeoutResponse, sw.ElapsedMilliseconds, sessionId);
            return timeoutResponse;
        }

        // Caller cancelled
        throw new OperationCanceledException(ct);
    }

    private static async Task TryRecordSessionResultAsync(
        SessionManager? manager,
        string? sessionId,
        CommandResponse response,
        CancellationToken ct)
    {
        if (manager == null || sessionId == null) return;
        try
        {
            if (response.Success)
                await manager.CompleteAsync(sessionId, response.Data, ct);
            else
                await manager.FailAsync(sessionId, response.Message ?? "Command failed", ct);
        }
        catch
        {
            // Session tracking must never crash the CLI
        }
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
            var entry = new FlightEntry
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = request.Command,
                Project = project,
                StatusCode = (int)response.StatusCode,
                DurationMs = durationMs,
                RequestId = response.RequestId,
                Level = response.Success ? "info" : "error",
                ExitCode = response.Success ? 0 : 1,
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
