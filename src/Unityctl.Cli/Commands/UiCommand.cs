using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class UiCommand
{
    public static void CanvasCreate(string project, string name = "Canvas", string? renderMode = null, bool json = false)
    {
        var request = CreateCanvasCreateRequest(name, renderMode);
        CommandRunner.Execute(project, request, json);
    }

    public static void ElementCreate(string project, string type, string? name = null, string? parent = null, bool json = false)
    {
        var request = CreateElementCreateRequest(type, name, parent);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetRect(
        string project,
        string id,
        string? anchoredPosition = null,
        string? sizeDelta = null,
        string? anchorMin = null,
        string? anchorMax = null,
        string? pivot = null,
        bool json = false)
    {
        var request = CreateSetRectRequest(id, anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCanvasCreateRequest(string name, string? renderMode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        var parameters = new JsonObject { ["name"] = name };
        if (!string.IsNullOrEmpty(renderMode)) parameters["renderMode"] = renderMode;

        return new CommandRequest
        {
            Command = WellKnownCommands.UiCanvasCreate,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateElementCreateRequest(string type, string? name, string? parent)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("type must not be empty", nameof(type));

        var parameters = new JsonObject { ["type"] = type };
        if (!string.IsNullOrEmpty(name)) parameters["name"] = name;
        if (!string.IsNullOrEmpty(parent)) parameters["parent"] = parent;

        return new CommandRequest
        {
            Command = WellKnownCommands.UiElementCreate,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateSetRectRequest(
        string id,
        string? anchoredPosition,
        string? sizeDelta,
        string? anchorMin,
        string? anchorMax,
        string? pivot)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        var parameters = new JsonObject { ["id"] = id };
        if (!string.IsNullOrEmpty(anchoredPosition)) parameters["anchoredPosition"] = anchoredPosition;
        if (!string.IsNullOrEmpty(sizeDelta)) parameters["sizeDelta"] = sizeDelta;
        if (!string.IsNullOrEmpty(anchorMin)) parameters["anchorMin"] = anchorMin;
        if (!string.IsNullOrEmpty(anchorMax)) parameters["anchorMax"] = anchorMax;
        if (!string.IsNullOrEmpty(pivot)) parameters["pivot"] = pivot;

        return new CommandRequest
        {
            Command = WellKnownCommands.UiSetRect,
            Parameters = parameters
        };
    }
}
