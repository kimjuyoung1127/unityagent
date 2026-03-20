using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace Unityctl.Shared.Protocol;

public sealed class VerificationDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public VerificationStep[] Steps { get; set; } = [];
}

public sealed class VerificationStep
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("view")]
    public string? View { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("baseline")]
    public string? Baseline { get; set; }

    [JsonPropertyName("candidate")]
    public string? Candidate { get; set; }

    [JsonPropertyName("durationSeconds")]
    public int? DurationSeconds { get; set; }

    [JsonPropertyName("maxChangedPixelRatio")]
    public double? MaxChangedPixelRatio { get; set; }

    [JsonPropertyName("targetId")]
    public string? TargetId { get; set; }

    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("expected")]
    public JsonNode? Expected { get; set; }

    [JsonPropertyName("settleTimeoutSeconds")]
    public int? SettleTimeoutSeconds { get; set; }
}
