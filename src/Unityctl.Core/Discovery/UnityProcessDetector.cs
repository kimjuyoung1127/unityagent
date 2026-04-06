using Unityctl.Core.Platform;

namespace Unityctl.Core.Discovery;

/// <summary>
/// Detects running Unity Editor processes and their associated projects.
/// Phase 2B: WMI (Windows) / ps (macOS/Linux) based detection.
/// </summary>
public sealed class UnityProcessDetector
{
    private readonly IPlatformServices _platform;

    public UnityProcessDetector(IPlatformServices platform)
    {
        _platform = platform;
    }

    /// <summary>
    /// Check if a Unity Editor is running for the given project.
    /// </summary>
    public bool IsEditorRunning(string projectPath)
    {
        var normalized = Unityctl.Shared.Constants.NormalizeProjectPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .Any(p => MatchesProjectPath(p.ProjectPath, normalized));
    }

    public bool IsInteractiveEditorRunning(string projectPath)
    {
        return FindInteractiveProcessForProject(projectPath) != null;
    }

    /// <summary>
    /// Find the Unity process for a specific project.
    /// </summary>
    public UnityProcessInfo? FindProcessForProject(string projectPath)
    {
        var normalized = Unityctl.Shared.Constants.NormalizeProjectPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .FirstOrDefault(p => MatchesProjectPath(p.ProjectPath, normalized));
    }

    public UnityProcessInfo? FindInteractiveProcessForProject(string projectPath)
    {
        var normalized = Unityctl.Shared.Constants.NormalizeProjectPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .FirstOrDefault(p => MatchesProjectPath(p.ProjectPath, normalized) && p.IsInteractiveEditor);
    }

    public UnityProcessInfo? FindProcessById(int processId)
    {
        return _platform.FindRunningUnityProcesses()
            .FirstOrDefault(p => p.ProcessId == processId);
    }

    public IReadOnlyList<UnityProcessInfo> FindProcessesForProject(string projectPath)
    {
        var normalized = Unityctl.Shared.Constants.NormalizeProjectPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .Where(p => MatchesProjectPath(p.ProjectPath, normalized))
            .OrderBy(p => p.ProcessId)
            .ToList();
    }

    public IReadOnlyList<UnityProcessInfo> FindInteractiveProcessesForProject(string projectPath)
    {
        var normalized = Unityctl.Shared.Constants.NormalizeProjectPath(projectPath);
        return _platform.FindRunningUnityProcesses()
            .Where(p => MatchesProjectPath(p.ProjectPath, normalized) && p.IsInteractiveEditor)
            .OrderBy(p => p.ProcessId)
            .ToList();
    }

    private static bool MatchesProjectPath(string? candidatePath, string normalizedProjectPath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath))
            return false;

        try
        {
            return string.Equals(
                Unityctl.Shared.Constants.NormalizeProjectPath(candidatePath),
                normalizedProjectPath,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
