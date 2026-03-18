using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unityctl.Shared.Protocol;

namespace Unityctl.Shared.Serialization;

[JsonSerializable(typeof(CommandRequest))]
[JsonSerializable(typeof(CommandResponse))]
[JsonSerializable(typeof(EventEnvelope))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(StatusCode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(FlightEntry))]
[JsonSerializable(typeof(FlightEntry[]))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class UnityctlJsonContext : JsonSerializerContext
{
}
