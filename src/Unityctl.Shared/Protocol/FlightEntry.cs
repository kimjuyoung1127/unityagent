using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

/// <summary>
/// A single entry in the flight recorder log (NDJSON format).
/// Phase 3B: 15-field model.
/// </summary>
public sealed class FlightEntry
{
    [JsonPropertyName("ts")]
    public long Timestamp { get; set; }

    [JsonPropertyName("op")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("project")]
    public string? Project { get; set; }

    [JsonPropertyName("transport")]
    public string? Transport { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; } = "info";

    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("unityVersion")]
    public string? UnityVersion { get; set; }

    [JsonPropertyName("machine")]
    public string? Machine { get; set; }

    [JsonPropertyName("v")]
    public string V { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public string? Args { get; set; }

    [JsonPropertyName("sid")]
    public string? Sid { get; set; }
}
