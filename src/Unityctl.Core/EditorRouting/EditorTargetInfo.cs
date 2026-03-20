namespace Unityctl.Core.EditorRouting;

public sealed class EditorTargetInfo
{
    public string ProjectPath { get; set; } = string.Empty;

    public string PipeName { get; set; } = string.Empty;

    public string? EditorVersion { get; set; }

    public string? EditorLocation { get; set; }

    public int? UnityPid { get; set; }

    public bool IsRunning { get; set; }
}
