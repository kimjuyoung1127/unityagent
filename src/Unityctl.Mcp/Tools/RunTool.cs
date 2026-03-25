using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class RunTool(CommandExecutor executor)
{
    private static readonly HashSet<string> Allowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        WellKnownCommands.PlayMode,
        WellKnownCommands.PlayerSettings,
        WellKnownCommands.AssetRefresh,
        WellKnownCommands.GameObjectCreate,
        WellKnownCommands.GameObjectDelete,
        WellKnownCommands.GameObjectSetActive,
        WellKnownCommands.GameObjectMove,
        WellKnownCommands.GameObjectRename,
        WellKnownCommands.SceneSave,
        WellKnownCommands.SceneOpen,
        WellKnownCommands.SceneCreate,
        WellKnownCommands.ComponentAdd,
        WellKnownCommands.ComponentRemove,
        WellKnownCommands.ComponentSetProperty,
        WellKnownCommands.BatchExecute,
        WellKnownCommands.Undo,
        WellKnownCommands.Redo,
        // Phase C-1: Asset CRUD
        WellKnownCommands.AssetCreate,
        WellKnownCommands.AssetCreateFolder,
        WellKnownCommands.AssetCopy,
        WellKnownCommands.AssetMove,
        WellKnownCommands.AssetDelete,
        WellKnownCommands.AssetImport,
        // Phase C-2: Prefab
        WellKnownCommands.PrefabCreate,
        WellKnownCommands.PrefabUnpack,
        WellKnownCommands.PrefabApply,
        WellKnownCommands.PrefabEdit,
        WellKnownCommands.PrefabInstantiate,
        // Phase C-3: Package Manager + Project Settings
        WellKnownCommands.PackageList,
        WellKnownCommands.PackageAdd,
        WellKnownCommands.PackageRemove,
        WellKnownCommands.ProjectSettingsGet,
        WellKnownCommands.ProjectSettingsSet,
        // Phase C-4: Material/Shader
        WellKnownCommands.MaterialCreate,
        WellKnownCommands.MaterialGet,
        WellKnownCommands.MaterialSet,
        WellKnownCommands.MaterialSetShader,
        // Phase C-5: Animation + UI
        WellKnownCommands.AnimationCreateClip,
        WellKnownCommands.AnimationCreateController,
        WellKnownCommands.UiCanvasCreate,
        WellKnownCommands.UiElementCreate,
        WellKnownCommands.UiSetRect,
        WellKnownCommands.UiClick,
        WellKnownCommands.UiToggle,
        WellKnownCommands.UiInput,
        // P0 잔여분: Asset Labels + Build Settings
        WellKnownCommands.AssetSetLabels,
        WellKnownCommands.BuildSettingsSetScenes,
        // Script Editing v1
        WellKnownCommands.ScriptCreate,
        WellKnownCommands.ScriptEdit,
        WellKnownCommands.ScriptDelete,
        WellKnownCommands.ScriptValidate,
        WellKnownCommands.ScriptPatch,
        WellKnownCommands.ScriptRenameSymbol,
        WellKnownCommands.ExecInvoke,
        // Mesh Primitives
        WellKnownCommands.MeshCreatePrimitive,
        // Tags & Layers
        WellKnownCommands.TagAdd,
        WellKnownCommands.LayerSet,
        WellKnownCommands.GameObjectSetTag,
        WellKnownCommands.GameObjectSetLayer,
        // Editor Utility
        WellKnownCommands.ConsoleClear,
        WellKnownCommands.DefineSymbolsSet,
        WellKnownCommands.EditorPause,
        WellKnownCommands.EditorFocusGameView,
        WellKnownCommands.EditorFocusSceneView,
        // Lighting
        WellKnownCommands.LightingBake,
        WellKnownCommands.LightingCancel,
        WellKnownCommands.LightingClear,
        WellKnownCommands.LightingSetSettings,
        // NavMesh
        WellKnownCommands.NavMeshBake,
        WellKnownCommands.NavMeshClear,
        // Physics
        WellKnownCommands.PhysicsSetSettings,
        WellKnownCommands.PhysicsSetCollisionMatrix,
        // Texture Import
        WellKnownCommands.TextureSetImportSettings,
        // ScriptableObject
        WellKnownCommands.ScriptableObjectSetProperty,
        // UI Toolkit — Phase I-2
        WellKnownCommands.UitkSetValue,
        // Cinemachine — Phase E
        WellKnownCommands.CinemachineSetProperty,
        // Volume/PostProcessing — Phase D
        WellKnownCommands.VolumeSetOverride,
        // UGUI Enhancement — Phase I-1
        WellKnownCommands.UiScroll,
        WellKnownCommands.UiSliderSet,
        WellKnownCommands.UiDropdownSet,
        // Profiler — Phase C
        WellKnownCommands.ProfilerStart,
        WellKnownCommands.ProfilerStop,
        // Animation Workflow — Phase H
        WellKnownCommands.AnimationAddCurve,
        // Asset Import/Export — Phase G
        WellKnownCommands.AssetExport
    };

    [McpServerTool(Name = "unityctl_run")]
    [Description("Execute write/action command (use unityctl_schema for params)")]
    public async Task<string> RunAsync(
        [Description("Unity project path")] string project,
        [Description("Command name (e.g. gameobject-create, component-add, play-mode)")] string command,
        [Description("Command parameters as a JSON object (excluding 'project')")] string? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            return JsonSerializer.Serialize(
                CommandResponse.Fail(StatusCode.InvalidParameters, "command must not be empty"),
                UnityctlJsonContext.Default.CommandResponse);

        if (!Allowlist.Contains(command))
            return JsonSerializer.Serialize(
                CommandResponse.Fail(
                    StatusCode.CommandNotFound,
                    $"Command '{command}' is not in the allowlist. Allowed: {string.Join(", ", Allowlist.Order())}"),
                UnityctlJsonContext.Default.CommandResponse);

        JsonObject? parsedParameters = null;
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            try
            {
                var node = JsonNode.Parse(parameters);
                parsedParameters = node?.AsObject();
                if (parsedParameters is null)
                    return JsonSerializer.Serialize(
                        CommandResponse.Fail(StatusCode.InvalidParameters, "parameters must be a JSON object"),
                        UnityctlJsonContext.Default.CommandResponse);
            }
            catch (JsonException ex)
            {
                return JsonSerializer.Serialize(
                    CommandResponse.Fail(StatusCode.InvalidParameters, $"Invalid JSON in parameters: {ex.Message}"),
                    UnityctlJsonContext.Default.CommandResponse);
            }
        }

        var request = new CommandRequest
        {
            Command = command,
            Parameters = parsedParameters
        };

        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
