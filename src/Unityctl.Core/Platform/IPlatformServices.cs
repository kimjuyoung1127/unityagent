using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public interface IPlatformServices
{
    string GetUnityHubEditorsJsonPath();
    IEnumerable<string> GetDefaultEditorSearchPaths();
    string GetUnityExecutablePath(string editorBasePath);
    IEnumerable<UnityProcessInfo> FindRunningUnityProcesses();
    bool IsProjectLocked(string projectPath);
    Stream CreateIpcClientStream(string projectPath);
    string GetTempResponseFilePath();
}

public sealed class UnityProcessInfo
{
    public int ProcessId { get; set; }
    public string? ProjectPath { get; set; }
    public string? Version { get; set; }
    public string? ExecutablePath { get; set; }
}
