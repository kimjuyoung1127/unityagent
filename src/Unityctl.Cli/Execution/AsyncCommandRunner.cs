using System.Diagnostics;
using System.Text.Json.Nodes;
using Unityctl.Core.FlightRecorder;
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
        int timeoutSeconds = 300,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var response = await executor(project, request, ct);

        if (response.StatusCode != StatusCode.Accepted)
        {
            sw.Stop();
            RecordEntry(project, request, response, sw.ElapsedMilliseconds);
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
            Command = WellKnownCommands.TestResult,
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
                    RecordEntry(project, request, pollResponse, sw.ElapsedMilliseconds);
                    return pollResponse;
                }

                await Task.Delay(PollIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            sw.Stop();
            var timeoutResponse = CommandResponse.Fail(
                StatusCode.TestFailed,
                $"Test execution timed out after {timeoutSeconds}s");
            RecordEntry(project, request, timeoutResponse, sw.ElapsedMilliseconds);
            return timeoutResponse;
        }

        // Caller cancelled
        throw new OperationCanceledException(ct);
    }

    private static void RecordEntry(
        string project,
        CommandRequest request,
        CommandResponse response,
        long durationMs)
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
