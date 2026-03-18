using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Xunit;
using Xunit.Sdk;

namespace Unityctl.Integration.Tests.Support;

internal static class JsonResponseExtensions
{
    public static CommandResponse ParseCommandResponse(string stdout, string commandName)
    {
        try
        {
            var response = JsonSerializer.Deserialize(stdout.Trim(), UnityctlJsonContext.Default.CommandResponse);
            if (response == null)
                throw SkipException.ForSkip($"SKIPPED: {commandName} did not emit a parsable JSON response.");

            return response;
        }
        catch (JsonException ex)
        {
            throw SkipException.ForSkip($"SKIPPED: {commandName} did not emit valid JSON: {ex.Message}");
        }
    }

    public static void SkipIfEnvironmentUnavailable(CommandResponse response, string commandName)
    {
        if (response.Success)
            return;

        var message = response.Message ?? string.Empty;
        if (response.StatusCode is StatusCode.NotFound or StatusCode.ProjectLocked or StatusCode.PluginNotInstalled)
        {
            throw SkipException.ForSkip(
                $"SKIPPED: {commandName} is unavailable in this environment ({response.StatusCode}): {message}");
        }
    }

    public static JsonElement RequireDataElement(CommandResponse response, string propertyName)
    {
        var data = RequireFullData(response);

        if (!data.TryGetProperty(propertyName, out var node))
            throw new XunitException($"Expected data payload to contain '{propertyName}'.");

        return node.Clone();
    }

    public static JsonElement RequireFullData(CommandResponse response)
    {
        if (response.Data == null)
            throw new XunitException("Expected command response data payload, but data was null.");

        var json = response.Data.ToJsonString();
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
