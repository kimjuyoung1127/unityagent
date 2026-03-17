using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Output;

public static class JsonOutput
{
    public static void PrintResponse(CommandResponse response)
    {
        var json = JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
        Console.WriteLine(json);
    }
}
