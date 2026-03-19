using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ProjectValidateCommandTests
{
    [Fact]
    public void CreateRequest_HasCorrectCommand()
    {
        var request = ProjectValidateCommand.CreateRequest();
        Assert.Equal(WellKnownCommands.ProjectValidate, request.Command);
    }

    [Fact]
    public void CreateRequest_HasRequestId()
    {
        var request = ProjectValidateCommand.CreateRequest();
        Assert.NotNull(request.RequestId);
        Assert.NotEmpty(request.RequestId);
    }

    [Fact]
    public void CreateRequest_HasEmptyParameters()
    {
        var request = ProjectValidateCommand.CreateRequest();
        Assert.Null(request.Parameters);
    }
}
