using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class AwaitReadyCommandTests
{
    [CliTestFact]
    public void IsStableReady_RequiresIpcTransportAndNoReloadFlags()
    {
        var response = CommandResponse.Ok("Ready", new JsonObject
        {
            ["isCompiling"] = false,
            ["isDomainReloading"] = false,
            ["target"] = new JsonObject
            {
                ["transport"] = "ipc"
            }
        });

        Assert.True(AwaitReadyCommand.IsStableReady(response));
    }

    [CliTestFact]
    public void IsStableReady_RejectsBatchFallback()
    {
        var response = CommandResponse.Ok("Ready", new JsonObject
        {
            ["isCompiling"] = false,
            ["isDomainReloading"] = false,
            ["target"] = new JsonObject
            {
                ["transport"] = "batch"
            }
        });

        Assert.False(AwaitReadyCommand.IsStableReady(response));
    }

    [CliTestFact]
    public async Task ExecuteAsync_WaitsUntilStableReady()
    {
        var calls = 0;
        var response = await AwaitReadyCommand.ExecuteAsync(
            @"C:\project",
            timeoutSeconds: 5,
            statusAsync: _ =>
            {
                calls++;
                return Task.FromResult(calls < 3
                    ? new CommandResponse
                    {
                        StatusCode = StatusCode.Reloading,
                        Success = true,
                        Message = "Reloading",
                        Data = new JsonObject
                        {
                            ["isCompiling"] = false,
                            ["isDomainReloading"] = true,
                            ["target"] = new JsonObject
                            {
                                ["transport"] = "ipc"
                            }
                        }
                    }
                    : CommandResponse.Ok("Ready", new JsonObject
                    {
                        ["isCompiling"] = false,
                        ["isDomainReloading"] = false,
                        ["target"] = new JsonObject
                        {
                            ["transport"] = "ipc"
                        }
                    }));
            },
            delayAsync: (_, _) => Task.CompletedTask);

        Assert.True(response.Success);
        Assert.Equal(StatusCode.Ready, response.StatusCode);
        Assert.Equal(3, response.Data!["attempts"]!.GetValue<int>());
    }

    [CliTestFact]
    public async Task ExecuteAsync_TimesOutWhenNeverStable()
    {
        var response = await AwaitReadyCommand.ExecuteAsync(
            @"C:\project",
            timeoutSeconds: 1,
            statusAsync: _ => Task.FromResult(new CommandResponse
            {
                StatusCode = StatusCode.Busy,
                Success = false,
                Message = "Unity Editor is running but IPC is not ready yet."
            }),
            delayAsync: (_, _) => Task.CompletedTask);

        Assert.False(response.Success);
        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("doctor", response.Data!["recommendedNextCommand"]!.GetValue<string>());
    }
}
