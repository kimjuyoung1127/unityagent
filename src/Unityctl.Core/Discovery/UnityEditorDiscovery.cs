using System.Text.Json.Nodes;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Models;

namespace Unityctl.Core.Discovery;

/// <summary>
/// Discovers installed Unity Editor versions via Unity Hub's editors.json
/// and filesystem scanning.
/// </summary>
public sealed class UnityEditorDiscovery
{
    private readonly IPlatformServices _platform;

    public UnityEditorDiscovery(IPlatformServices platform)
    {
        _platform = platform;
    }

    public List<UnityEditorInfo> FindEditors()
    {
        var editors = new Dictionary<string, UnityEditorInfo>(StringComparer.OrdinalIgnoreCase);

        var editorsJsonPath = _platform.GetUnityHubEditorsJsonPath();
        if (File.Exists(editorsJsonPath))
        {
            try
            {
                var json = File.ReadAllText(editorsJsonPath);
                ParseEditorsJson(json, editors);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Failed to parse {editorsJsonPath}: {ex.Message}");
            }
        }

        foreach (var searchPath in _platform.GetDefaultEditorSearchPaths())
        {
            if (!Directory.Exists(searchPath)) continue;
            ScanEditorDirectory(searchPath, editors);
        }

        var runningProcesses = _platform.FindRunningUnityProcesses().ToList();
        HydrateRunningState(editors.Values, runningProcesses);

        return editors.Values
            .OrderByDescending(e => e.Version, UnityVersionComparer.Instance)
            .ToList();
    }

    public UnityEditorInfo? FindEditorForProject(string projectPath)
    {
        var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFile)) return null;

        var content = File.ReadAllText(versionFile);
        var version = ParseProjectVersion(content);
        if (version == null) return null;

        var editors = FindEditors();
        return editors.FirstOrDefault(e => e.Version == version)
            ?? editors
                .Where(e => SharesMajorVersion(e.Version, version))
                .OrderByDescending(e => e.Version, UnityVersionComparer.Instance)
                .FirstOrDefault();
    }

    private void ParseEditorsJson(string json, Dictionary<string, UnityEditorInfo> editors)
    {
        var root = JsonNode.Parse(json);
        if (root == null) return;

        foreach (var (version, node) in root.AsObject())
        {
            if (node == null) continue;
            var locationProp = node["location"] ?? node["Location"];
            var location = locationProp?.GetValue<string>();

            if (string.IsNullOrEmpty(location)) continue;

            var exePath = _platform.GetUnityExecutablePath(location);
            if (!File.Exists(exePath)) continue;

            editors[version] = new UnityEditorInfo
            {
                Version = version,
                Location = location
            };
        }
    }

    private void ScanEditorDirectory(string basePath, Dictionary<string, UnityEditorInfo> editors)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                var version = Path.GetFileName(dir);
                if (editors.ContainsKey(version)) continue;

                var exePath = _platform.GetUnityExecutablePath(dir);
                if (!File.Exists(exePath)) continue;

                editors[version] = new UnityEditorInfo
                {
                    Version = version,
                    Location = dir
                };
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Failed to scan {basePath}: {ex.Message}");
        }
    }

    public static string? ParseProjectVersion(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("m_EditorVersion:"))
            {
                return trimmed.Substring("m_EditorVersion:".Length).Trim();
            }
        }
        return null;
    }

    private static bool SharesMajorVersion(string candidate, string requested)
    {
        var candidateMajor = candidate.Split('.').FirstOrDefault();
        var requestedMajor = requested.Split('.').FirstOrDefault();
        return candidateMajor != null
            && requestedMajor != null
            && candidateMajor.Equals(requestedMajor, StringComparison.OrdinalIgnoreCase);
    }

    public List<UnityEditorInstanceInfo> FindRunningEditorInstances(bool probeIpc = false)
    {
        var installedEditors = FindEditors();
        var runningProcesses = _platform.FindRunningUnityProcesses().ToList();
        var instances = new List<UnityEditorInstanceInfo>(runningProcesses.Count);

        foreach (var process in runningProcesses)
        {
            string? normalizedProjectPath = null;
            string? pipeName = null;
            var ipcReady = false;

            if (!string.IsNullOrWhiteSpace(process.ProjectPath))
            {
                normalizedProjectPath = Unityctl.Shared.Constants.NormalizeProjectPath(process.ProjectPath);
                pipeName = Unityctl.Shared.Constants.GetPipeName(process.ProjectPath);

                if (probeIpc)
                {
                    IpcTransport? ipc = null;
                    try
                    {
                        ipc = new IpcTransport(process.ProjectPath);
                        ipcReady = ipc.ProbeAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        ipcReady = false;
                    }
                    finally
                    {
                        if (ipc != null)
                            ipc.DisposeAsync().GetAwaiter().GetResult();
                    }
                }
            }

            var matchingEditor = installedEditors.FirstOrDefault(editor =>
                string.Equals(editor.Version, process.Version, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(process.ExecutablePath)
                    && string.Equals(
                        _platform.GetUnityExecutablePath(editor.Location),
                        process.ExecutablePath,
                        StringComparison.OrdinalIgnoreCase)));

            instances.Add(new UnityEditorInstanceInfo
            {
                ProcessId = process.ProcessId,
                ProjectPath = normalizedProjectPath,
                Version = process.Version ?? matchingEditor?.Version,
                EditorLocation = matchingEditor?.Location,
                PipeName = pipeName,
                IpcReady = ipcReady,
                IsBatchMode = process.IsBatchMode,
                HasMainWindow = process.HasMainWindow,
                ProcessKind = process.IsInteractiveEditor ? "interactive" : process.IsBatchMode ? "headless" : "background"
            });
        }

        return instances
            .OrderBy(instance => instance.ProjectPath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(instance => instance.ProcessId)
            .ToList();
    }

    private void HydrateRunningState(IEnumerable<UnityEditorInfo> editors, IReadOnlyList<UnityProcessInfo> runningProcesses)
    {
        foreach (var editor in editors)
        {
            var editorExecutablePath = Path.GetFullPath(_platform.GetUnityExecutablePath(editor.Location));
            var matches = runningProcesses
                .Where(process => string.Equals(
                    process.ExecutablePath,
                    editorExecutablePath,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            editor.IsRunning = matches.Count > 0;
            editor.RunningProcessIds = matches.Select(process => process.ProcessId).Distinct().OrderBy(id => id).ToList();
            editor.RunningProjectPaths = matches
                .Where(process => !string.IsNullOrWhiteSpace(process.ProjectPath))
                .Select(process => Unityctl.Shared.Constants.NormalizeProjectPath(process.ProjectPath!))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
