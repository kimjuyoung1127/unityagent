using System.Text.Json.Serialization;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// Compact (non-indented) JSON serialization context for NDJSON flight log files.
/// Separate from UnityctlJsonContext to avoid WriteIndented=true breaking NDJSON format.
/// </summary>
[JsonSerializable(typeof(FlightEntry))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class FlightJsonContext : JsonSerializerContext
{
}
