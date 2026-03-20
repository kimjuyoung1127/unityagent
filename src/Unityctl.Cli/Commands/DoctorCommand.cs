using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.FlightRecorder;
using Unityctl.Core.Diagnostics;
using Unityctl.Core.Discovery;
using Unityctl.Core.EditorRouting;
using Unityctl.Core.Platform;
using Unityctl.Core.Sessions;
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
        var editors = discovery.FindEditors();
        var editorFound = editors.Count > 0;
        var editorVersion = editors.FirstOrDefault()?.Version ?? "not found";

        var manifestPath = Path.Combine(project, "Packages", "manifest.json");
        var pluginInstalled = false;
        string? pluginSource = null;
        string? pluginSourceKind = null;
        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
                var dependencies = manifest?["dependencies"]?.AsObject();
                if (dependencies != null
                    && dependencies.TryGetPropertyValue(Constants.PluginPackageName, out var sourceNode)
                    && sourceNode is JsonValue sourceValue)
                {
                    pluginSource = sourceValue.TryGetValue<string>(out var stringValue)
                        ? stringValue
                        : sourceNode.ToJsonString();
                    pluginInstalled = !string.IsNullOrWhiteSpace(pluginSource);
                    pluginSourceKind = ClassifyPluginSource(pluginSource);
                }
            }
            catch
            {
                // Keep doctor resilient even when manifest parsing fails.
            }
        }

        var ipcConnected = false;
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
        var buildState = GetBuildStateInfo(project);

        var structuredDiagnostics = EditorLogDiagnostics.GetStructuredDiagnostics();

        return new DoctorSnapshot
        {
            PipeName = pipeName,
            EditorFound = editorFound,
            EditorVersion = editorVersion,
            PluginInstalled = pluginInstalled,
            PluginSource = pluginSource,
            PluginSourceKind = pluginSourceKind,
            IpcConnected = ipcConnected,
            ProjectLocked = projectLocked,
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
                ["sourceKind"] = result.PluginSourceKind
            },
            ["ipc"] = new JsonObject { ["connected"] = result.IpcConnected, ["pipeName"] = result.PipeName },
            ["projectLock"] = new JsonObject
            {
                ["locked"] = result.ProjectLocked,
                ["lockFilePath"] = result.LockFilePath,
                ["lockFileExists"] = result.LockFileExists
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
        }

        lines.Add(result.IpcConnected
            ? $"  \u2713 IPC connected (pipe: {result.PipeName})"
            : $"  \u2717 IPC probe failed (pipe: {result.PipeName})");

        lines.Add(result.ProjectLocked && analysis.LockSeverity == "informational"
            ? $"  \u2713 Project lock detected but informational: {result.LockFilePath}"
            : result.ProjectLocked
            ? $"  \u26a0 Project lock detected: {result.LockFilePath}"
            : $"  \u2713 Project lock: not detected ({result.LockFilePath})");

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

    private static string? ClassifyPluginSource(string? pluginSource)
    {
        if (string.IsNullOrWhiteSpace(pluginSource))
            return null;

        if (pluginSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return "local-file";

        if (pluginSource.Contains(".git", StringComparison.OrdinalIgnoreCase)
            || pluginSource.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            return "git";

        if (pluginSource.Contains("://", StringComparison.Ordinal))
            return "remote-url";

        return "unknown";
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(value);
        return array;
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
