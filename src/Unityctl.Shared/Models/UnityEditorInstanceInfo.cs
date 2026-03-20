using System.Text.Json.Serialization;

namespace Unityctl.Shared.Models;

public sealed class UnityEditorInstanceInfo
{
    [JsonPropertyName("processId")]
    public int ProcessId { get; set; }

    [JsonPropertyName("projectPath")]
    public string? ProjectPath { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("editorLocation")]
    public string? EditorLocation { get; set; }

    [JsonPropertyName("pipeName")]
    public string? PipeName { get; set; }

    [JsonPropertyName("ipcReady")]
    public bool IpcReady { get; set; }
}
