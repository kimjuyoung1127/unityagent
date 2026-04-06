using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.FlightRecorder;
using Unityctl.Core.Diagnostics;
using Unityctl.Core.Discovery;
using Unityctl.Core.EditorRouting;
using Unityctl.Core.Platform;
using Unityctl.Core.Setup;
using Unityctl.Core.Sessions;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class DoctorCommand
{
    public static void Execute(string? project = null, bool json = false)
    {
        if (!CommandRunner.TryResolveProject(project, out var resolvedProject, out var failureResponse))
        {
            CommandRunner.PrintResponse(failureResponse!, json);
            Environment.ExitCode = 1;
            return;
        }

        var result = Diagnose(resolvedProject);
        var analysis = Analyze(resolvedProject, result);
        var selection = new EditorSelectionStore().GetCurrent();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(BuildJson(resolvedProject, result, analysis, selection), new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.Write(RenderText(resolvedProject, result, analysis, selection));
        }

        Environment.ExitCode = result.IpcConnected ? 0 : 1;
    }

    internal static DoctorSnapshot Diagnose(string project)
    {
        var pipeName = Constants.GetPipeName(project);

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var processDetector = new UnityProcessDetector(platform);
        var editors = discovery.FindEditors();
        var editorFound = editors.Count > 0;
        var editorVersion = editors.FirstOrDefault()?.Version ?? "not found";

        var installInfo = PluginInstallationInspector.Inspect(project);
        var pluginInstalled = installInfo.PluginInstalled;
        var pluginSource = installInfo.EffectiveSource;
        var pluginSourceKind = installInfo.EffectiveSourceKind;
        var bridgeEnabled = installInfo.BridgeEnabled;
        var embeddedPath = installInfo.EmbeddedPath;

        var ipcConnected = false;
        bool? isCompiling = null;
        bool? isDomainReloading = null;
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            pipe.Connect(1000);
            ipcConnected = true;
        }
        catch
        {
            // Expected when editor is not reachable.
        }

        var lockFilePath = Path.Combine(Path.GetFullPath(project), "Temp", "UnityLockfile");
        var lockFileExists = File.Exists(lockFilePath);
        var projectLocked = platform.IsProjectLocked(project);
        var runningProcesses = processDetector.FindProcessesForProject(project);
        var interactiveProcesses = processDetector.FindInteractiveProcessesForProject(project);
        var buildState = GetBuildStateInfo(project);

        var structuredDiagnostics = EditorLogDiagnostics.GetStructuredDiagnostics();
        var unityctlSignals = EditorLogDiagnostics.GetUnityctlSignals();
        var bridgeLoaded = ipcConnected
                           || unityctlSignals.BridgeInitialized
                           || unityctlSignals.CommandRegistryInitialized;
        var ipcPipePresent = ipcConnected
                             || string.Equals(unityctlSignals.LastIpcServerState, "started", StringComparison.OrdinalIgnoreCase);

        if (ipcConnected)
        {
            IpcTransport? ipc = null;
            try
            {
                ipc = new IpcTransport(project);
                var statusResponse = ipc.SendAsync(new CommandRequest { Command = WellKnownCommands.Status }).GetAwaiter().GetResult();
                if (statusResponse.Success)
                {
                    isCompiling = statusResponse.Data?["isCompiling"]?.GetValue<bool>();
                    var isUpdating = statusResponse.Data?["isUpdating"]?.GetValue<bool>();
                    var isEnteringPlayMode = statusResponse.Data?["isEnteringPlayMode"]?.GetValue<bool>();
                    isDomainReloading = (isUpdating ?? false) || (isEnteringPlayMode ?? false);
                }
            }
            catch
            {
                // Keep doctor resilient.
            }
            finally
            {
                if (ipc != null)
                    ipc.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        else if (projectLocked && bridgeLoaded)
        {
            isCompiling = false;
            isDomainReloading = true;
        }

        return new DoctorSnapshot
        {
            PipeName = pipeName,
            EditorFound = editorFound,
            EditorVersion = editorVersion,
            PluginInstalled = pluginInstalled,
            PluginSource = pluginSource,
            PluginSourceKind = pluginSourceKind,
            BridgeEnabled = bridgeEnabled,
            EmbeddedPath = embeddedPath,
            IpcConnected = ipcConnected,
            IpcPipePresent = ipcPipePresent,
            BridgeLoaded = bridgeLoaded,
            ProjectLocked = projectLocked,
            RunningProcessCount = runningProcesses.Count,
            InteractiveProcessCount = interactiveProcesses.Count,
            HeadlessProcessCount = runningProcesses.Count(process => process.IsBatchMode),
            IsCompiling = isCompiling,
            IsDomainReloading = isDomainReloading,
            LockFilePath = lockFilePath,
            LockFileExists = lockFileExists,
            BuildStateDirectory = GetBuildStateDirectory(project),
            BuildStateExists = buildState.exists,
            BuildStateCount = buildState.count,
            BuildStateOldestAgeMinutes = buildState.oldestAgeMinutes,
            LogPath = EditorLogDiagnostics.GetEditorLogPath(),
            EditorLogErrors = structuredDiagnostics?.Errors ?? [],
            UnityctlLogLines = structuredDiagnostics?.UnityctlLines ?? [],
            HumanDiagnostics = EditorLogDiagnostics.GetRecentDiagnostics()
        };
    }

    internal static DoctorAnalysis Analyze(
        string project,
        DoctorSnapshot snapshot,
        FlightLog? flightLog = null,
        SessionManager? sessionManager = null)
    {
        var recentEntries = (flightLog ?? new FlightLog()).Query(new FlightQuery { Last = 200 });
        var sessions = (sessionManager ?? new SessionManager(new NdjsonSessionStore())).ListAsync().GetAwaiter().GetResult();
        return DoctorAnalyzer.Analyze(snapshot, project, recentEntries, sessions.ToList());
    }

    private static (bool exists, int count, double oldestAgeMinutes) GetBuildStateInfo(string project)
    {
        var directory = GetBuildStateDirectory(project);
        if (!Directory.Exists(directory))
            return (false, 0, 0);

        var files = Directory.GetFiles(directory, "*.json");
        if (files.Length == 0)
            return (false, 0, 0);

        var oldestWriteTimeUtc = files
            .Select(File.GetLastWriteTimeUtc)
            .OrderBy(ts => ts)
            .FirstOrDefault();

        var oldestAgeMinutes = (DateTime.UtcNow - oldestWriteTimeUtc).TotalMinutes;
        return (true, files.Length, Math.Max(0, oldestAgeMinutes));
    }

    private static string GetBuildStateDirectory(string project)
    {
        return Path.Combine(Path.GetFullPath(project), "Library", "Unityctl", "build-state");
    }

    internal static bool ShouldAutoDiagnose(CommandResponse response)
    {
        if (response.Success)
            return false;

        return response.StatusCode switch
        {
            StatusCode.ProjectLocked => true,
            StatusCode.Busy => true,
            StatusCode.PluginNotInstalled => true,
            StatusCode.CommandNotFound => true,
            StatusCode.UnknownError => LooksTransportOrReloadRelated(response.Message),
            _ => false
        };
    }

    internal static string RenderAutoDiagnosis(string project)
    {
        var result = Diagnose(project);
        var analysis = Analyze(project, result);
        return RenderAutoDiagnosis(result, analysis);
    }

    internal static string RenderAutoDiagnosis(DoctorSnapshot result, DoctorAnalysis analysis)
    {
        var lines = new List<string>
        {
            "  Doctor summary:",
            $"    Classification: {analysis.Classification} — {analysis.Summary}",
            result.EditorFound
                ? $"    \u2713 Unity Editor found: {result.EditorVersion}"
                : "    \u2717 Unity Editor not found",
            result.PluginInstalled
                ? $"    \u2713 Plugin installed: {Constants.PluginPackageName} ({result.PluginSourceKind ?? "unknown"})"
                : "    \u2717 Plugin not installed",
            result.IpcConnected
                ? $"    \u2713 IPC connected ({result.PipeName})"
                : $"    \u2717 IPC probe failed ({result.PipeName})",
            result.ProjectLocked && analysis.LockSeverity == "informational"
                ? $"    \u2713 Project lock detected but informational ({result.LockFilePath})"
                : result.ProjectLocked
                ? $"    \u26a0 Project lock detected ({result.LockFilePath})"
                : $"    \u2713 Project lock not detected ({result.LockFilePath})"
        };

        if (!string.IsNullOrWhiteSpace(result.PluginSource))
            lines.Add($"    Plugin source: {result.PluginSource}");

        if (analysis.RecentFailures.Count > 0)
            lines.Add($"    Recent failure: {FormatActivity(analysis.RecentFailures[0])}");

        if (analysis.Recommendations.Count > 0)
        {
            lines.Add("    Next step:");
            lines.Add($"      - {analysis.Recommendations[0]}");
        }

        if (result.HumanDiagnostics != null)
        {
            lines.Add(string.Empty);
            lines.Add(result.HumanDiagnostics.TrimEnd());
        }
        else if (result.LogPath != null)
        {
            lines.Add($"  Log: {result.LogPath}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    internal static JsonObject BuildJson(string project, DoctorSnapshot result, DoctorAnalysis analysis, EditorSelection? selection = null)
    {
        var results = new JsonObject
        {
            ["editor"] = new JsonObject { ["found"] = result.EditorFound, ["version"] = result.EditorVersion },
            ["plugin"] = new JsonObject
            {
                ["installed"] = result.PluginInstalled,
                ["source"] = result.PluginSource,
                ["sourceKind"] = result.PluginSourceKind,
                ["bridgeEnabled"] = result.BridgeEnabled,
                ["embeddedPath"] = result.EmbeddedPath,
                ["bridgeLoaded"] = result.BridgeLoaded
            },
            ["ipc"] = new JsonObject
            {
                ["connected"] = result.IpcConnected,
                ["pipeName"] = result.PipeName,
                ["ipcPipePresent"] = result.IpcPipePresent
            },
            ["projectLock"] = new JsonObject
            {
                ["locked"] = result.ProjectLocked,
                ["lockFilePath"] = result.LockFilePath,
                ["lockFileExists"] = result.LockFileExists
            },
            ["processes"] = new JsonObject
            {
                ["runningProcessCount"] = result.RunningProcessCount,
                ["interactiveProcessCount"] = result.InteractiveProcessCount,
                ["headlessProcessCount"] = result.HeadlessProcessCount,
                ["interactiveEditorDetected"] = result.InteractiveProcessCount > 0
            },
            ["buildState"] = new JsonObject
            {
                ["directory"] = result.BuildStateDirectory,
                ["exists"] = result.BuildStateExists,
                ["count"] = result.BuildStateCount,
                ["oldestAgeMinutes"] = result.BuildStateOldestAgeMinutes
            },
            ["editorLog"] = new JsonObject
            {
                ["errors"] = ToJsonArray(result.EditorLogErrors),
                ["unityctl"] = ToJsonArray(result.UnityctlLogLines)
            },
            ["logPath"] = result.LogPath,
            ["summary"] = new JsonObject
            {
                ["classification"] = analysis.Classification,
                ["message"] = analysis.Summary,
                ["lockSeverity"] = analysis.LockSeverity
            },
            ["readiness"] = new JsonObject
            {
                ["projectLocked"] = result.ProjectLocked,
                ["ipcPipePresent"] = result.IpcPipePresent,
                ["bridgeLoaded"] = result.BridgeLoaded,
                ["isDomainReloading"] = result.IsDomainReloading == null ? null : JsonValue.Create(result.IsDomainReloading.Value),
                ["isCompiling"] = result.IsCompiling == null ? null : JsonValue.Create(result.IsCompiling.Value),
                ["recommendedNextCommand"] = analysis.RecommendedNextCommand
            },
            ["recentActivity"] = new JsonObject
            {
                ["lastSuccess"] = analysis.LastSuccess == null ? null : BuildActivityJson(analysis.LastSuccess),
                ["recentFailures"] = new JsonArray(analysis.RecentFailures.Select(BuildActivityJson).ToArray()),
                ["repeatedStatusCodes"] = new JsonArray(analysis.RepeatedStatusCodes.Select(BuildStatusCodeSummaryJson).ToArray()),
                ["batchFallbackSignature"] = analysis.HasBatchFallbackSignature,
                ["pipeErrorsDetected"] = analysis.HasRecentPipeErrors
            },
            ["activeSessions"] = new JsonArray(analysis.ActiveSessions.Select(BuildSessionJson).ToArray()),
            ["recommendations"] = ToJsonArray(analysis.Recommendations)
        };

        if (selection != null)
        {
            results["selection"] = new JsonObject
            {
                ["projectPath"] = selection.ProjectPath,
                ["unityPid"] = selection.UnityPid,
                ["selectionMode"] = selection.SelectionMode,
                ["selectedAt"] = selection.SelectedAt,
                ["matchesRequestedProject"] = MatchesProjectPath(selection.ProjectPath, project)
            };
        }

        return results;
    }

    internal static string RenderText(string project, DoctorSnapshot result, DoctorAnalysis analysis, EditorSelection? selection = null)
    {
        var lines = new List<string>
        {
            $"unityctl doctor — project: {project}",
            string.Empty,
            $"  Classification: {analysis.Classification} — {analysis.Summary}",
            result.EditorFound
                ? $"  \u2713 Unity Editor found: {result.EditorVersion}"
                : "  \u2717 Unity Editor not found",
            result.PluginInstalled
                ? $"  \u2713 Plugin installed: {Constants.PluginPackageName}"
                : "  \u2717 Plugin not installed (run: unityctl init)"
        };

        if (result.PluginInstalled)
        {
            lines.Add($"    Source kind: {result.PluginSourceKind ?? "unknown"}");
            if (!string.IsNullOrWhiteSpace(result.PluginSource))
                lines.Add($"    Source: {result.PluginSource}");
            lines.Add($"    Bridge enabled: {result.BridgeEnabled}");
            if (!string.IsNullOrWhiteSpace(result.EmbeddedPath))
                lines.Add($"    Embedded path: {result.EmbeddedPath}");
        }

        lines.Add(result.IpcConnected
            ? $"  \u2713 IPC connected (pipe: {result.PipeName})"
            : $"  \u2717 IPC probe failed (pipe: {result.PipeName})");
        lines.Add(result.BridgeLoaded
            ? "  \u2713 Bridge loaded"
            : "  \u2717 Bridge not confirmed as loaded");
        lines.Add($"  \u2713 IPC pipe present: {(result.IpcPipePresent ? "yes" : "no")}");

        lines.Add(result.ProjectLocked && analysis.LockSeverity == "informational"
            ? $"  \u2713 Project lock detected but informational: {result.LockFilePath}"
            : result.ProjectLocked
            ? $"  \u26a0 Project lock detected: {result.LockFilePath}"
            : $"  \u2713 Project lock: not detected ({result.LockFilePath})");
        lines.Add($"  \u2713 Running processes: {result.RunningProcessCount} total / {result.InteractiveProcessCount} interactive / {result.HeadlessProcessCount} headless");

        if (result.IsCompiling.HasValue || result.IsDomainReloading.HasValue)
        {
            lines.Add($"  \u2713 isCompiling: {result.IsCompiling?.ToString() ?? "unknown"}");
            lines.Add($"  \u2713 isDomainReloading: {result.IsDomainReloading?.ToString() ?? "unknown"}");
        }

        lines.Add(result.BuildStateExists
            ? $"  \u2713 Build transition state: {result.BuildStateCount} file(s), oldest {result.BuildStateOldestAgeMinutes:n1} min"
            : $"  \u2713 Build transition state: none ({result.BuildStateDirectory})");

        if (selection != null)
        {
            var matchLabel = MatchesProjectPath(selection.ProjectPath, project)
                ? "matches requested project"
                : "differs from requested project";
            lines.Add($"  \u2713 Current selection: {selection.ProjectPath} ({matchLabel})");
            if (selection.UnityPid.HasValue)
                lines.Add($"    Selected PID: {selection.UnityPid.Value}");
            if (!string.IsNullOrWhiteSpace(selection.SelectionMode))
                lines.Add($"    Selection mode: {selection.SelectionMode}");
        }

        if (analysis.LastSuccess != null || analysis.RecentFailures.Count > 0 || analysis.RepeatedStatusCodes.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Recent activity:");
            if (analysis.LastSuccess != null)
                lines.Add($"    Last success: {FormatActivity(analysis.LastSuccess)}");
            foreach (var failure in analysis.RecentFailures)
                lines.Add($"    Recent failure: {FormatActivity(failure)}");
            foreach (var statusCode in analysis.RepeatedStatusCodes)
                lines.Add($"    Repeated status: [{statusCode.StatusCode}] x{statusCode.Count} ({string.Join(", ", statusCode.Operations)})");
            if (analysis.HasBatchFallbackSignature)
                lines.Add("    Batch fallback signature detected: repeated 'no response file' failures");
            if (analysis.HasRecentPipeErrors)
                lines.Add("    Recent Unity log lines include IPC/pipe errors");
        }

        if (analysis.ActiveSessions.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Active sessions:");
            foreach (var session in analysis.ActiveSessions)
            {
                var staleLabel = session.StaleSuspected ? " stale-suspected" : string.Empty;
                lines.Add($"    {ShortenId(session.Id)} {session.Command} ({session.State}{staleLabel})");
            }
        }

        if (analysis.Recommendations.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("  Recommended next steps:");
            for (var i = 0; i < analysis.Recommendations.Count; i++)
                lines.Add($"    {i + 1}. {analysis.Recommendations[i]}");
            if (!string.IsNullOrWhiteSpace(analysis.RecommendedNextCommand))
                lines.Add($"    Next command: {analysis.RecommendedNextCommand}");
        }

        if (result.HumanDiagnostics != null)
        {
            lines.Add(string.Empty);
            lines.Add(result.HumanDiagnostics.TrimEnd());
        }
        else if (!result.IpcConnected)
        {
            lines.Add(string.Empty);
            lines.Add("  No compilation errors in Editor.log");
            lines.Add("  Possible causes: Unity not running, domain reload in progress, project lock held by another process, or plugin import/compile not finished");
        }

        if (result.LogPath != null && result.HumanDiagnostics == null)
            lines.Add($"  Log: {result.LogPath}");

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static bool LooksTransportOrReloadRelated(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("IPC", StringComparison.OrdinalIgnoreCase)
            || message.Contains("pipe", StringComparison.OrdinalIgnoreCase)
            || message.Contains("reload", StringComparison.OrdinalIgnoreCase)
            || message.Contains("domain", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        return new JsonArray(values.Select(value => JsonValue.Create(value)).ToArray());
    }

    private static JsonObject BuildActivityJson(DoctorActivity activity)
    {
        return new JsonObject
        {
            ["timestamp"] = activity.Timestamp,
            ["operation"] = activity.Operation,
            ["statusCode"] = activity.StatusCode,
            ["durationMs"] = activity.DurationMs,
            ["error"] = activity.Error,
            ["success"] = activity.Success
        };
    }

    private static JsonObject BuildStatusCodeSummaryJson(DoctorStatusCodeSummary summary)
    {
        return new JsonObject
        {
            ["statusCode"] = summary.StatusCode,
            ["count"] = summary.Count,
            ["operations"] = new JsonArray(summary.Operations.Select(operation => JsonValue.Create(operation)).ToArray())
        };
    }

    private static JsonObject BuildSessionJson(DoctorSessionSummary session)
    {
        return new JsonObject
        {
            ["id"] = session.Id,
            ["command"] = session.Command,
            ["state"] = session.State,
            ["createdAt"] = session.CreatedAt,
            ["updatedAt"] = session.UpdatedAt,
            ["errorMessage"] = session.ErrorMessage,
            ["durationMs"] = session.DurationMs,
            ["staleSuspected"] = session.StaleSuspected
        };
    }

    private static string FormatActivity(DoctorActivity activity)
    {
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(activity.Timestamp)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss");
        var errorText = string.IsNullOrWhiteSpace(activity.Error) ? string.Empty : $" — {activity.Error}";
        return $"{timestamp} {activity.Operation} [{activity.StatusCode}] {activity.DurationMs}ms{errorText}";
    }

    private static string ShortenId(string id)
    {
        return id.Length > 8 ? id[..8] : id;
    }

    private static bool MatchesProjectPath(string? candidatePath, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(projectPath))
            return false;

        if (string.Equals(candidatePath, projectPath, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            return string.Equals(
                Constants.NormalizeProjectPath(candidatePath),
                Constants.NormalizeProjectPath(projectPath),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
