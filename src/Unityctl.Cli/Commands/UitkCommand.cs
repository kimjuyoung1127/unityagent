using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class UitkCommand
{
    public static void Find(string project, string? name = null, string? className = null, string? type = null, int? limit = null, bool json = false)
    {
        var request = CreateFindRequest(name, className, type, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string? name = null, string? locator = null, bool json = false)
    {
        var request = CreateGetRequest(name, locator);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetValue(string project, string value, string? name = null, string? locator = null, bool json = false)
    {
        var request = CreateSetValueRequest(value, name, locator);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateFindRequest(string? name, string? className, string? type, int? limit)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(name)) parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(className)) parameters["className"] = className;
        if (!string.IsNullOrWhiteSpace(type)) parameters["type"] = type;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.UitkFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string? name = null, string? locator = null)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(locator))
            throw new ArgumentException("name or locator must not be empty");

        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(name))
            parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(locator))
            parameters["locator"] = locator;

        return new CommandRequest
        {
            Command = WellKnownCommands.UitkGet,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateSetValueRequest(string value, string? name = null, string? locator = null)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(locator))
            throw new ArgumentException("name or locator must not be empty");
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var parameters = new JsonObject
        {
            ["value"] = value
        };
        if (!string.IsNullOrWhiteSpace(name))
            parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(locator))
            parameters["locator"] = locator;

        return new CommandRequest
        {
            Command = WellKnownCommands.UitkSetValue,
            Parameters = parameters
        };
    }
}
