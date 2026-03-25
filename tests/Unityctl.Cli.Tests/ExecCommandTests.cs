using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ExecCommandTests
{
    [CliTestFact]
    public void CreateRequest_SetsExecCommandName()
    {
        var request = ExecCommand.CreateRequest("EditorApplication.isPlaying = true");

        Assert.Equal(WellKnownCommands.Exec, request.Command);
    }

    [CliTestFact]
    public void CreateRequest_SetsCodeParameter()
    {
        const string code = "EditorApplication.isPlaying = true";
        var request = ExecCommand.CreateRequest(code);

        Assert.Equal(code, request.Parameters!["code"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateRequest_EmptyCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ExecCommand.CreateRequest(""));
    }

    [CliTestFact]
    public void CreateRequest_WhitespaceCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ExecCommand.CreateRequest("   "));
    }

    [CliTestFact]
    public void CreateRequest_HasRequestId()
    {
        var request = ExecCommand.CreateRequest("Debug.Log(\"hello\")");

        Assert.False(string.IsNullOrEmpty(request.RequestId));
    }

    [CliTestFact]
    public void ResolveCode_WithInlineCode_ReturnsCode()
    {
        const string code = "EditorApplication.isPlaying";
        var result = ExecCommand.ResolveCode(code, file: null);

        Assert.Equal(code, result);
    }

    [CliTestFact]
    public void ResolveCode_NullCodeAndNullFile_ReturnsNull()
    {
        var result = ExecCommand.ResolveCode(null, file: null);

        Assert.Null(result);
    }

    [CliTestFact]
    public void ResolveCode_EmptyCode_ReturnsNull()
    {
        var result = ExecCommand.ResolveCode("", file: null);

        Assert.Null(result);
    }

    [CliTestFact]
    public void CreateListCallablesRequest_HasCorrectCommandAndParameters()
    {
        var request = ExecCommand.CreateListCallablesRequest("EditorApplication", 10);

        Assert.Equal(WellKnownCommands.ExecListCallables, request.Command);
        Assert.Equal("EditorApplication", request.Parameters!["filter"]?.GetValue<string>());
        Assert.Equal(10, request.Parameters["limit"]?.GetValue<int>());
    }

    [CliTestFact]
    public void CreateInvokeRequest_HasCorrectCommandAndParameters()
    {
        var request = ExecCommand.CreateInvokeRequest("UnityEditor.EditorApplication", "Beep", "[]");

        Assert.Equal(WellKnownCommands.ExecInvoke, request.Command);
        Assert.Equal("UnityEditor.EditorApplication", request.Parameters!["type"]?.GetValue<string>());
        Assert.Equal("Beep", request.Parameters["method"]?.GetValue<string>());
        Assert.Equal("[]", request.Parameters["args"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateInvokeRequest_EmptyType_Throws()
    {
        Assert.Throws<ArgumentException>(() => ExecCommand.CreateInvokeRequest("", "Beep", "[]"));
    }
}
