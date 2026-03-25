using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class CliCommandSuggestionsTests
{
    [CliTestFact]
    public void TryBuildUnknownCommandResponse_WithKnownCommand_ReturnsFalse()
    {
        var handled = CliCommandSuggestions.TryBuildUnknownCommandResponse(
            ["status", "--json"],
            out _,
            out _);

        Assert.False(handled);
    }

    [CliTestFact]
    public void TryBuildUnknownCommandResponse_WithConcretePlaySubcommand_ReturnsFalse()
    {
        var handled = CliCommandSuggestions.TryBuildUnknownCommandResponse(
            ["play", "start", "--json"],
            out _,
            out _);

        Assert.False(handled);
    }

    [CliTestFact]
    public void TryBuildUnknownCommandResponse_SuggestsNearestCommand()
    {
        var handled = CliCommandSuggestions.TryBuildUnknownCommandResponse(
            ["console", "entries"],
            out var response,
            out var json);

        Assert.True(handled);
        Assert.False(json);
        Assert.Equal(StatusCode.CommandNotFound, response.StatusCode);
        Assert.Contains("console get-entries", response.Message);
        Assert.Equal("console get-entries", response.Data!["suggestedCommand"]!.GetValue<string>());
    }

    [CliTestFact]
    public void TryBuildUnknownCommandResponse_PreservesJsonMode()
    {
        var handled = CliCommandSuggestions.TryBuildUnknownCommandResponse(
            ["package", "resove", "--json"],
            out var response,
            out var json);

        Assert.True(handled);
        Assert.True(json);
        Assert.Equal("package resolve", response.Data!["suggestedCommand"]!.GetValue<string>());
    }
}
