using System.Text.Json.Serialization;

namespace Unityctl.Core.EditorRouting;

public sealed class EditorSelection
{
    [JsonPropertyName("projectPath")]
    public string ProjectPath { get; set; } = string.Empty;

    [JsonPropertyName("unityPid")]
    public int? UnityPid { get; set; }

    [JsonPropertyName("selectionMode")]
    public string? SelectionMode { get; set; }

    [JsonPropertyName("selectedAt")]
    public string SelectedAt { get; set; } = string.Empty;
}
