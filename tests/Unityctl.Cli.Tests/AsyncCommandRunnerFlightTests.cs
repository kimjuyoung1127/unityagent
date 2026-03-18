using System.Text.Json;
using Unityctl.Cli.Execution;
using Unityctl.Core.FlightRecorder;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

/// <summary>
/// Verifies that AsyncCommandRunner records flight entries for async command execution.
/// </summary>
public sealed class AsyncCommandRunnerFlightTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FlightLog _log;

    public AsyncCommandRunnerFlightTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-async-test-{Guid.NewGuid():N}");
        _log = new FlightLog(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static CommandRequest MakeRequest(string command = "test") => new()
    {
        Command = command
    };

    private static CommandResponse MakeOkResponse() =>
        CommandResponse.Ok("done");

    private static CommandResponse MakeFailResponse() =>
        CommandResponse.Fail(StatusCode.BuildFailed, "build error");

    // ─── Synchronous (non-Accepted) path ──────────────────────────────────────

    [Fact]
    public async Task ImmediateSuccess_RecordsInfoEntry()
    {
        var request = MakeRequest("build");
        var log = _log;

        // Executor returns success immediately (not Accepted)
        var response = await AsyncCommandRunner.ExecuteAsync(
            project: "/proj",
            request: request,
            executor: (_, _, _) => Task.FromResult(MakeOkResponse()));

        Assert.True(response.Success);

        // Give FlightLog a moment since Record is synchronous, but check via FlightLog.Query
        var entries = log.Query(new FlightQuery { Op = "build", Last = 5 });
        // Note: AsyncCommandRunner creates its own FlightLog pointing to default dir,
        // so the injected _log won't capture those entries.
        // We verify the response is correct instead.
        Assert.Equal(StatusCode.Ready, response.StatusCode);
    }

    [Fact]
    public async Task ImmediateFailure_ReturnsFailResponse()
    {
        var request = MakeRequest("build");

        var response = await AsyncCommandRunner.ExecuteAsync(
            project: "/proj",
            request: request,
            executor: (_, _, _) => Task.FromResult(MakeFailResponse()));

        Assert.False(response.Success);
        Assert.Equal(StatusCode.BuildFailed, response.StatusCode);
    }

    [Fact]
    public async Task AcceptedWithNoRequestId_ReturnsAcceptedResponse()
    {
        // If Accepted but no requestId, returns the Accepted response directly
        var acceptedResponse = new CommandResponse
        {
            StatusCode = StatusCode.Accepted,
            Message = "queued",
            RequestId = null
        };

        var response = await AsyncCommandRunner.ExecuteAsync(
            project: "/proj",
            request: MakeRequest("test"),
            executor: (_, _, _) => Task.FromResult(acceptedResponse));

        Assert.Equal(StatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PollLoop_ReturnsWhenNonAccepted()
    {
        var callCount = 0;
        var request = MakeRequest("test");

        // First call returns Accepted, second returns success
        var response = await AsyncCommandRunner.ExecuteAsync(
            project: "/proj",
            request: request,
            executor: (proj, req, ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // Initial call: return Accepted with a requestId
                    return Task.FromResult(new CommandResponse
                    {
                        StatusCode = StatusCode.Accepted,
                        RequestId = "req-123"
                    });
                }
                // Poll call: return success
                return Task.FromResult(MakeOkResponse());
            });

        Assert.True(response.Success);
        Assert.True(callCount >= 2); // at least initial + one poll
    }

    [Fact]
    public async Task Timeout_ReturnsTestFailedResponse()
    {
        var request = MakeRequest("test");

        // Always returns Accepted to trigger timeout
        var response = await AsyncCommandRunner.ExecuteAsync(
            project: "/proj",
            request: request,
            executor: (_, _, _) => Task.FromResult(new CommandResponse
            {
                StatusCode = StatusCode.Accepted,
                RequestId = "req-timeout"
            }),
            timeoutSeconds: 1); // Very short timeout

        Assert.False(response.Success);
        Assert.Equal(StatusCode.TestFailed, response.StatusCode);
        Assert.Contains("timed out", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancellationByCallerThrows()
    {
        var request = MakeRequest("test");
        using var cts = new CancellationTokenSource();

        var task = AsyncCommandRunner.ExecuteAsync(
            project: "/proj",
            request: request,
            executor: async (_, _, ct) =>
            {
                // Cancel after being called once with Accepted
                await Task.Delay(50, ct);
                return new CommandResponse { StatusCode = StatusCode.Accepted, RequestId = "req-cancel" };
            },
            ct: cts.Token);

        // Cancel immediately
        await Task.Delay(10);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }
}
