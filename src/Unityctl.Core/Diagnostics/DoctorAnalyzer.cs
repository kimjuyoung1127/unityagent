using System.Diagnostics;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.Diagnostics;

internal static class DoctorAnalyzer
{
    public static DoctorAnalysis Analyze(
        DoctorSnapshot snapshot,
        string projectPath,
        IReadOnlyList<FlightEntry>? recentEntries,
        IReadOnlyList<Session>? sessions)
    {
        recentEntries ??= [];
        sessions ??= [];

        var matchingEntries = recentEntries
            .Where(entry => MatchesProjectPath(entry.Project, projectPath))
            .OrderByDescending(entry => entry.Timestamp)
            .Take(20)
            .ToList();

        var matchingSessions = sessions
            .Where(session => MatchesProjectPath(session.ProjectPath, projectPath))
            .Where(session => session.State is SessionState.Created or SessionState.Running)
            .OrderByDescending(session => ParseIso(session.UpdatedAt) ?? ParseIso(session.CreatedAt) ?? DateTimeOffset.MinValue)
            .Take(3)
            .Select(ToSessionSummary)
            .ToList();

        var lastSuccess = matchingEntries
            .FirstOrDefault(IsSuccessfulEntry);

        var recentFailures = matchingEntries
            .Where(entry => !IsSuccessfulEntry(entry))
            .Take(3)
            .Select(ToActivity)
            .ToList();

        var repeatedStatusCodes = matchingEntries
            .Where(entry => !IsSuccessfulEntry(entry))
            .GroupBy(entry => entry.StatusCode)
            .Select(group => new DoctorStatusCodeSummary
            {
                StatusCode = group.Key,
                Count = group.Count(),
                Operations = group.Select(entry => entry.Operation).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(op => op).ToList()
            })
            .Where(group => group.Count > 1)
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.StatusCode)
            .ToList();

        var hasBatchFallbackSignature = matchingEntries.Any(entry =>
            !IsSuccessfulEntry(entry)
            && entry.Error?.Contains("no response file", StringComparison.OrdinalIgnoreCase) == true);

        var hasBusyFailures = matchingEntries.Any(entry =>
            !IsSuccessfulEntry(entry)
            && entry.StatusCode == (int)StatusCode.Busy);

        var hasCommandNotFoundFailures = matchingEntries.Any(entry =>
            !IsSuccessfulEntry(entry)
            && (entry.StatusCode == (int)StatusCode.CommandNotFound
                || entry.Error?.Contains("Unknown command", StringComparison.OrdinalIgnoreCase) == true));

        var hasRecentPipeErrors = snapshot.UnityctlLogLines.Any(line =>
            line.Contains("pipe", StringComparison.OrdinalIgnoreCase)
            || line.Contains("ipc", StringComparison.OrdinalIgnoreCase));

        var analysis = new DoctorAnalysis
        {
            LastSuccess = lastSuccess == null ? null : ToActivity(lastSuccess),
            RecentFailures = recentFailures,
            RepeatedStatusCodes = repeatedStatusCodes,
            HasBatchFallbackSignature = hasBatchFallbackSignature,
            HasRecentPipeErrors = hasRecentPipeErrors,
            ActiveSessions = matchingSessions
        };

