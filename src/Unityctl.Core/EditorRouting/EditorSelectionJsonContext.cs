using System.Text.Json.Serialization;

namespace Unityctl.Core.EditorRouting;

[JsonSerializable(typeof(EditorSelection))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class EditorSelectionJsonContext : JsonSerializerContext
{
}
