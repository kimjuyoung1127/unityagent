using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class CameraCommandTests
{
    [Fact]
    public void ListRequest_HasCorrectCommand()
    {
        var request = CameraCommand.CreateListRequest();
        Assert.Equal(WellKnownCommands.CameraList, request.Command);
    }

    [Fact]
    public void ListRequest_Default_NoIncludeInactive()
    {
        var request = CameraCommand.CreateListRequest();
        Assert.Null(request.Parameters!["includeInactive"]);
    }

    [Fact]
    public void ListRequest_IncludeInactive_SetsParameter()
    {
        var request = CameraCommand.CreateListRequest(includeInactive: true);
        Assert.True(request.Parameters!["includeInactive"]!.GetValue<bool>());
    }

    [Fact]
    public void GetRequest_HasCorrectCommand()
    {
        var request = CameraCommand.CreateGetRequest("GlobalObjectId_V1-1-abc-123-0");
        Assert.Equal(WellKnownCommands.CameraGet, request.Command);
    }

    [Fact]
    public void GetRequest_SetsId()
    {
        var request = CameraCommand.CreateGetRequest("GlobalObjectId_V1-1-abc-123-0");
        Assert.Equal("GlobalObjectId_V1-1-abc-123-0", request.Parameters!["id"]!.ToString());
    }

    [Fact]
    public void GetRequest_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CameraCommand.CreateGetRequest(""));
    }

    [Fact]
    public void GetRequest_NullId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CameraCommand.CreateGetRequest(null!));
    }

    [Fact]
    public void GetRequest_WhitespaceId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CameraCommand.CreateGetRequest("  "));
    }
}
