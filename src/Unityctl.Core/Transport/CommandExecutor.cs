using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Retry;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Transport;
using System.Text.Json.Nodes;
using Unityctl.Shared.Models;

namespace Unityctl.Core.Transport;

/// <summary>
/// Transport orchestrator: selects IPC (fast) → Batch (fallback),
/// applies retry policy, records to flight log.
/// </summary>
public sealed class CommandExecutor
{
    private const int LockedProjectProbeRetries = 6;
    private static readonly TimeSpan LockedProjectProbeDelay = TimeSpan.FromSeconds(2);

    private readonly IPlatformServices _platform;
    private readonly UnityEditorDiscovery _discovery;
    private readonly RetryPolicy _retryPolicy;
    private readonly UnityProcessDetector _processDetector;

    public CommandExecutor(IPlatformServices platform, UnityEditorDiscovery discovery, RetryPolicy? retryPolicy = null)
    {
        _platform = platform;
        _discovery = discovery;
        _retryPolicy = retryPolicy ?? new RetryPolicy();
        _processDetector = new UnityProcessDetector(_platform);
    }

    /// <summary>
    /// Execute a command against a Unity project, selecting the best transport.
    /// Priority: IPC (if editor running) → Batch (spawn new editor).
    /// </summary>
    public async Task<CommandResponse> ExecuteAsync(
        string projectPath,
        CommandRequest request,
        bool retry = false,
        CancellationToken ct = default)
    {
        projectPath = Path.GetFullPath(projectPath);

        if (retry)
        {
            return await _retryPolicy.ExecuteWithRetryAsync(
                () => ExecuteOnceAsync(projectPath, request, ct), ct);
        }

        return await ExecuteOnceAsync(projectPath, request, ct);
    }

    private async Task<CommandResponse> ExecuteOnceAsync(
        string projectPath,
        CommandRequest request,
        CancellationToken ct)
    {
        // IPC first: probe checks if an Editor with IPC server is running
        await using var ipc = new IpcTransport(projectPath, _platform, _processDetector);
        var process = _processDetector.FindProcessForProject(projectPath);
        var interactiveProcess = _processDetector.FindInteractiveProcessForProject(projectPath);
        var editor = _discovery.FindEditorForProject(projectPath);
        var projectLocked = _platform.IsProjectLocked(projectPath);
        if (await ipc.ProbeAsync(ct))
        {
            var response = await ipc.SendAsync(request, ct);
            return AttachTargetMetadata(response, projectPath, "ipc", editor, process, projectLocked, null);
        }

        if (projectLocked && interactiveProcess != null)
        {
            for (var attempt = 0; attempt < LockedProjectProbeRetries; attempt++)
            {
                await Task.Delay(LockedProjectProbeDelay, ct);
                if (await ipc.ProbeAsync(ct))
                {
                    var reconnected = await ipc.SendAsync(request, ct);
                    return AttachTargetMetadata(reconnected, projectPath, "ipc", editor, process, projectLocked, "ipc-became-ready-after-wait");
                }
            }

            var pending = BuildInteractiveBusyResponse(projectPath, request.Command);
            return AttachTargetMetadata(pending, projectPath, null, editor, interactiveProcess, projectLocked, "editor-running-ipc-not-ready");
        }

        if (projectLocked && process != null)
        {
            var pending = BuildHeadlessBusyResponse(request.Command, process);
            return AttachTargetMetadata(pending, projectPath, null, editor, process, projectLocked, "headless-process-holding-lock");
        }

        // Fallback: batch transport (only when probe fails, NOT on SendAsync failure)
        await using var batch = new BatchTransport(_platform, _discovery, projectPath);
        var batchResponse = await batch.SendAsync(request, ct);
        return AttachTargetMetadata(batchResponse, projectPath, "batch", editor, process, projectLocked, "ipc-probe-failed");
    }

    internal static CommandResponse AttachTargetMetadata(
        CommandResponse response,
        string projectPath,
        string? transport,
        UnityEditorInfo? editor,
        UnityProcessInfo? process,
        bool projectLocked,
        string? fallbackReason)
    {
        response.Data ??= new JsonObject();
        response.Data["target"] = BuildTargetMetadata(projectPath, transport, editor, process, projectLocked, fallbackReason);
        return response;
    }

    internal static JsonObject BuildTargetMetadata(
        string projectPath,
        string? transport,
        UnityEditorInfo? editor,
        UnityProcessInfo? process,
        bool projectLocked,
        string? fallbackReason)
    {
        var target = new JsonObject
        {
            ["projectPath"] = Unityctl.Shared.Constants.NormalizeProjectPath(projectPath),
            ["pipeName"] = Unityctl.Shared.Constants.GetPipeName(projectPath),
            ["projectLocked"] = projectLocked
        };

        if (!string.IsNullOrWhiteSpace(transport))
            target["transport"] = transport;

        if (!string.IsNullOrWhiteSpace(fallbackReason))
            target["fallbackReason"] = fallbackReason;

        if (editor != null)
        {
            target["editorVersion"] = editor.Version;
            target["editorLocation"] = editor.Location;
        }

        if (process != null)
        {
            target["unityPid"] = process.ProcessId;
            target["isRunning"] = true;
            target["isBatchMode"] = process.IsBatchMode;
            target["hasMainWindow"] = process.HasMainWindow;
            target["processKind"] = process.IsInteractiveEditor ? "interactive" : process.IsBatchMode ? "headless" : "background";
        }
        else
        {
            target["isRunning"] = false;
        }

        return target;
    }

