using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class UiEnhancementCommandTests
{
    // Scroll
    [Fact]
    public void ScrollRequest_HasCorrectCommand()
    {
        var request = UiCommand.CreateScrollRequest("goid123", "0.5", "0.8");
        Assert.Equal(WellKnownCommands.UiScroll, request.Command);
    }

    [Fact]
    public void ScrollRequest_SetsIdAndMode()
    {
        var request = UiCommand.CreateScrollRequest("goid123", null, null, "edit");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("edit", request.Parameters!["mode"]!.ToString());
    }

    [Fact]
    public void ScrollRequest_SetsOptionalXY()
    {
        var request = UiCommand.CreateScrollRequest("goid123", "0.5", "0.8");
        Assert.Equal("0.5", request.Parameters!["x"]!.ToString());
        Assert.Equal("0.8", request.Parameters!["y"]!.ToString());
    }

    [Fact]
    public void ScrollRequest_OmitsNullXY()
    {
        var request = UiCommand.CreateScrollRequest("goid123", null, null);
        Assert.Null(request.Parameters!["x"]);
        Assert.Null(request.Parameters!["y"]);
    }

    [Fact]
    public void ScrollRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateScrollRequest("", "0.5", null));
    }

    // SliderSet
    [Fact]
    public void SliderSetRequest_HasCorrectCommand()
    {
        var request = UiCommand.CreateSliderSetRequest("goid123", "0.75");
        Assert.Equal(WellKnownCommands.UiSliderSet, request.Command);
    }

    [Fact]
    public void SliderSetRequest_SetsAllParameters()
    {
        var request = UiCommand.CreateSliderSetRequest("goid123", "0.75", "play");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("0.75", request.Parameters!["value"]!.ToString());
        Assert.Equal("play", request.Parameters!["mode"]!.ToString());
    }

    [Fact]
    public void SliderSetRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateSliderSetRequest("", "0.5"));
    }

    [Fact]
    public void SliderSetRequest_EmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateSliderSetRequest("goid123", ""));
    }

    [Fact]
    public void SliderSetRequest_InvalidMode_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateSliderSetRequest("goid123", "0.5", "invalid"));
    }

    // DropdownSet
    [Fact]
    public void DropdownSetRequest_HasCorrectCommand()
    {
        var request = UiCommand.CreateDropdownSetRequest("goid123", "2");
        Assert.Equal(WellKnownCommands.UiDropdownSet, request.Command);
    }

    [Fact]
    public void DropdownSetRequest_SetsAllParameters()
    {
        var request = UiCommand.CreateDropdownSetRequest("goid123", "3", "edit");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("3", request.Parameters!["value"]!.ToString());
        Assert.Equal("edit", request.Parameters!["mode"]!.ToString());
    }

    [Fact]
    public void DropdownSetRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateDropdownSetRequest("", "0"));
    }

    [Fact]
    public void DropdownSetRequest_EmptyValue_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateDropdownSetRequest("goid123", ""));
    }

    [Fact]
    public void ClickRequest_HasCorrectCommand()
    {
        var request = UiCommand.CreateClickRequest("goid123", "active", "play");
        Assert.Equal(WellKnownCommands.UiClick, request.Command);
    }

    [Fact]
    public void ClickRequest_SetsAllParameters()
    {
        var request = UiCommand.CreateClickRequest("goid123", "MainScene", "play");
        Assert.Equal("goid123", request.Parameters!["id"]!.ToString());
        Assert.Equal("MainScene", request.Parameters["scene"]!.ToString());
        Assert.Equal("play", request.Parameters["mode"]!.ToString());
    }

    [Fact]
    public void ClickRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => UiCommand.CreateClickRequest("", "active", "play"));
    }
}
