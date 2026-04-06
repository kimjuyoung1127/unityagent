using Unityctl.Core.Transport;
using Unityctl.Core.Platform;
using Unityctl.Shared.Models;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Transport;

public sealed class CommandExecutorReadinessTests
{
    [Fact]
    public void BuildInteractiveBusyResponse_ForScriptGetErrors_AddsScriptSpecificGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.ScriptGetErrors);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("script get-errors", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("script validate", response.Data["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForNonScriptCommand_KeepsGenericMessage()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.Status);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("IPC is not ready", response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForUiInput_AddsUiSpecificGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.UiInput);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("ui input", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("deterministically", response.Data["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void BuildInteractiveBusyResponse_ForUiClick_AddsPlayModeGuidance()
    {
        var response = CommandExecutor.BuildInteractiveBusyResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.UiClick);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("ui click", response.Message);
        Assert.Contains("Button.onClick", response.Data!["followUpAction"]!.GetValue<string>());
    }

    [Fact]
    public void AttachTargetMetadata_AddsNormalizedTargetBlock()
    {
        var response = CommandExecutor.AttachTargetMetadata(
            CommandResponse.Ok("ok"),
            @"C:\Users\gmdqn\RobotApp",
            "ipc",
            new UnityEditorInfo
            {
                Version = "6000.0.64f1",
                Location = @"C:\Program Files\Unity\Hub\Editor\6000.0.64f1"
            },
            new UnityProcessInfo
            {
                ProcessId = 2944,
                ProjectPath = @"C:\Users\gmdqn\RobotApp"
            },
            projectLocked: true,
            fallbackReason: "ipc-probe-failed");

        Assert.NotNull(response.Data);
        Assert.Equal("ipc", response.Data!["target"]!["transport"]!.GetValue<string>());
        Assert.Equal("6000.0.64f1", response.Data["target"]!["editorVersion"]!.GetValue<string>());
        Assert.Contains("unityctl_", response.Data["target"]!["pipeName"]!.GetValue<string>());
        Assert.Equal(2944, response.Data["target"]!["unityPid"]!.GetValue<int>());
        Assert.True(response.Data["target"]!["projectLocked"]!.GetValue<bool>());
        Assert.Equal("ipc-probe-failed", response.Data["target"]!["fallbackReason"]!.GetValue<string>());
    }

    [Fact]
    public void BuildHeadlessBusyResponse_ExplainsInteractiveRequirement()
    {
        var response = CommandExecutor.BuildHeadlessBusyResponse(
            WellKnownCommands.Status,
            new UnityProcessInfo
            {
                ProcessId = 40184,
                IsBatchMode = true
            });

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("headless Unity process", response.Message);
        Assert.True(response.Data!["requiresInteractiveEditor"]!.GetValue<bool>());
    }
}