    internal static CommandResponse BuildInteractiveBusyResponse(string projectPath, string command)
    {
        var cliCommand = command switch
        {
            WellKnownCommands.ScriptGetErrors => "script get-errors",
            WellKnownCommands.ScriptFindRefs => "script find-refs",
            WellKnownCommands.ScriptRenameSymbol => "script rename-symbol",
            WellKnownCommands.ExecListCallables => "exec list-callables",
            WellKnownCommands.ExecInvoke => "exec invoke",
            WellKnownCommands.UiClick => "ui click",
            WellKnownCommands.UiToggle => "ui toggle",
            WellKnownCommands.UiInput => "ui input",
            _ => command
        };

        if (command is WellKnownCommands.ScriptGetErrors
            or WellKnownCommands.ScriptFindRefs
            or WellKnownCommands.ScriptRenameSymbol
            or WellKnownCommands.ExecListCallables
            or WellKnownCommands.ExecInvoke
            or WellKnownCommands.UiClick
            or WellKnownCommands.UiToggle
            or WellKnownCommands.UiInput)
        {
            var followUpAction = command switch
            {
                WellKnownCommands.ExecListCallables => "Keep the Unity Editor open and let IPC reconnect before retrying `exec list-callables`; it inspects the current AppDomain and is unreliable over batch fallback.",
                WellKnownCommands.ExecInvoke => "Keep the Unity Editor open and let IPC reconnect before retrying `exec invoke`; it resolves currently loaded types/methods from the live AppDomain.",
                WellKnownCommands.UiClick => "Keep the Unity Editor open and let IPC reconnect before retrying this UI interaction command. `ui click` invokes Button.onClick deterministically in Play Mode and does not rely on desktop focus automation.",
                WellKnownCommands.ScriptGetErrors => $"If compilation diagnostics are still missing after Ready, run `unityctl script validate --project \"{projectPath}\" --wait` once to populate the latest compile cache.",
                WellKnownCommands.UiInput => "Keep the Unity Editor open and let IPC reconnect before retrying this UI interaction command. `ui input` sets InputField.text deterministically and does not emulate keystrokes.",
                WellKnownCommands.UiToggle => "Keep the Unity Editor open and let IPC reconnect before retrying this UI interaction command. `ui toggle` sets Toggle.isOn deterministically and does not emulate a pointer click.",
                _ => "Keep the Unity Editor open and let IPC reconnect before retrying this script command. Batch fallback is less reliable for script diagnostics/refactors."
            };

            return new CommandResponse
            {
                StatusCode = StatusCode.Busy,
                Success = false,
                Message = $"Unity Editor is still compiling or reloading. `{cliCommand}` works best with a running Editor and IPC ready.",
                Data = new JsonObject
                {
                    ["command"] = cliCommand,
                    ["requiresIpcReady"] = true,
                    ["recommendedAction"] = $"Run `unityctl status --project \"{projectPath}\" --wait` and retry `{cliCommand}` after the Editor reports Ready.",
                    ["followUpAction"] = followUpAction
                }
            };
        }

        return CommandResponse.Fail(
            StatusCode.Busy,
            "Unity Editor is running but IPC is not ready yet. Wait for compilation/domain reload to finish and retry.");
    }

    internal static CommandResponse BuildHeadlessBusyResponse(string command, UnityProcessInfo process)
    {
        var cliCommand = command switch
        {
            WellKnownCommands.ProjectValidate => "project validate",
            WellKnownCommands.ExecListCallables => "exec list-callables",
            _ => command
        };

        return new CommandResponse
        {
            StatusCode = StatusCode.Busy,
            Success = false,
            Message = $"A headless Unity process is holding the project lock, so `{cliCommand}` cannot wait for IPC readiness.",
            Data = new JsonObject
            {
                ["command"] = cliCommand,
                ["requiresInteractiveEditor"] = true,
                ["recommendedAction"] = $"Wait for the batch/headless Unity process (pid {process.ProcessId}) to exit, or open the project in the interactive Editor before retrying.",
                ["processKind"] = process.IsBatchMode ? "headless" : "background",
                ["unityPid"] = process.ProcessId
            }
        };
    }

    /// <summary>
    /// Subscribe to a streaming channel (requires IPC).
    /// </summary>
    public IAsyncEnumerable<EventEnvelope>? WatchAsync(
        string projectPath, string channel, CancellationToken ct = default)
    {
        // Phase 3C: IPC streaming
        var ipc = new IpcTransport(projectPath);
        return ipc.SubscribeAsync(channel, ct);
    }
}
