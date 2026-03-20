using System.Text.Json;
using Unityctl.Cli.Commands;
using Unityctl.Shared.Models;
using Xunit;

namespace Unityctl.Cli.Tests;

[Collection("ConsoleOutput")]
public sealed class UnityEditorInstancesTests
{
    [CliTestFact]
    public void InstancesCore_Json_PrintsInstanceArray()
    {
        UnityEditorInstanceInfo[] instances =
        [
            new UnityEditorInstanceInfo
            {
                ProcessId = 55028,
                ProjectPath = "c:/users/ezen601/desktop/jason/my project",
                Version = "6000.0.64f1",
                EditorLocation = @"C:\Program Files\Unity\Hub\Editor\6000.0.64f1",
                PipeName = "unityctl_dd58be40d478d9be",
                IpcReady = true
            }
        ];

        var previousOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            EditorCommands.InstancesCore(instances, json: true);
        }
        finally
        {
            Console.SetOut(previousOut);
        }

        var parsed = JsonSerializer.Deserialize<UnityEditorInstanceInfo[]>(sw.ToString());
        Assert.NotNull(parsed);
        Assert.Single(parsed!);
        Assert.Equal(55028, parsed[0].ProcessId);
    }

    [CliTestFact]
    public void InstancesCore_Text_PrintsHelpfulEmptyMessage()
    {
        var previousOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            EditorCommands.InstancesCore([], json: false);
        }
        finally
        {
            Console.SetOut(previousOut);
        }

        Assert.Contains("No running Unity Editor instances found.", sw.ToString());
    }
}
