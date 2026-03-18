using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class AsyncCommandRunnerTests
{
    private static CommandResponse AcceptedResponse(string requestId) =>
        new()
        {
            StatusCode = StatusCode.Accepted,
            Success = true,
            Message = "Tests started",
            RequestId = requestId,
            Data = new JsonObject { ["requestId"] = requestId }
        };

    private static CommandResponse CompletedResponse() =>
        CommandResponse.Ok("Tests completed: 5 passed, 0 skipped",
            new JsonObject { ["passed"] = 5, ["failed"] = 0 });

    private static CommandResponse FailedResponse() =>
        CommandResponse.Fail(StatusCode.TestFailed, "1 test failed");

    [CliTestFact]
    public async Task ImmediateResult_NonAccepted_ReturnsDirectly()
    {
        var ok = CommandResponse.Ok("pong");
        var callCount = 0;

        var result = await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, _, _) =>
            {
                callCount++;
                return Task.FromResult(ok);
            },
            timeoutSeconds: 5);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.Ready, result.StatusCode);
        Assert.Equal(1, callCount);
    }

    [CliTestFact]
    public async Task WaitSuccess_PollsUntilComplete()
    {
        var pollCount = 0;
        const string requestId = "abc123";

        var result = await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, req, _) =>
            {
                if (req.Command == "test")
                    return Task.FromResult(AcceptedResponse(requestId));

                // test-result polling
                pollCount++;
                if (pollCount < 3)
                    return Task.FromResult(AcceptedResponse(requestId)); // still running

                return Task.FromResult(CompletedResponse());
            },
            timeoutSeconds: 30);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.Ready, result.StatusCode);
        Assert.Equal(3, pollCount);
    }

    [CliTestFact]
    public async Task WaitSuccess_FailedTests_ReturnsFail()
    {
        const string requestId = "fail1";

        var result = await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, req, _) =>
            {
                if (req.Command == "test")
                    return Task.FromResult(AcceptedResponse(requestId));
                return Task.FromResult(FailedResponse());
            },
            timeoutSeconds: 30);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.TestFailed, result.StatusCode);
    }

    [CliTestFact]
    public async Task WaitTimeout_ReturnsTimeoutError()
    {
        const string requestId = "timeout1";

        var result = await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, req, _) =>
            {
                if (req.Command == "test")
                    return Task.FromResult(AcceptedResponse(requestId));
                return Task.FromResult(AcceptedResponse(requestId)); // always running
            },
            timeoutSeconds: 2);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.TestFailed, result.StatusCode);
        Assert.Contains("timed out", result.Message);
    }

    [CliTestFact]
    public async Task WaitCancel_ThrowsOperationCancelled()
    {
        const string requestId = "cancel1";
        using var cts = new CancellationTokenSource();

        var pollCount = 0;
        var task = AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, req, _) =>
            {
                if (req.Command == "test")
                    return Task.FromResult(AcceptedResponse(requestId));

                pollCount++;
                if (pollCount >= 2)
                    cts.Cancel();

                return Task.FromResult(AcceptedResponse(requestId));
            },
            timeoutSeconds: 30,
            ct: cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [CliTestFact]
    public async Task NoRequestId_ReturnsAcceptedDirectly()
    {
        var noIdResponse = new CommandResponse
        {
            StatusCode = StatusCode.Accepted,
            Success = true,
            Message = "Started"
            // No RequestId, no Data
        };

        var result = await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, _, _) => Task.FromResult(noIdResponse),
            timeoutSeconds: 5);

        Assert.Equal(StatusCode.Accepted, result.StatusCode);
    }

    [CliTestFact]
    public async Task PollRequest_UsesTestResultCommand()
    {
        const string requestId = "cmd1";
        string? polledCommand = null;
        string? polledRequestId = null;

        await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "test" },
            (_, req, _) =>
            {
                if (req.Command == "test")
                    return Task.FromResult(AcceptedResponse(requestId));

                polledCommand = req.Command;
                polledRequestId = req.Parameters?["requestId"]?.GetValue<string>();
                return Task.FromResult(CompletedResponse());
            },
            timeoutSeconds: 30);

        Assert.Equal(WellKnownCommands.TestResult, polledCommand);
        Assert.Equal(requestId, polledRequestId);
    }

    [CliTestFact]
    public async Task PollRequest_UsesCustomPollCommand()
    {
        const string requestId = "asset1";
        string? polledCommand = null;

        await AsyncCommandRunner.ExecuteAsync(
            "/project",
            new CommandRequest { Command = "asset-refresh" },
            (_, req, _) =>
            {
                if (req.Command == "asset-refresh")
                    return Task.FromResult(AcceptedResponse(requestId));

                polledCommand = req.Command;
                return Task.FromResult(CompletedResponse());
            },
            pollCommand: WellKnownCommands.AssetRefreshResult,
            timeoutSeconds: 30);

        Assert.Equal(WellKnownCommands.AssetRefreshResult, polledCommand);
    }
}
