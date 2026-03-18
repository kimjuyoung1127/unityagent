using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class UndoCommandTests
{
    [CliTestFact]
    public void CreateUndoRequest_HasCorrectCommand()
    {
        var request = UndoCommand.CreateUndoRequest();

        Assert.Equal(WellKnownCommands.Undo, request.Command);
        Assert.NotNull(request.Parameters);
    }

    [CliTestFact]
    public void CreateUndoRequest_HasRequestId()
    {
        var request = UndoCommand.CreateUndoRequest();

        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void CreateRedoRequest_HasCorrectCommand()
    {
        var request = UndoCommand.CreateRedoRequest();

        Assert.Equal(WellKnownCommands.Redo, request.Command);
        Assert.NotNull(request.Parameters);
    }

    [CliTestFact]
    public void CreateRedoRequest_HasRequestId()
    {
        var request = UndoCommand.CreateRedoRequest();

        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }
}
