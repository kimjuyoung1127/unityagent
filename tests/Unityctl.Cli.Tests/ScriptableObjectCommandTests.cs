using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ScriptableObjectCommandTests
{
    // --- Find ---

    [Fact]
    public void FindRequest_HasCorrectCommand()
    {
        var request = ScriptableObjectCommand.CreateFindRequest();
        Assert.Equal(WellKnownCommands.ScriptableObjectFind, request.Command);
    }

    [Fact]
    public void FindRequest_Default_NoOptionalParams()
    {
        var request = ScriptableObjectCommand.CreateFindRequest();
        Assert.Null(request.Parameters!["type"]);
        Assert.Null(request.Parameters!["folder"]);
        Assert.Null(request.Parameters!["limit"]);
    }

    [Fact]
    public void FindRequest_WithType_SetsParameter()
    {
        var request = ScriptableObjectCommand.CreateFindRequest(type: "GameConfig");
        Assert.Equal("GameConfig", request.Parameters!["type"]!.ToString());
    }

    [Fact]
    public void FindRequest_WithFolder_SetsParameter()
    {
        var request = ScriptableObjectCommand.CreateFindRequest(folder: "Assets/Data");
        Assert.Equal("Assets/Data", request.Parameters!["folder"]!.ToString());
    }

    [Fact]
    public void FindRequest_WithLimit_SetsParameter()
    {
        var request = ScriptableObjectCommand.CreateFindRequest(limit: 10);
        Assert.Equal(10, request.Parameters!["limit"]!.GetValue<int>());
    }

    // --- Get ---

    [Fact]
    public void GetRequest_HasCorrectCommand()
    {
        var request = ScriptableObjectCommand.CreateGetRequest("Assets/Data/config.asset");
        Assert.Equal(WellKnownCommands.ScriptableObjectGet, request.Command);
    }

    [Fact]
    public void GetRequest_SetsPath()
    {
        var request = ScriptableObjectCommand.CreateGetRequest("Assets/Data/config.asset");
        Assert.Equal("Assets/Data/config.asset", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void GetRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScriptableObjectCommand.CreateGetRequest(""));
    }

    [Fact]
    public void GetRequest_WithProperty_SetsParameter()
    {
        var request = ScriptableObjectCommand.CreateGetRequest("Assets/Data/config.asset", property: "m_Name");
        Assert.Equal("m_Name", request.Parameters!["property"]!.ToString());
    }

    [Fact]
    public void GetRequest_NoProperty_OmitsKey()
    {
        var request = ScriptableObjectCommand.CreateGetRequest("Assets/Data/config.asset");
        Assert.Null(request.Parameters!["property"]);
    }

    // --- SetProperty ---

    [Fact]
    public void SetPropertyRequest_HasCorrectCommand()
    {
        var request = ScriptableObjectCommand.CreateSetPropertyRequest("Assets/Data/config.asset", "m_Name", "newName");
        Assert.Equal(WellKnownCommands.ScriptableObjectSetProperty, request.Command);
    }

    [Fact]
    public void SetPropertyRequest_SetsAllParameters()
    {
        var request = ScriptableObjectCommand.CreateSetPropertyRequest("Assets/Data/config.asset", "speed", "10.5");
        Assert.Equal("Assets/Data/config.asset", request.Parameters!["path"]!.ToString());
        Assert.Equal("speed", request.Parameters!["property"]!.ToString());
        Assert.Equal("10.5", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void SetPropertyRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScriptableObjectCommand.CreateSetPropertyRequest("", "m_Name", "newName"));
    }

    [Fact]
    public void SetPropertyRequest_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScriptableObjectCommand.CreateSetPropertyRequest("Assets/Data/config.asset", "", "newName"));
    }

    [Fact]
    public void SetPropertyRequest_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ScriptableObjectCommand.CreateSetPropertyRequest("Assets/Data/config.asset", "m_Name", null!));
    }

    [Fact]
    public void FindRequest_AllOptions_SetsAll()
    {
        var request = ScriptableObjectCommand.CreateFindRequest(type: "GameConfig", folder: "Assets/Data", limit: 5);
        Assert.Equal("GameConfig", request.Parameters!["type"]!.ToString());
        Assert.Equal("Assets/Data", request.Parameters!["folder"]!.ToString());
        Assert.Equal(5, request.Parameters!["limit"]!.GetValue<int>());
    }
}
