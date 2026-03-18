using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ProjectSettingsCommand
{
    public static void Get(string project, string scope, string property, bool json = false)
    {
        var request = CreateGetRequest(scope, property);
        CommandRunner.Execute(project, request, json);
    }

    public static void Set(string project, string scope, string property, string value, bool json = false)
    {
        var request = CreateSetRequest(scope, property, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetRequest(string scope, string property)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("scope must not be empty", nameof(scope));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));

        return new CommandRequest
        {
            Command = WellKnownCommands.ProjectSettingsGet,
            Parameters = new JsonObject
            {
                ["scope"] = scope,
                ["property"] = property
            }
        };
    }

    internal static CommandRequest CreateSetRequest(string scope, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("scope must not be empty", nameof(scope));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.ProjectSettingsSet,
            Parameters = new JsonObject
            {
                ["scope"] = scope,
                ["property"] = property,
                ["value"] = value
            }
        };
    }
}
