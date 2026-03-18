using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AnimationCommand
{
    public static void CreateClip(string project, string path, bool json = false)
    {
        var request = CreateCreateClipRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void CreateController(string project, string path, bool json = false)
    {
        var request = CreateCreateControllerRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateClipRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationCreateClip,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateCreateControllerRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AnimationCreateController,
            Parameters = new JsonObject { ["path"] = path }
        };
    }
}
