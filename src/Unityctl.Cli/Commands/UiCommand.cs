using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class UiCommand
{
    private const int InteractiveUiProbeAttempts = 12;
    private const int InteractiveUiProbeDelayMs = 1000;

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

    public static void Find(
        string project,
        string? name = null,
        string? text = null,
        string? type = null,
        string? parent = null,
        string? canvas = null,
        string? scene = null,
        string? interactable = null,
        string? active = null,
        bool includeInactive = false,
        int? limit = null,
        bool json = false)
    {
        var request = CreateFindRequest(name, text, type, parent, canvas, scene, interactable, active, includeInactive, limit);
        CommandRunner.Execute(project, request, json);
    }

    public static void Get(string project, string id, bool json = false)
    {
        var request = CreateGetRequest(id);
        CommandRunner.Execute(project, request, json);
    }

    public static void Toggle(string project, string id, string value, string mode = "auto", bool json = false)
    {
        var request = CreateToggleRequest(id, value, mode);
        var exitCode = ExecuteInteractiveAsync(project, request, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    public static void Input(string project, string id, string text, string mode = "auto", bool json = false)
    {
        var request = CreateInputRequest(id, text, mode);
        var exitCode = ExecuteInteractiveAsync(project, request, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
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

    internal static CommandRequest CreateFindRequest(
        string? name,
        string? text,
        string? type,
        string? parent,
        string? canvas,
        string? scene,
        string? interactable,
        string? active,
        bool includeInactive,
        int? limit)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(name)) parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(text)) parameters["text"] = text;
        if (!string.IsNullOrWhiteSpace(type)) parameters["type"] = type;
        if (!string.IsNullOrWhiteSpace(parent)) parameters["parent"] = parent;
        if (!string.IsNullOrWhiteSpace(canvas)) parameters["canvas"] = canvas;
        if (!string.IsNullOrWhiteSpace(scene)) parameters["scene"] = scene;
        if (!string.IsNullOrWhiteSpace(interactable)) parameters["interactable"] = ParseOptionalBool(interactable, nameof(interactable));
        if (!string.IsNullOrWhiteSpace(active)) parameters["active"] = ParseOptionalBool(active, nameof(active));
        if (includeInactive) parameters["includeInactive"] = true;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.UiFind,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateGetRequest(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        return new CommandRequest
        {
            Command = WellKnownCommands.UiGet,
            Parameters = new JsonObject { ["id"] = id }
        };
    }

    internal static CommandRequest CreateToggleRequest(string id, string value, string mode = "auto")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        var parsedValue = ParseOptionalBool(value, nameof(value));
        var normalizedMode = ParseInteractionMode(mode, nameof(mode));

        return new CommandRequest
        {
            Command = WellKnownCommands.UiToggle,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["value"] = parsedValue,
                ["mode"] = normalizedMode
            }
        };
    }

    internal static CommandRequest CreateInputRequest(string id, string text, string mode = "auto")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var normalizedMode = ParseInteractionMode(mode, nameof(mode));

        return new CommandRequest
        {
            Command = WellKnownCommands.UiInput,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["text"] = text,
                ["mode"] = normalizedMode
            }
        };
    }

    // Phase I-1: UGUI Enhancement
    public static void Scroll(string project, string id, string? x = null, string? y = null, string mode = "auto", bool json = false)
    {
        var request = CreateScrollRequest(id, x, y, mode);
        CommandRunner.Execute(project, request, json);
    }

    public static void SliderSet(string project, string id, string value, string mode = "auto", bool json = false)
    {
        var request = CreateSliderSetRequest(id, value, mode);
        CommandRunner.Execute(project, request, json);
    }

    public static void DropdownSet(string project, string id, string value, string mode = "auto", bool json = false)
    {
        var request = CreateDropdownSetRequest(id, value, mode);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateScrollRequest(string id, string? x, string? y, string mode = "auto")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));

        var normalizedMode = ParseInteractionMode(mode, nameof(mode));
        var parameters = new JsonObject
        {
            ["id"] = id,
            ["mode"] = normalizedMode
        };
        if (!string.IsNullOrEmpty(x)) parameters["x"] = x;
        if (!string.IsNullOrEmpty(y)) parameters["y"] = y;

        return new CommandRequest
        {
            Command = WellKnownCommands.UiScroll,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateSliderSetRequest(string id, string value, string mode = "auto")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("value must not be empty", nameof(value));

        var normalizedMode = ParseInteractionMode(mode, nameof(mode));
        return new CommandRequest
        {
            Command = WellKnownCommands.UiSliderSet,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["value"] = value,
                ["mode"] = normalizedMode
            }
        };
    }

    internal static CommandRequest CreateDropdownSetRequest(string id, string value, string mode = "auto")
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("value must not be empty", nameof(value));

        var normalizedMode = ParseInteractionMode(mode, nameof(mode));
        return new CommandRequest
        {
            Command = WellKnownCommands.UiDropdownSet,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["value"] = value,
                ["mode"] = normalizedMode
            }
        };
    }

    internal static bool ParseOptionalBool(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} must not be empty", parameterName);

        if (bool.TryParse(value, out var parsed))
            return parsed;

        throw new ArgumentException($"{parameterName} must be 'true' or 'false'", parameterName);
    }

    internal static string ParseInteractionMode(string? mode, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "auto";

        var normalized = mode.Trim().ToLowerInvariant();
        if (normalized is "auto" or "edit" or "play")
            return normalized;

        throw new ArgumentException($"{parameterName} must be 'auto', 'edit', or 'play'", parameterName);
    }

    internal static async Task<int> ExecuteInteractiveAsync(
        string project,
        CommandRequest request,
        bool json,
        Func<string, bool>? isProjectLocked = null,
        Func<string, CancellationToken, Task<bool>>? probeIpcAsync = null,
        Func<string, CommandRequest, bool, bool, Task<int>>? executeAsync = null,
        CancellationToken ct = default)
    {
        var readiness = await EnsureInteractiveEditorReadyAsync(
            project,
            isProjectLocked ?? (path => PlatformFactory.Create().IsProjectLocked(path)),
            probeIpcAsync ?? ProbeIpcAsync,
            InteractiveUiProbeAttempts,
            InteractiveUiProbeDelayMs,
            ct);

        if (readiness == UiInteractiveReadinessResult.TimedOut)
        {
            var response = CreateInteractiveReadinessFailureResponse(project, request.Command);
            CommandRunner.PrintResponse(project, response, json);
            return CommandRunner.GetExitCode(response);
        }

        return await (executeAsync ?? CommandRunner.ExecuteAsync)(project, request, json, false);
    }

    internal static async Task<UiInteractiveReadinessResult> EnsureInteractiveEditorReadyAsync(
        string project,
        Func<string, bool> isProjectLocked,
        Func<string, CancellationToken, Task<bool>> probeIpcAsync,
        int maxAttempts,
        int delayMs,
        CancellationToken ct = default)
    {
        if (!isProjectLocked(project))
            return UiInteractiveReadinessResult.ContinueWithoutReadyIpc;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (await probeIpcAsync(project, ct).ConfigureAwait(false))
                return UiInteractiveReadinessResult.Ready;

            if (!isProjectLocked(project))
                return UiInteractiveReadinessResult.ContinueWithoutReadyIpc;

            if (attempt < maxAttempts - 1)
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
        }

        return UiInteractiveReadinessResult.TimedOut;
    }

    internal static CommandResponse CreateInteractiveReadinessFailureResponse(string project, string command)
    {
        var cliCommand = GetCliCommandName(command);
        return new CommandResponse
        {
            StatusCode = StatusCode.Busy,
            Success = false,
            Message = $"Unity Editor is still compiling or reloading. `{cliCommand}` works best with a running Editor and IPC ready.",
            Data = new JsonObject
            {
                ["command"] = cliCommand,
                ["requiresIpcReady"] = true,
                ["recommendedAction"] = $"Run `unityctl status --project \"{project}\" --wait` and retry `{cliCommand}` after the Editor reports Ready.",
                ["followUpAction"] = "Keep the Unity Editor open and let IPC reconnect before retrying this UI interaction command. Batch fallback is not guaranteed for UI interaction commands."
            }
        };
    }

    private static async Task<bool> ProbeIpcAsync(string project, CancellationToken ct)
    {
        await using var ipc = new IpcTransport(project);
        return await ipc.ProbeAsync(ct).ConfigureAwait(false);
    }

    private static string GetCliCommandName(string command)
    {
        return command switch
        {
            WellKnownCommands.UiToggle => "ui toggle",
            WellKnownCommands.UiInput => "ui input",
            WellKnownCommands.UiScroll => "ui scroll",
            WellKnownCommands.UiSliderSet => "ui slider-set",
            WellKnownCommands.UiDropdownSet => "ui dropdown-set",
            _ => command
        };
    }
}

internal enum UiInteractiveReadinessResult
{
    Ready,
    ContinueWithoutReadyIpc,
    TimedOut
}