        analysis.Classification = Classify(snapshot, hasBusyFailures, hasBatchFallbackSignature, hasCommandNotFoundFailures);
        analysis.LockSeverity = snapshot.ProjectLocked && snapshot.IpcConnected ? "informational" : snapshot.ProjectLocked ? "warning" : "none";
        analysis.Summary = BuildSummary(analysis.Classification, snapshot);
        analysis.Recommendations = BuildRecommendations(analysis.Classification, projectPath, analysis, snapshot);
        analysis.RecommendedNextCommand = ExtractCommandSuggestion(analysis.Recommendations.FirstOrDefault());
        return analysis;
    }

    internal static bool MatchesProjectPath(string? candidatePath, string projectPath)
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

    private static DoctorSessionSummary ToSessionSummary(Session session)
    {
        var staleSuspected = session.CliPid.HasValue && !IsProcessAlive(session.CliPid.Value);
        return new DoctorSessionSummary
        {
            Id = session.Id,
            Command = session.Command,
            State = session.State.ToString(),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            ErrorMessage = session.ErrorMessage,
            DurationMs = session.DurationMs,
            StaleSuspected = staleSuspected
        };
    }

    private static DoctorActivity ToActivity(FlightEntry entry)
    {
        return new DoctorActivity
        {
            Timestamp = entry.Timestamp,
            Operation = entry.Operation,
            StatusCode = entry.StatusCode,
            DurationMs = entry.DurationMs,
            Error = entry.Error,
            Success = IsSuccessfulEntry(entry)
        };
    }

    private static bool IsSuccessfulEntry(FlightEntry entry)
    {
        if (entry.ExitCode.HasValue)
            return entry.ExitCode.Value == 0;

        return entry.StatusCode == (int)StatusCode.Ready;
    }

    private static string Classify(
        DoctorSnapshot snapshot,
        bool hasBusyFailures,
        bool hasBatchFallbackSignature,
        bool hasCommandNotFoundFailures)
    {
        if (!snapshot.EditorFound)
            return "editor-missing";

        if (!snapshot.PluginInstalled)
            return "plugin-missing";

        if (snapshot.IpcConnected)
            return "healthy";

        if (snapshot.ProjectLocked)
            return "starting-or-reloading";

        if (hasBatchFallbackSignature)
            return "transport-degraded";

        if (hasCommandNotFoundFailures)
            return "plugin-mismatch-suspected";

        if (hasBusyFailures)
            return "starting-or-reloading";

        return "transport-degraded";
    }

    private static string BuildSummary(string classification, DoctorSnapshot snapshot)
    {
        return classification switch
        {
            "healthy" => snapshot.ProjectLocked
                ? "Editor IPC is healthy. The current Unity lockfile is informational while the Editor owns the project."
                : "Editor IPC is healthy and the project looks ready for normal commands.",
            "plugin-missing" => "The Unity bridge package is not configured in this project yet.",
            "editor-missing" => "No compatible Unity Editor installation was discovered for this environment.",
            "starting-or-reloading" => "Unity appears to be compiling, reloading, or still bringing IPC online.",
            "transport-degraded" => "Recent failures suggest IPC is unavailable and batch fallback is unreliable for this project.",
            "plugin-mismatch-suspected" => "Recent command mismatches suggest the Unity plugin may not match the CLI command set yet.",
            _ => "Doctor could not classify the current project state."
        };
    }

    private static List<string> BuildRecommendations(
        string classification,
        string projectPath,
        DoctorAnalysis analysis,
        DoctorSnapshot snapshot)
    {
        var recommendations = new List<string>();

        switch (classification)
        {
            case "editor-missing":
                recommendations.Add("Run `unityctl editor list` and verify Unity Hub or custom editor paths are installed.");
                recommendations.Add("Open the project once in Unity Hub so editor discovery metadata is available.");
                break;
            case "plugin-missing":
                recommendations.Add($"Run `unityctl init --project \"{projectPath}\"` to add `{Constants.PluginPackageName}`.");
                recommendations.Add("Reopen the project or wait for Unity package import to finish after init.");
                break;
            case "starting-or-reloading":
                recommendations.Add($"Run `unityctl status --project \"{projectPath}\" --wait` and retry when it reports Ready.");
                recommendations.Add($"Use `unityctl watch --project \"{projectPath}\" --channel compilation` to observe reload/compile completion.");
                AddScriptSpecificRecommendations(recommendations, projectPath, analysis);
                AddUiSpecificRecommendations(recommendations, analysis);
                break;
            case "plugin-mismatch-suspected":
                recommendations.Add("Reopen the Unity Editor or wait for package import/domain reload so the plugin command registry refreshes.");
                recommendations.Add($"Verify the configured plugin source in `unityctl doctor --project \"{projectPath}\"` and rerun `unityctl init` if it drifted.");
                AddScriptSpecificRecommendations(recommendations, projectPath, analysis);
                AddUiSpecificRecommendations(recommendations, analysis);
                break;
            case "transport-degraded":
                recommendations.Add("Prefer a running Unity Editor with IPC ready before retrying commands on this project.");
                recommendations.Add("Inspect the Unity Editor.log path from doctor output for batchmode startup or response-file failures.");
                AddScriptSpecificRecommendations(recommendations, projectPath, analysis);
                AddUiSpecificRecommendations(recommendations, analysis);
                break;
            default:
                if (analysis.HasRecentPipeErrors || analysis.RecentFailures.Count > 0)
                    recommendations.Add("The latest diagnostics suggest the Editor has recovered; retry the original command first.");
                recommendations.Add("Prefer IPC-backed commands while the Editor is already open for the fastest and most reliable path.");
                break;
        }

        if (analysis.ActiveSessions.Any(session => session.StaleSuspected))
            recommendations.Add("Review `unityctl session list` for stale running sessions before assuming a command is still in progress.");

        if (!snapshot.IpcConnected && snapshot.LogPath != null)
            recommendations.Add($"Check the Unity Editor.log at `{snapshot.LogPath}` if retries continue to fail.");

        return recommendations
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string? ExtractCommandSuggestion(string? recommendation)
    {
        if (string.IsNullOrWhiteSpace(recommendation))
            return null;

        var start = recommendation.IndexOf('`');
        if (start < 0)
            return null;

        var end = recommendation.IndexOf('`', start + 1);
        if (end <= start)
            return null;

        var candidate = recommendation.Substring(start + 1, end - start - 1).Trim();
        return candidate.StartsWith("unityctl ", StringComparison.OrdinalIgnoreCase) ? candidate : null;
    }

    private static void AddScriptSpecificRecommendations(List<string> recommendations, string projectPath, DoctorAnalysis analysis)
    {
        var lastScriptFailure = analysis.RecentFailures
            .FirstOrDefault(activity => IsScriptCommand(activity.Operation));
        if (lastScriptFailure == null)
            return;

        if (string.Equals(lastScriptFailure.Operation, WellKnownCommands.ScriptGetErrors, StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add($"If `script get-errors` still reports missing compilation data after Ready, run `unityctl script validate --project \"{projectPath}\" --wait` once and retry.");
            recommendations.Add("`script get-errors` depends on the latest compile cache and is less useful before the Editor finishes a compile/reload cycle.");
            return;
        }

        if (string.Equals(lastScriptFailure.Operation, WellKnownCommands.ScriptFindRefs, StringComparison.OrdinalIgnoreCase)
            || string.Equals(lastScriptFailure.Operation, WellKnownCommands.ScriptRenameSymbol, StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("`script find-refs` and `script rename-symbol` are most reliable with a running Unity Editor and IPC ready.");
            recommendations.Add("Avoid relying on batch fallback for script refactor commands unless you have already verified that project path in practice.");
        }
    }

    private static void AddUiSpecificRecommendations(List<string> recommendations, DoctorAnalysis analysis)
    {
        var lastUiFailure = analysis.RecentFailures
            .FirstOrDefault(activity => IsUiInteractionCommand(activity.Operation));
        if (lastUiFailure == null)
            return;

        if (string.Equals(lastUiFailure.Operation, WellKnownCommands.UiInput, StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("`ui input` sets InputField.text deterministically and does not emulate keystrokes or focus changes.");
            recommendations.Add("Prefer a running Unity Editor with IPC ready before relying on `ui input`; batch fallback is not guaranteed for UI interaction commands.");
            return;
        }

        if (string.Equals(lastUiFailure.Operation, WellKnownCommands.UiToggle, StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("`ui toggle` sets Toggle.isOn deterministically and does not emulate a pointer click.");
            recommendations.Add("Prefer a running Unity Editor with IPC ready before relying on `ui toggle`; batch fallback is not guaranteed for UI interaction commands.");
            return;
        }

        if (string.Equals(lastUiFailure.Operation, WellKnownCommands.UiClick, StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("`ui click` invokes Button.onClick deterministically in Play Mode and does not depend on desktop focus or synthetic input.");
            recommendations.Add("Prefer a running Unity Editor with IPC ready before relying on `ui click`; batch fallback is not guaranteed for UI interaction commands.");
        }
    }

    private static bool IsScriptCommand(string? operation)
    {
        return string.Equals(operation, WellKnownCommands.ScriptGetErrors, StringComparison.OrdinalIgnoreCase)
            || string.Equals(operation, WellKnownCommands.ScriptFindRefs, StringComparison.OrdinalIgnoreCase)
            || string.Equals(operation, WellKnownCommands.ScriptRenameSymbol, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUiInteractionCommand(string? operation)
    {
        return string.Equals(operation, WellKnownCommands.UiToggle, StringComparison.OrdinalIgnoreCase)
            || string.Equals(operation, WellKnownCommands.UiInput, StringComparison.OrdinalIgnoreCase)
            || string.Equals(operation, WellKnownCommands.UiClick, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static DateTimeOffset? ParseIso(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}

internal sealed class DoctorSnapshot
{
    public string PipeName { get; set; } = string.Empty;
    public bool EditorFound { get; set; }
    public string EditorVersion { get; set; } = "not found";
    public bool PluginInstalled { get; set; }
    public string? PluginSource { get; set; }
    public string? PluginSourceKind { get; set; }
    public bool IpcConnected { get; set; }
    public bool IpcPipePresent { get; set; }
    public bool BridgeLoaded { get; set; }
    public bool ProjectLocked { get; set; }
    public bool? IsCompiling { get; set; }
    public bool? IsDomainReloading { get; set; }
    public string LockFilePath { get; set; } = string.Empty;
    public bool LockFileExists { get; set; }
    public string? LogPath { get; set; }
    public string BuildStateDirectory { get; set; } = string.Empty;
    public bool BuildStateExists { get; set; }
    public int BuildStateCount { get; set; }
    public double BuildStateOldestAgeMinutes { get; set; }
    public List<string> EditorLogErrors { get; set; } = [];
    public List<string> UnityctlLogLines { get; set; } = [];
    public string? HumanDiagnostics { get; set; }
}

internal sealed class DoctorAnalysis
{
    public string Classification { get; set; } = "transport-degraded";
    public string Summary { get; set; } = string.Empty;
    public string LockSeverity { get; set; } = "none";
    public DoctorActivity? LastSuccess { get; set; }
    public List<DoctorActivity> RecentFailures { get; set; } = [];
    public List<DoctorStatusCodeSummary> RepeatedStatusCodes { get; set; } = [];
    public bool HasBatchFallbackSignature { get; set; }
    public bool HasRecentPipeErrors { get; set; }
    public List<DoctorSessionSummary> ActiveSessions { get; set; } = [];
    public List<string> Recommendations { get; set; } = [];
    public string? RecommendedNextCommand { get; set; }
}

internal sealed class DoctorActivity
{
    public long Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public string? Error { get; set; }
    public bool Success { get; set; }
}

internal sealed class DoctorStatusCodeSummary
{
    public int StatusCode { get; set; }
    public int Count { get; set; }
    public List<string> Operations { get; set; } = [];
}

internal sealed class DoctorSessionSummary
{
    public string Id { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }
    public bool StaleSuspected { get; set; }
}
