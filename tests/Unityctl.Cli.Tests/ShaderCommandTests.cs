using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ShaderCommandTests
{
    [Fact]
    public void FindRequest_HasCorrectCommand()
    {
        var request = ShaderCommand.CreateFindRequest();
        Assert.Equal(WellKnownCommands.ShaderFind, request.Command);
    }

    [Fact]
    public void FindRequest_Default_NoOptionalParams()
    {
        var request = ShaderCommand.CreateFindRequest();
        Assert.Null(request.Parameters!["filter"]);
        Assert.Null(request.Parameters!["includeBuiltin"]);
        Assert.Null(request.Parameters!["limit"]);
    }

    [Fact]
    public void FindRequest_WithFilter_SetsParameter()
    {
        var request = ShaderCommand.CreateFindRequest(filter: "Standard");
        Assert.Equal("Standard", request.Parameters!["filter"]!.ToString());
    }

    [Fact]
    public void FindRequest_IncludeBuiltin_SetsParameter()
    {
        var request = ShaderCommand.CreateFindRequest(includeBuiltin: true);
        Assert.True(request.Parameters!["includeBuiltin"]!.GetValue<bool>());
    }

    [Fact]
    public void FindRequest_WithLimit_SetsParameter()
    {
        var request = ShaderCommand.CreateFindRequest(limit: 20);
        Assert.Equal(20, request.Parameters!["limit"]!.GetValue<int>());
    }

    [Fact]
    public void FindRequest_AllOptions_SetsAll()
    {
        var request = ShaderCommand.CreateFindRequest(filter: "URP", includeBuiltin: true, limit: 50);
        Assert.Equal("URP", request.Parameters!["filter"]!.ToString());
        Assert.True(request.Parameters!["includeBuiltin"]!.GetValue<bool>());
        Assert.Equal(50, request.Parameters!["limit"]!.GetValue<int>());
    }

    [Fact]
    public void GetPropertiesRequest_HasCorrectCommand()
    {
        var request = ShaderCommand.CreateGetPropertiesRequest("Standard");
        Assert.Equal(WellKnownCommands.ShaderGetProperties, request.Command);
    }

    [Fact]
    public void GetPropertiesRequest_SetsName()
    {
        var request = ShaderCommand.CreateGetPropertiesRequest("Standard");
        Assert.Equal("Standard", request.Parameters!["name"]!.ToString());
    }

    [Fact]
    public void GetPropertiesRequest_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ShaderCommand.CreateGetPropertiesRequest(""));
    }

    [Fact]
    public void GetPropertiesRequest_NullName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ShaderCommand.CreateGetPropertiesRequest(null!));
    }
}
