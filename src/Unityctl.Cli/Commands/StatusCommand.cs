using Unityctl.Cli.Execution;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class StatusCommand
{
    private const int DomainReloadMaxAttempts = 30;
    private const int DomainReloadDelayMs = 3000;

    public static void Execute(string? project = null, bool wait = false, bool json = false)
    {
        if (wait)
        {
            if (!CommandRunner.TryResolveProject(project, out var resolvedProject, out var failureResponse))
            {
                CommandRunner.PrintResponse(failureResponse!, json);
                Environment.Exit(1);
                return;
            }

            var exitCode = ExecuteWithSmartWaitAsync(resolvedProject, json).GetAwaiter().GetResult();
            Environment.Exit(exitCode);
            return;
        }

        var request = new CommandRequest { Command = WellKnownCommands.Status };
        CommandRunner.Execute(project, request, json, retry: false);
    }

    internal static async Task<int> ExecuteWithSmartWaitAsync(
        string project,
        bool json,
        Func<string, bool>? isProjectLocked = null,
        Func<string, CancellationToken, Task<bool>>? probeIpcAsync = null,
        CancellationToken ct = default)
    {
        var lockCheck = isProjectLocked ?? (path => PlatformFactory.Create().IsProjectLocked(path));
        var probe = probeIpcAsync ?? DefaultProbeIpcAsync;

        // Phase 1: wait for IPC to become ready (handles domain reloads)
        for (var attempt = 0; attempt < DomainReloadMaxAttempts; attempt++)
        {
            if (await probe(project, ct).ConfigureAwait(false))
            {
                // IPC ready — execute status normally
                var request = new CommandRequest { Command = WellKnownCommands.Status };
                return await CommandRunner.ExecuteAsync(project, request, json, retry: false);
            }

            // If lockfile is gone, Unity died — no point waiting
            if (!lockCheck(project))
            {
                var request = new CommandRequest { Command = WellKnownCommands.Status };
                return await CommandRunner.ExecuteAsync(project, request, json, retry: false);
            }

            // Still locked, IPC not ready — domain reload in progress
            if (attempt < DomainReloadMaxAttempts - 1)
            {
                Console.Error.WriteLine(
                    $"[unityctl] Waiting for Unity IPC... ({attempt + 1}/{DomainReloadMaxAttempts})");
                await Task.Delay(DomainReloadDelayMs, ct).ConfigureAwait(false);
            }
        }

        // Timed out waiting — run one final attempt with normal path
        var finalRequest = new CommandRequest { Command = WellKnownCommands.Status };
        return await CommandRunner.ExecuteAsync(project, finalRequest, json, retry: false);
    }

    private static async Task<bool> DefaultProbeIpcAsync(string project, CancellationToken ct)
    {
        await using var ipc = new IpcTransport(project);
        return await ipc.ProbeAsync(ct).ConfigureAwait(false);
    }
}
