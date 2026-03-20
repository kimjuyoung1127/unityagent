using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;

namespace Unityctl.Cli.Tests;

public class WorkflowCommandTests
{
    [CliTestFact]
    public void WorkflowDefinition_Deserialize_ValidJson()
    {
        const string json = """
        {
            "name": "build-and-test",
            "continueOnError": false,
            "steps": [
                { "command": "build", "project": "/MyProject", "parameters": { "target": "WebGL" } },
                { "command": "test", "project": "/MyProject" }
            ]
        }
        """;

        var workflow = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowDefinition);

        Assert.NotNull(workflow);
        Assert.Equal("build-and-test", workflow!.Name);
        Assert.Equal(2, workflow.Steps.Length);
        Assert.False(workflow.ContinueOnError);
    }

    [CliTestFact]
    public void WorkflowStep_Deserialize_CommandAndProject()
    {
        const string json = """
        { "command": "ping", "project": "/path/to/project" }
        """;

        var step = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowStep);

        Assert.NotNull(step);
        Assert.Equal("ping", step!.Command);
        Assert.Equal("/path/to/project", step.Project);
    }

    [CliTestFact]
    public void WorkflowDefinition_ContinueOnError_DefaultsFalse()
    {
        const string json = """{ "name": "test", "steps": [] }""";

        var workflow = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowDefinition);

        Assert.NotNull(workflow);
        Assert.False(workflow!.ContinueOnError);
    }

    [CliTestFact]
    public void WorkflowDefinition_SerializeRoundTrip_PreservesData()
    {
        var original = new WorkflowDefinition
        {
            Name = "my-workflow",
            ContinueOnError = true,
            Steps =
            [
                new WorkflowStep { Command = "build", Project = "/proj", TimeoutSeconds = 60 }
            ]
        };

        var json = JsonSerializer.Serialize(original, UnityctlJsonContext.Default.WorkflowDefinition);
        var deserialized = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowDefinition);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized!.Name);
        Assert.True(deserialized.ContinueOnError);
        Assert.Single(deserialized.Steps);
        Assert.Equal("build", deserialized.Steps[0].Command);
        Assert.Equal(60, deserialized.Steps[0].TimeoutSeconds);
    }

    [CliTestFact]
    public void WorkflowStep_TimeoutSeconds_Null_WhenOmitted()
    {
        const string json = """{ "command": "check", "project": "/proj" }""";

        var step = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.WorkflowStep);

        Assert.NotNull(step);
        Assert.Null(step!.TimeoutSeconds);
    }

    [CliTestFact]
    public void VerificationDefinition_Deserialize_ValidJson()
    {
        const string json = """
        {
            "name": "verify-basic",
            "steps": [
                { "id": "validate", "kind": "projectValidate" },
                { "id": "baseline", "kind": "capture", "view": "scene", "width": 640, "height": 360, "format": "png" },
                { "id": "current", "kind": "capture", "view": "scene" },
                { "id": "diff", "kind": "imageDiff", "baseline": "baseline", "candidate": "current", "maxChangedPixelRatio": 0.0 },
                { "id": "assert", "kind": "uiAssert", "targetId": "gid-toggle", "field": "toggle.isOn", "expected": false },
                { "id": "smoke", "kind": "playSmoke", "durationSeconds": 1, "settleTimeoutSeconds": 5 }
            ]
        }
        """;

        var definition = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.VerificationDefinition);

        Assert.NotNull(definition);
        Assert.Equal("verify-basic", definition!.Name);
        Assert.Equal(6, definition.Steps.Length);
        Assert.Equal("capture", definition.Steps[1].Kind);
        Assert.Equal("toggle.isOn", definition.Steps[4].Field);
        Assert.Equal(5, definition.Steps[5].SettleTimeoutSeconds);
    }

    [CliTestFact]
    public void TryReadNode_ReadsNestedJsonField()
    {
        var json = JsonNode.Parse("""
        {
          "toggle": {
            "isOn": false
          }
        }
        """)!;

        var value = WorkflowCommand.TryReadNode(json, "toggle.isOn");

        Assert.NotNull(value);
        Assert.False(value!.GetValue<bool>());
    }

    [CliTestFact]
    public async Task WaitForPlayModeAsync_ReturnsTrue_WhenStatusEventuallyReportsPlaying()
    {
        var callCount = 0;
        var fakeExecutor = new Func<string, Unityctl.Shared.Protocol.CommandRequest, CancellationToken, Task<Unityctl.Shared.Protocol.CommandResponse>>(
            (_, _, _) =>
            {
                callCount++;
                return Task.FromResult(Unityctl.Shared.Protocol.CommandResponse.Ok(data: new JsonObject
                {
                    ["isPlaying"] = callCount >= 3
                }));
            });

        var result = await WorkflowCommand.WaitForPlayModeAsync(
            "/proj",
            2,
            fakeExecutor);

        Assert.True(result);
    }
}
