using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class UiCommandTests
{
    [CliTestFact]
    public void Find_SetsCommandName()
    {
        var request = UiCommand.CreateFindRequest(null, null, null, null, null, null, null, null, false, null);
        Assert.Equal(WellKnownCommands.UiFind, request.Command);
    }

    [CliTestFact]
    public void Find_SetsProvidedParameters()
    {
        var request = UiCommand.CreateFindRequest(
            "Pause",
            "Resume",
            "Button",
            "gid-parent",
            "gid-canvas",
            "active",
            "true",
            "false",
            includeInactive: true,
            limit: 5);

        Assert.Equal("Pause", request.Parameters!["name"]?.GetValue<string>());
        Assert.Equal("Resume", request.Parameters["text"]?.GetValue<string>());
        Assert.Equal("Button", request.Parameters["type"]?.GetValue<string>());
        Assert.Equal("gid-parent", request.Parameters["parent"]?.GetValue<string>());
        Assert.Equal("gid-canvas", request.Parameters["canvas"]?.GetValue<string>());
        Assert.Equal("active", request.Parameters["scene"]?.GetValue<string>());
        Assert.True(request.Parameters["interactable"]?.GetValue<bool>());
        Assert.False(request.Parameters["active"]?.GetValue<bool>());
        Assert.True(request.Parameters["includeInactive"]?.GetValue<bool>());
        Assert.Equal(5, request.Parameters["limit"]?.GetValue<int>());
    }

    [CliTestFact]
    public void Find_OmitsUnsetParameters()
    {
        var request = UiCommand.CreateFindRequest(null, null, null, null, null, null, null, null, false, null);

        Assert.False(request.Parameters!.ContainsKey("name"));
        Assert.False(request.Parameters.ContainsKey("text"));
        Assert.False(request.Parameters.ContainsKey("type"));
        Assert.False(request.Parameters.ContainsKey("parent"));
        Assert.False(request.Parameters.ContainsKey("canvas"));
        Assert.False(request.Parameters.ContainsKey("scene"));
        Assert.False(request.Parameters.ContainsKey("interactable"));
        Assert.False(request.Parameters.ContainsKey("active"));
        Assert.False(request.Parameters.ContainsKey("includeInactive"));
        Assert.False(request.Parameters.ContainsKey("limit"));
    }

    [CliTestFact]
    public void Find_InvalidInteractable_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            UiCommand.CreateFindRequest(null, null, null, null, null, null, "yes", null, false, null));
    }

    [CliTestFact]
    public void Get_SetsCommandName()
    {
        var request = UiCommand.CreateGetRequest("gid-ui");
        Assert.Equal(WellKnownCommands.UiGet, request.Command);
    }

    [CliTestFact]
    public void Get_SetsIdParameter()
    {
        var request = UiCommand.CreateGetRequest("gid-ui-123");
        Assert.Equal("gid-ui-123", request.Parameters!["id"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Get_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateGetRequest(""));
    }

    [CliTestFact]
    public void Toggle_SetsCommandNameAndParameters()
    {
        var request = UiCommand.CreateToggleRequest("gid-toggle", "true", "play");

        Assert.Equal(WellKnownCommands.UiToggle, request.Command);
        Assert.Equal("gid-toggle", request.Parameters!["id"]?.GetValue<string>());
        Assert.True(request.Parameters["value"]?.GetValue<bool>());
        Assert.Equal("play", request.Parameters["mode"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Toggle_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateToggleRequest("gid-toggle", "yes", "auto"));
    }

    [CliTestFact]
    public void Toggle_InvalidMode_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateToggleRequest("gid-toggle", "true", "runtime"));
    }

    [CliTestFact]
    public void Input_SetsCommandNameAndParameters()
    {
        var request = UiCommand.CreateInputRequest("gid-input", "Player Name", "edit");

        Assert.Equal(WellKnownCommands.UiInput, request.Command);
        Assert.Equal("gid-input", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("Player Name", request.Parameters["text"]?.GetValue<string>());
        Assert.Equal("edit", request.Parameters["mode"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Input_AllowsEmptyText()
    {
        var request = UiCommand.CreateInputRequest("gid-input", string.Empty, "auto");

        Assert.Equal(string.Empty, request.Parameters!["text"]?.GetValue<string>());
    }

    [CliTestFact]
    public async Task EnsureInteractiveEditorReadyAsync_WhenUnlocked_ContinuesWithoutReadyIpc()
    {
        var result = await UiCommand.EnsureInteractiveEditorReadyAsync(
            @"C:\project",
            _ => false,
            (_, _) => Task.FromResult(false),
            maxAttempts: 3,
            delayMs: 1);

        Assert.Equal(UiInteractiveReadinessResult.ContinueWithoutReadyIpc, result);
    }

    [CliTestFact]
    public async Task EnsureInteractiveEditorReadyAsync_WhenProbeTurnsReady_ReturnsReady()
    {
        var attempts = 0;
        var result = await UiCommand.EnsureInteractiveEditorReadyAsync(
            @"C:\project",
            _ => true,
            (_, _) =>
            {
                attempts++;
                return Task.FromResult(attempts >= 2);
            },
            maxAttempts: 3,
            delayMs: 1);

        Assert.Equal(UiInteractiveReadinessResult.Ready, result);
        Assert.Equal(2, attempts);
    }

    [CliTestFact]
    public void CreateInteractiveReadinessFailureResponse_IncludesUiSpecificGuidance()
    {
        var response = UiCommand.CreateInteractiveReadinessFailureResponse(
            @"C:\Users\gmdqn\robotapp",
            WellKnownCommands.UiInput);

        Assert.Equal(StatusCode.Busy, response.StatusCode);
        Assert.Contains("ui input", response.Message);
        Assert.True(response.Data!["requiresIpcReady"]!.GetValue<bool>());
        Assert.Contains("Batch fallback is not guaranteed", response.Data["followUpAction"]!.GetValue<string>());
    }
}
