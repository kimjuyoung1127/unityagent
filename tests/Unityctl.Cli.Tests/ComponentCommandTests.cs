using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ComponentCommandTests
{
    // === Get ===

    [CliTestFact]
    public void Get_SetsCommandName()
    {
        var request = ComponentCommand.CreateGetRequest("comp-gid", null);
        Assert.Equal(WellKnownCommands.ComponentGet, request.Command);
    }

    [CliTestFact]
    public void Get_SetsComponentId()
    {
        var request = ComponentCommand.CreateGetRequest("comp-gid-123", null);
        Assert.Equal("comp-gid-123", request.Parameters!["componentId"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Get_IncludesPropertyWhenProvided()
    {
        var request = ComponentCommand.CreateGetRequest("comp-gid-123", "m_LocalPosition");
        Assert.Equal("m_LocalPosition", request.Parameters!["property"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Get_OmitsPropertyWhenNull()
    {
        var request = ComponentCommand.CreateGetRequest("comp-gid-123", null);
        Assert.False(request.Parameters!.ContainsKey("property"));
    }

    [CliTestFact]
    public void Get_EmptyComponentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateGetRequest("", null));
    }

    // === Add ===

    [CliTestFact]
    public void Add_SetsCommandName()
    {
        var request = ComponentCommand.CreateAddRequest("gid-go", null, "UnityEngine.Rigidbody");
        Assert.Equal(WellKnownCommands.ComponentAdd, request.Command);
    }

    [CliTestFact]
    public void Add_SetsIdAndType()
    {
        var request = ComponentCommand.CreateAddRequest("gid-go", null, "UnityEngine.BoxCollider");
        Assert.Equal("gid-go", request.Parameters!["id"]?.GetValue<string>());
        Assert.Equal("UnityEngine.BoxCollider", request.Parameters!["type"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Add_WithName_SetsNameParam()
    {
        var request = ComponentCommand.CreateAddRequest(null, "MyCube", "UnityEngine.Rigidbody");
        Assert.Equal("MyCube", request.Parameters!["name"]?.GetValue<string>());
        Assert.False(request.Parameters!.ContainsKey("id"));
    }

    [CliTestFact]
    public void Add_NeitherIdNorName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateAddRequest(null, null, "Rigidbody"));
    }

    [CliTestFact]
    public void Add_EmptyIdAndName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateAddRequest("", "", "Rigidbody"));
    }

    [CliTestFact]
    public void Add_EmptyType_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateAddRequest("gid", null, ""));
    }

    [CliTestFact]
    public void Add_HasRequestId()
    {
        var request = ComponentCommand.CreateAddRequest("gid", null, "Rigidbody");
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    // === Remove ===

    [CliTestFact]
    public void Remove_SetsCommandName()
    {
        var request = ComponentCommand.CreateRemoveRequest("comp-gid");
        Assert.Equal(WellKnownCommands.ComponentRemove, request.Command);
    }

    [CliTestFact]
    public void Remove_SetsComponentId()
    {
        var request = ComponentCommand.CreateRemoveRequest("comp-gid-123");
        Assert.Equal("comp-gid-123", request.Parameters!["componentId"]?.GetValue<string>());
    }

    [CliTestFact]
    public void Remove_EmptyComponentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateRemoveRequest(""));
    }

    // === SetProperty ===

    [CliTestFact]
    public void SetProperty_SetsCommandName()
    {
        var request = ComponentCommand.CreateSetPropertyRequest("comp-gid", "mass", "5");
        Assert.Equal(WellKnownCommands.ComponentSetProperty, request.Command);
    }

    [CliTestFact]
    public void SetProperty_SetsAllParameters()
    {
        var request = ComponentCommand.CreateSetPropertyRequest("comp-gid", "m_LocalPosition", "{\"x\":1,\"y\":2,\"z\":3}");
        Assert.Equal("comp-gid", request.Parameters!["componentId"]?.GetValue<string>());
        Assert.Equal("m_LocalPosition", request.Parameters!["property"]?.GetValue<string>());
        Assert.Equal("{\"x\":1,\"y\":2,\"z\":3}", request.Parameters!["value"]?.GetValue<string>());
    }

    [CliTestFact]
    public void SetProperty_EmptyComponentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateSetPropertyRequest("", "prop", "val"));
    }

    [CliTestFact]
    public void SetProperty_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() => ComponentCommand.CreateSetPropertyRequest("gid", "", "val"));
    }

    [CliTestFact]
    public void SetProperty_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ComponentCommand.CreateSetPropertyRequest("gid", "prop", null!));
    }

    [CliTestFact]
    public void SetProperty_HasRequestId()
    {
        var request = ComponentCommand.CreateSetPropertyRequest("gid", "prop", "val");
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void Get_HasRequestId()
    {
        var request = ComponentCommand.CreateGetRequest("gid", null);
        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }
}
