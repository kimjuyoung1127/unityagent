using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

public sealed class VerificationResult
{
    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("artifactsDirectory")]
    public string ArtifactsDirectory { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public VerificationStepResult[] Steps { get; set; } = [];

    [JsonPropertyName("artifacts")]
    public VerificationArtifact[] Artifacts { get; set; } = [];
}

public sealed class VerificationStepResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonObject? Data { get; set; }
}

public sealed class VerificationArtifact
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("inlineBase64")]
    public string? InlineBase64 { get; set; }

    [JsonPropertyName("metadata")]
    public JsonObject? Metadata { get; set; }
}
