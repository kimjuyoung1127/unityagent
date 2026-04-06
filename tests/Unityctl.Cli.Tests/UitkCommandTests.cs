using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class UitkCommandTests
{
    [Fact]
    public void FindRequest_HasCorrectCommand()
    {
        var request = UitkCommand.CreateFindRequest(null, null, null, null);
        Assert.Equal(WellKnownCommands.UitkFind, request.Command);
    }

    [Fact]
    public void FindRequest_SetsOptionalParameters()
    {
        var request = UitkCommand.CreateFindRequest("myButton", "btn-primary", "Button", 10);
        Assert.Equal("myButton", request.Parameters!["name"]!.ToString());
        Assert.Equal("btn-primary", request.Parameters!["className"]!.ToString());
        Assert.Equal("Button", request.Parameters!["type"]!.ToString());
        Assert.Equal(10, (int)request.Parameters!["limit"]!);
    }

    [Fact]
    public void FindRequest_OmitsNullParameters()
    {
        var request = UitkCommand.CreateFindRequest(null, null, null, null);
        Assert.Null(request.Parameters!["name"]);
        Assert.Null(request.Parameters!["className"]);
    }

    [Fact]
    public void GetRequest_HasCorrectCommand()
    {
        var request = UitkCommand.CreateGetRequest(name: "myElement");
        Assert.Equal(WellKnownCommands.UitkGet, request.Command);
    }

    [Fact]
    public void GetRequest_SetsName()
    {
        var request = UitkCommand.CreateGetRequest(name: "myElement");
        Assert.Equal("myElement", request.Parameters!["name"]!.ToString());
    }

    [Fact]
    public void GetRequest_Locator_IsSupported()
    {
        var request = UitkCommand.CreateGetRequest(locator: "MainHud::root/0:Button#Play");
        Assert.Equal("MainHud::root/0:Button#Play", request.Parameters!["locator"]!.ToString());
    }

    [Fact]
    public void SetValueRequest_HasCorrectCommand()
    {
        var request = UitkCommand.CreateSetValueRequest("hello", name: "myField");
        Assert.Equal(WellKnownCommands.UitkSetValue, request.Command);
    }

    [Fact]
    public void SetValueRequest_SetsParameters()
    {
        var request = UitkCommand.CreateSetValueRequest("world", name: "myField");
        Assert.Equal("myField", request.Parameters!["name"]!.ToString());
        Assert.Equal("world", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void SetValueRequest_Locator_IsSupported()
    {
        var request = UitkCommand.CreateSetValueRequest("hello", locator: "MainHud::root/0:Button#Play");
        Assert.Equal("MainHud::root/0:Button#Play", request.Parameters!["locator"]!.ToString());
    }

    [Fact]
    public void SetValueRequest_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => UitkCommand.CreateSetValueRequest(null!, name: "myField"));
    }

    [Fact]
    public void SetValueRequest_EmptyValue_Allowed()
    {
        var request = UitkCommand.CreateSetValueRequest("", name: "myField");
        Assert.Equal("", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void GetRequest_RequiresNameOrLocator()
    {
        Assert.Throws<ArgumentException>(() => UitkCommand.CreateGetRequest());
    }

    [Fact]
    public void SetValueRequest_RequiresNameOrLocator()
    {
        Assert.Throws<ArgumentException>(() => UitkCommand.CreateSetValueRequest("hello"));
    }
}
