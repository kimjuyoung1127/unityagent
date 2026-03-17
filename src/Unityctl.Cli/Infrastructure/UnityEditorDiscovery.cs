using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Platform;
using Unityctl.Shared.Models;

namespace Unityctl.Cli.Infrastructure;

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

    /// <summary>
    /// Find all installed Unity Editors.
    /// Strategy: 1) Parse editors.json from Unity Hub, 2) Scan default paths.
    /// </summary>
    public List<UnityEditorInfo> FindEditors()
    {
        var editors = new Dictionary<string, UnityEditorInfo>(StringComparer.OrdinalIgnoreCase);

        // Strategy 1: Unity Hub editors.json
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

        // Strategy 2: Scan default paths
        foreach (var searchPath in _platform.GetDefaultEditorSearchPaths())
        {
            if (!Directory.Exists(searchPath)) continue;
            ScanEditorDirectory(searchPath, editors);
        }

        return editors.Values.OrderByDescending(e => e.Version).ToList();
    }

    /// <summary>
    /// Find the best editor for a specific project by reading ProjectVersion.txt.
    /// </summary>
    public UnityEditorInfo? FindEditorForProject(string projectPath)
    {
        var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFile)) return null;

        var content = File.ReadAllText(versionFile);
        var version = ParseProjectVersion(content);
        if (version == null) return null;

        var editors = FindEditors();
        return editors.FirstOrDefault(e => e.Version == version)
            ?? editors.FirstOrDefault(e => e.Version.StartsWith(version.Split('.')[0] + "."));
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
}
