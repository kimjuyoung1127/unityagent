using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class MaterialCommand
{
    public static void Get(string project, string path, string? property = null, bool json = false)
    {
        var request = CreateGetRequest(path, property);
        CommandRunner.Execute(project, request, json);
    }

    public static void Set(string project, string path, string property, string propertyType, string value, bool json = false)
    {
        var request = CreateSetRequest(path, property, propertyType, value);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetShader(string project, string path, string shader, bool json = false)
    {
        var request = CreateSetShaderRequest(path, shader);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetRequest(string path, string? property)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject { ["path"] = path };
        if (!string.IsNullOrEmpty(property)) parameters["property"] = property;

        return new CommandRequest
        {
            Command = WellKnownCommands.MaterialGet,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateSetRequest(string path, string property, string propertyType, string value)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (string.IsNullOrWhiteSpace(propertyType))
            throw new ArgumentException("propertyType must not be empty", nameof(propertyType));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.MaterialSet,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["property"] = property,
                ["propertyType"] = propertyType,
                ["value"] = value
            }
        };
    }

    internal static CommandRequest CreateSetShaderRequest(string path, string shader)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(shader))
            throw new ArgumentException("shader must not be empty", nameof(shader));

        return new CommandRequest
        {
            Command = WellKnownCommands.MaterialSetShader,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["shader"] = shader
            }
        };
    }
}
