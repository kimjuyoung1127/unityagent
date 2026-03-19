using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class ScriptCommandTests
{
    [Fact]
    public void CreateListRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateListRequest();
        Assert.Equal(WellKnownCommands.ScriptList, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateListRequest_SetsFolderParameter()
    {
        var request = ScriptCommand.CreateListRequest(folder: "Assets/Scripts");
        Assert.Equal("Assets/Scripts", request.Parameters!["folder"]!.ToString());
    }

    [Fact]
    public void CreateListRequest_SetsFilterParameter()
    {
        var request = ScriptCommand.CreateListRequest(filter: "Player");
        Assert.Equal("Player", request.Parameters!["filter"]!.ToString());
    }

    [Fact]
    public void CreateListRequest_SetsLimitParameter()
    {
        var request = ScriptCommand.CreateListRequest(limit: 10);
        Assert.Equal(10, (int)request.Parameters!["limit"]!);
    }

    [Fact]
    public void CreateListRequest_NoOptionalParams_HasEmptyParameters()
    {
        var request = ScriptCommand.CreateListRequest();
        Assert.Empty(request.Parameters!);
    }

    [Fact]
    public void CreateCreateRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateCreateRequest("Assets/Scripts/Test.cs", "Test", null, "MonoBehaviour");
        Assert.Equal(WellKnownCommands.ScriptCreate, request.Command);
    }

    [Fact]
    public void CreateCreateRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => ScriptCommand.CreateCreateRequest("", "Test", null, "MonoBehaviour"));
    }

    [Fact]
    public void CreateEditRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateEditRequest("Assets/Scripts/Test.cs", "using UnityEngine;");
        Assert.Equal(WellKnownCommands.ScriptEdit, request.Command);
    }

    [Fact]
    public void CreateDeleteRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateDeleteRequest("Assets/Scripts/Test.cs");
        Assert.Equal(WellKnownCommands.ScriptDelete, request.Command);
    }

    [Fact]
    public void CreateValidateRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreateValidateRequest(null);
        Assert.Equal(WellKnownCommands.ScriptValidate, request.Command);
    }

    [Fact]
    public void CreatePatchRequest_HasCorrectCommand()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 5, 1, "// patched");
        Assert.Equal(WellKnownCommands.ScriptPatch, request.Command);
    }

    [Fact]
    public void CreatePatchRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ScriptCommand.CreatePatchRequest("", 1, 0, "// test"));
    }

    [Fact]
    public void CreatePatchRequest_SetsPathParameter()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 5, 2, "// new");
        Assert.Equal("Assets/Scripts/Test.cs", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void CreatePatchRequest_SetsStartLine()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 10, 0, "// header");
        Assert.Equal(10, (int)request.Parameters!["startLine"]!);
    }

    [Fact]
    public void CreatePatchRequest_SetsDeleteCount()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 3, 5, null);
        Assert.Equal(5, (int)request.Parameters!["deleteCount"]!);
    }

    [Fact]
    public void CreatePatchRequest_InsertOnly_NoContent_OmitsKey()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 1, 3, null);
        Assert.Null(request.Parameters!["insertContent"]);
    }

    [Fact]
    public void CreatePatchRequest_SetsInsertContent()
    {
        var request = ScriptCommand.CreatePatchRequest("Assets/Scripts/Test.cs", 1, 0, "using System;\nusing UnityEngine;");
        Assert.Equal("using System;\nusing UnityEngine;", request.Parameters!["insertContent"]!.ToString());
    }
}
