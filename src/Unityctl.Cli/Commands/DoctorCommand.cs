using System.Collections.Generic;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Core.Diagnostics;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class DoctorCommand
{
    public static void Execute(string project, bool json = false)
    {
        var result = Diagnose(project);

        if (json)
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
                }
            };

            if (result.StructuredDiagnostics != null)
            {
                var errArr = new JsonArray();
                foreach (var e in result.StructuredDiagnostics.Value.Errors)
                    errArr.Add(e);

                var uArr = new JsonArray();
                foreach (var u in result.StructuredDiagnostics.Value.UnityctlLines)
                    uArr.Add(u);

                results["editorLog"] = new JsonObject { ["errors"] = errArr, ["unityctl"] = uArr };
            }
            else
            {
                results["editorLog"] = new JsonObject { ["errors"] = new JsonArray(), ["unityctl"] = new JsonArray() };
            }

            results["logPath"] = result.LogPath;

            Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"unityctl doctor — project: {project}");
            Console.WriteLine();
            Console.WriteLine(result.EditorFound
                ? $"  \u2713 Unity Editor found: {result.EditorVersion}"
                : "  \u2717 Unity Editor not found");
            Console.WriteLine(result.PluginInstalled
                ? $"  \u2713 Plugin installed: {Constants.PluginPackageName}"
                : "  \u2717 Plugin not installed (run: unityctl init)");
            if (result.PluginInstalled)
            {
                Console.WriteLine($"    Source kind: {result.PluginSourceKind ?? "unknown"}");
                if (!string.IsNullOrWhiteSpace(result.PluginSource))
                    Console.WriteLine($"    Source: {result.PluginSource}");
            }
            Console.WriteLine(result.IpcConnected
                ? $"  \u2713 IPC connected (pipe: {result.PipeName})"
                : $"  \u2717 IPC probe failed (pipe: {result.PipeName})");
            Console.WriteLine(result.ProjectLocked
                ? $"  \u26a0 Project lock detected: {result.LockFilePath}"
                : $"  \u2713 Project lock: not detected ({result.LockFilePath})");
            Console.WriteLine(result.BuildStateExists
                ? $"  \u2713 Build transition state: {result.BuildStateCount} file(s), oldest {result.BuildStateOldestAgeMinutes:n1} min"
                : $"  \u2713 Build transition state: none ({result.BuildStateDirectory})");

            if (result.HumanDiagnostics != null)
            {
                Console.WriteLine();
                Console.Write(result.HumanDiagnostics);
            }
            else if (!result.IpcConnected)
            {
                Console.WriteLine();
                Console.WriteLine("  No compilation errors in Editor.log");
                Console.WriteLine("  Possible causes: Unity not running, domain reload in progress, project lock held by another process, or plugin import/compile not finished");
            }

            if (result.LogPath != null && result.HumanDiagnostics == null)
            {
                Console.WriteLine($"  Log: {result.LogPath}");
            }
        }

        Environment.ExitCode = result.IpcConnected ? 0 : 1;
    }

    internal static DoctorResult Diagnose(string project)
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

        return new DoctorResult
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
            StructuredDiagnostics = EditorLogDiagnostics.GetStructuredDiagnostics(),
            HumanDiagnostics = EditorLogDiagnostics.GetRecentDiagnostics()
        };
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
        var lines = new List<string>
        {
            "  Doctor summary:",
            result.EditorFound
                ? $"    \u2713 Unity Editor found: {result.EditorVersion}"
                : "    \u2717 Unity Editor not found",
            result.PluginInstalled
                ? $"    \u2713 Plugin installed: {Constants.PluginPackageName} ({result.PluginSourceKind ?? "unknown"})"
                : "    \u2717 Plugin not installed",
            result.IpcConnected
                ? $"    \u2713 IPC connected ({result.PipeName})"
                : $"    \u2717 IPC probe failed ({result.PipeName})",
            result.ProjectLocked
                ? $"    \u26a0 Project lock detected ({result.LockFilePath})"
                : $"    \u2713 Project lock not detected ({result.LockFilePath})"
        };

        if (!string.IsNullOrWhiteSpace(result.PluginSource))
            lines.Add($"    Plugin source: {result.PluginSource}");

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
}

internal sealed class DoctorResult
{
    public string PipeName { get; set; } = string.Empty;

    public bool EditorFound { get; set; }

    public string EditorVersion { get; set; } = "not found";

    public bool PluginInstalled { get; set; }

    public string? PluginSource { get; set; }

    public string? PluginSourceKind { get; set; }

    public bool IpcConnected { get; set; }

    public bool ProjectLocked { get; set; }

    public string LockFilePath { get; set; } = string.Empty;

    public bool LockFileExists { get; set; }

    public string? LogPath { get; set; }

    public string BuildStateDirectory { get; set; } = string.Empty;

    public bool BuildStateExists { get; set; }

    public int BuildStateCount { get; set; }

    public double BuildStateOldestAgeMinutes { get; set; }

    public (List<string> Errors, List<string> UnityctlLines)? StructuredDiagnostics { get; set; }

    public string? HumanDiagnostics { get; set; }
}
