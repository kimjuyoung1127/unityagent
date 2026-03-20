using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class TextureCommand
{
    public static void GetImportSettings(string project, string path, bool json = false)
    {
        var request = CreateGetImportSettingsRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetImportSettings(string project, string path, string property, string value, bool json = false)
    {
        var request = CreateSetImportSettingsRequest(path, property, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetImportSettingsRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.TextureGetImportSettings,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateSetImportSettingsRequest(string path, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.TextureSetImportSettings,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["property"] = property,
                ["value"] = value
            }
        };
    }
}
