using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Protocol;

namespace Unityctl.Shared.Serialization;

[JsonSerializable(typeof(CommandRequest))]
[JsonSerializable(typeof(CommandResponse))]
[JsonSerializable(typeof(EventEnvelope))]
[JsonSerializable(typeof(EventEnvelope[]))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(StatusCode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(FlightEntry))]
[JsonSerializable(typeof(FlightEntry[]))]
[JsonSerializable(typeof(PreflightCheck))]
[JsonSerializable(typeof(PreflightCheck[]))]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(Session[]))]
[JsonSerializable(typeof(SessionState))]
[JsonSerializable(typeof(SceneSnapshot))]
[JsonSerializable(typeof(SceneSnapshot[]))]
[JsonSerializable(typeof(SceneSetupEntry))]
[JsonSerializable(typeof(SceneSetupEntry[]))]
[JsonSerializable(typeof(SceneEntry))]
[JsonSerializable(typeof(SceneEntry[]))]
[JsonSerializable(typeof(GameObjectEntry))]
[JsonSerializable(typeof(GameObjectEntry[]))]
[JsonSerializable(typeof(ComponentEntry))]
[JsonSerializable(typeof(ComponentEntry[]))]
[JsonSerializable(typeof(SceneDiffResult))]
[JsonSerializable(typeof(SceneDiffResult[]))]
[JsonSerializable(typeof(SceneDiffEntry))]
[JsonSerializable(typeof(SceneDiffEntry[]))]
[JsonSerializable(typeof(DiffObjectEntry))]
[JsonSerializable(typeof(DiffObjectEntry[]))]
[JsonSerializable(typeof(ModifiedObjectEntry))]
[JsonSerializable(typeof(ModifiedObjectEntry[]))]
[JsonSerializable(typeof(ModifiedComponentEntry))]
[JsonSerializable(typeof(ModifiedComponentEntry[]))]
[JsonSerializable(typeof(ComponentDiffEntry))]
[JsonSerializable(typeof(ComponentDiffEntry[]))]
[JsonSerializable(typeof(PropertyChange))]
[JsonSerializable(typeof(PropertyChange[]))]
[JsonSerializable(typeof(CommandSchema))]
[JsonSerializable(typeof(CommandDefinition))]
[JsonSerializable(typeof(CommandDefinition[]))]
[JsonSerializable(typeof(CommandParameterDefinition))]
[JsonSerializable(typeof(CommandParameterDefinition[]))]
[JsonSerializable(typeof(Unityctl.Shared.Models.UnityEditorInfo))]
[JsonSerializable(typeof(Unityctl.Shared.Models.UnityEditorInfo[]))]
[JsonSerializable(typeof(Unityctl.Shared.Models.UnityEditorInstanceInfo))]
[JsonSerializable(typeof(Unityctl.Shared.Models.UnityEditorInstanceInfo[]))]
[JsonSerializable(typeof(VerificationDefinition))]
[JsonSerializable(typeof(VerificationStep))]
[JsonSerializable(typeof(VerificationStep[]))]
[JsonSerializable(typeof(VerificationResult))]
[JsonSerializable(typeof(VerificationStepResult))]
[JsonSerializable(typeof(VerificationStepResult[]))]
[JsonSerializable(typeof(VerificationArtifact))]
[JsonSerializable(typeof(VerificationArtifact[]))]
[JsonSerializable(typeof(WorkflowDefinition))]
[JsonSerializable(typeof(WorkflowStep))]
[JsonSerializable(typeof(WorkflowStep[]))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class UnityctlJsonContext : JsonSerializerContext
{
}
