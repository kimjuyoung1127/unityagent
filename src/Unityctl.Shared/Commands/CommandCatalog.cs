using System.Text.Json.Serialization;
using Unityctl.Shared.Protocol;

namespace Unityctl.Shared.Commands;

public static class CommandCatalog
{
    public static readonly CommandDefinition Init = Define(
        "init",
        "Install the unityctl plugin into a Unity project",
        "setup",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("source", "string", "Custom local plugin source path", required: false));

    public static readonly CommandDefinition EditorList = Define(
        "editor list",
        "Discover installed Unity editors",
        "discovery",
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Status = Define(
        WellKnownCommands.Status,
        "Check Unity editor and project status",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("wait", "bool", "Retry until editor responds", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Ping = Define(
        WellKnownCommands.Ping,
        "Verify unityctl connectivity to a Unity project",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Build = Define(
        WellKnownCommands.Build,
        "Build a Unity project for a target platform",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("target", "string", "Build target (StandaloneWindows64, OSX, Linux, Android, iOS, WebGL)", required: false),
        Parameter("output", "string", "Output path for build artifacts", required: false),
        Parameter("dryRun", "bool", "Validate without building (preflight check)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Test = Define(
        WellKnownCommands.Test,
        "Start Unity tests (EditMode or PlayMode)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("mode", "string", "Test mode: edit or play", required: false),
        Parameter("filter", "string", "Test name filter", required: false),
        Parameter("wait", "bool", "Wait for test completion (default: true, disabled for PlayMode)", required: false),
        Parameter("timeout", "int", "Timeout in seconds when waiting (default: 300)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Check = Define(
        WellKnownCommands.Check,
        "Check whether Unity scripts compiled successfully",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("type", "string", "Check type: compile", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Tools = Define(
        "tools",
        "List all available tools with parameters",
        "meta",
        Parameter("json", "bool", "Output as machine-readable JSON", required: false));

    public static readonly CommandDefinition Log = Define(
        "log",
        "Query and manage command execution logs",
        "query",
        Parameter("last", "int", "Show last N entries (default: 20)", required: false),
        Parameter("tail", "bool", "Follow log file in real-time", required: false),
        Parameter("op", "string", "Filter by operation (build, test, etc)", required: false),
        Parameter("level", "string", "Filter by level (info, warn, error)", required: false),
        Parameter("since", "string", "Filter since date/time (ISO 8601)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false),
        Parameter("prune", "bool", "Apply retention policy (30 days / 50 MB)", required: false),
        Parameter("stats", "bool", "Show log statistics", required: false));

    public static readonly CommandDefinition SessionList = Define(
        "session list",
        "List active and recent command execution sessions",
        "meta",
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SessionStop = Define(
        "session stop",
        "Cancel a running session",
        "action",
        Parameter("id", "string", "Session ID to cancel", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SessionClean = Define(
        "session clean",
        "Remove stale sessions and apply TTL retention policy",
        "action");

    public static readonly CommandDefinition Watch = Define(
        WellKnownCommands.Watch,
        "Stream real-time events from a running Unity Editor",
        "stream",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("channel", "string", "Event channel: console, hierarchy, compilation, all (default: all)", required: false),
        Parameter("format", "string", "Output format: text, json (default: text)", required: false),
        Parameter("no-color", "bool", "Disable colored output", required: false));

    public static readonly CommandDefinition SceneSnapshot = Define(
        "scene snapshot",
        "Capture a snapshot of all scene objects and their serialized properties",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scenePath", "string", "Filter to a specific scene path", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SceneDiff = Define(
        "scene diff",
        "Compare two scene snapshots and report property-level changes",
        "query",
        Parameter("snap1", "string", "Path to base snapshot JSON file", required: false),
        Parameter("snap2", "string", "Path to head snapshot JSON file", required: false),
        Parameter("project", "string", "Path to Unity project (for --live mode)", required: false),
        Parameter("live", "bool", "Compare current scene against last snapshot", required: false),
        Parameter("epsilon", "double", "Float comparison threshold (default: 1e-6)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Schema = Define(
        WellKnownCommands.Schema,
        "Output machine-readable JSON schema of all available commands",
        "meta",
        Parameter("format", "string", "Output format: json (default: json)", required: false));

    public static readonly CommandDefinition Exec = Define(
        WellKnownCommands.Exec,
        "Execute a C# expression in the Unity Editor via reflection",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("code", "string", "C# expression to evaluate (e.g. 'EditorApplication.isPlaying = true')", required: false),
        Parameter("file", "string", "Path to a .cs script file to execute", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition Workflow = Define(
        WellKnownCommands.Workflow,
        "Execute a sequential workflow of unityctl commands from a JSON file",
        "action",
        Parameter("file", "string", "Path to workflow JSON definition file", required: true),
        Parameter("project", "string", "Default project path for steps that omit it", required: false),
        Parameter("json", "bool", "Output results as JSON", required: false));

    public static readonly CommandDefinition PlayMode = Define(
        WellKnownCommands.PlayMode,
        "Control Unity play mode (start, stop, pause)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("action", "string", "Play mode action: start, stop, pause", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PlayerSettingsGet = Define(
        "player-settings-get",
        "Get a PlayerSettings property value",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("key", "string", "Property name (e.g. companyName, productName)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PlayerSettingsSet = Define(
        "player-settings-set",
        "Set a PlayerSettings property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("key", "string", "Property name (e.g. companyName, productName)", required: true),
        Parameter("value", "string", "New value to set", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AssetRefresh = Define(
        WellKnownCommands.AssetRefresh,
        "Refresh the Unity AssetDatabase (IPC-only, async)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("noWait", "bool", "Return immediately after Accepted (do not poll)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition GameObjectCreate = Define(
        WellKnownCommands.GameObjectCreate,
        "Create a new GameObject in a scene",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("name", "string", "Name for the new GameObject", required: true),
        Parameter("parent", "string", "Parent GlobalObjectId (optional)", required: false),
        Parameter("scene", "string", "Target scene path (optional, defaults to active scene)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition GameObjectDelete = Define(
        WellKnownCommands.GameObjectDelete,
        "Delete a GameObject by GlobalObjectId",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject to delete", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition GameObjectSetActive = Define(
        WellKnownCommands.GameObjectSetActive,
        "Set a GameObject's active state",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("active", "bool", "Active state to set", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition GameObjectMove = Define(
        WellKnownCommands.GameObjectMove,
        "Reparent a GameObject (same-scene only)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject to move", required: true),
        Parameter("parent", "string", "GlobalObjectId of the new parent", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition GameObjectRename = Define(
        WellKnownCommands.GameObjectRename,
        "Rename a GameObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("name", "string", "New name for the GameObject", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SceneSave = Define(
        WellKnownCommands.SceneSave,
        "Save scene(s) to disk",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scene", "string", "Scene path to save (optional, defaults to active scene)", required: false),
        Parameter("all", "bool", "Save all dirty scenes", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SceneOpen = Define(
        WellKnownCommands.SceneOpen,
        "Open a scene in the Unity Editor",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Scene asset path to open", required: true),
        Parameter("mode", "string", "Open mode: single or additive (default: single)", required: false),
        Parameter("force", "bool", "Discard dirty scene changes when opening in single mode", required: false),
        Parameter("saveCurrentModified", "bool", "Save dirty scenes before opening in single mode", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SceneCreate = Define(
        WellKnownCommands.SceneCreate,
        "Create and save a new scene asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Scene asset path to create", required: true),
        Parameter("template", "string", "Scene template: default or empty (default: default)", required: false),
        Parameter("mode", "string", "Create mode: single or additive (default: single)", required: false),
        Parameter("force", "bool", "Discard dirty scene changes when creating in single mode", required: false),
        Parameter("saveCurrentModified", "bool", "Save dirty scenes before creating in single mode", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition ComponentAdd = Define(
        WellKnownCommands.ComponentAdd,
        "Add a component to a GameObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the target GameObject", required: true),
        Parameter("type", "string", "Component type name (e.g. UnityEngine.Rigidbody)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition ComponentRemove = Define(
        WellKnownCommands.ComponentRemove,
        "Remove a component by its GlobalObjectId",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("componentId", "string", "GlobalObjectId of the component to remove", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition ComponentSetProperty = Define(
        WellKnownCommands.ComponentSetProperty,
        "Set a serialized property on a component via SerializedObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("componentId", "string", "GlobalObjectId of the target component", required: true),
        Parameter("property", "string", "SerializedProperty path (e.g. m_Mass, m_LocalPosition.x)", required: true),
        Parameter("value", "string", "New value as JSON string", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition UndoCmd = Define(
        WellKnownCommands.Undo,
        "Undo the most recent Unity editor operation",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition RedoCmd = Define(
        WellKnownCommands.Redo,
        "Redo the most recently undone Unity editor operation",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    // Write API — Phase C-1: Asset CRUD
    public static readonly CommandDefinition AssetCreate = Define(
        WellKnownCommands.AssetCreate,
        "Create a new asset (ScriptableObject, etc.)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path (e.g. Assets/Data/MyConfig.asset)", required: true),
        Parameter("type", "string", "Asset type (e.g. ScriptableObject)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AssetCreateFolder = Define(
        WellKnownCommands.AssetCreateFolder,
        "Create a folder in the Assets directory",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("parent", "string", "Parent folder path (e.g. Assets/Data)", required: true),
        Parameter("name", "string", "New folder name", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AssetCopy = Define(
        WellKnownCommands.AssetCopy,
        "Copy an asset to a new path",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("source", "string", "Source asset path", required: true),
        Parameter("destination", "string", "Destination asset path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AssetMoveCmd = Define(
        WellKnownCommands.AssetMove,
        "Move or rename an asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("source", "string", "Source asset path", required: true),
        Parameter("destination", "string", "Destination asset path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AssetDeleteCmd = Define(
        WellKnownCommands.AssetDelete,
        "Delete an asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path to delete", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AssetImport = Define(
        WellKnownCommands.AssetImport,
        "Force reimport an asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path to reimport", required: true),
        Parameter("options", "string", "Import options (e.g. ForceUpdate)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    // Write API — Phase C-2: Prefab
    public static readonly CommandDefinition PrefabCreate = Define(
        WellKnownCommands.PrefabCreate,
        "Save a scene GameObject as a Prefab asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("target", "string", "GlobalObjectId of the scene GameObject", required: true),
        Parameter("path", "string", "Prefab asset path (e.g. Assets/Prefabs/MyPrefab.prefab)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PrefabUnpack = Define(
        WellKnownCommands.PrefabUnpack,
        "Unpack a prefab instance in the scene",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the prefab instance", required: true),
        Parameter("mode", "string", "Unpack mode: completely or outermost (default: outermost)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PrefabApply = Define(
        WellKnownCommands.PrefabApply,
        "Apply prefab instance overrides back to the Prefab asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the prefab instance", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PrefabEditCmd = Define(
        WellKnownCommands.PrefabEdit,
        "Edit a Prefab asset's contents (set property on root or child)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Prefab asset path", required: true),
        Parameter("property", "string", "SerializedProperty path on the root GameObject", required: true),
        Parameter("value", "string", "New value as JSON string", required: true),
        Parameter("childPath", "string", "Hierarchy path to child (e.g. Body/Arm)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    // Write API — Phase C-3: Package Manager + Project Settings
    public static readonly CommandDefinition PackageListCmd = Define(
        WellKnownCommands.PackageList,
        "List installed packages",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PackageAddCmd = Define(
        WellKnownCommands.PackageAdd,
        "Add a package to the project",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("package", "string", "Package identifier (e.g. com.unity.textmeshpro@3.0.6)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition PackageRemoveCmd = Define(
        WellKnownCommands.PackageRemove,
        "Remove a package from the project",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("package", "string", "Package name (e.g. com.unity.textmeshpro)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition ProjectSettingsGetCmd = Define(
        WellKnownCommands.ProjectSettingsGet,
        "Get a project settings property value",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scope", "string", "Settings scope: editor, graphics, quality, physics, time, audio", required: true),
        Parameter("property", "string", "SerializedProperty path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition ProjectSettingsSetCmd = Define(
        WellKnownCommands.ProjectSettingsSet,
        "Set a project settings property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scope", "string", "Settings scope: editor, graphics, quality, physics, time, audio", required: true),
        Parameter("property", "string", "SerializedProperty path", required: true),
        Parameter("value", "string", "New value", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    // Write API — Phase C-4: Material/Shader
    public static readonly CommandDefinition MaterialGetCmd = Define(
        WellKnownCommands.MaterialGet,
        "Get material properties",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path", required: true),
        Parameter("property", "string", "Property name (optional, returns all if omitted)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition MaterialSetCmd = Define(
        WellKnownCommands.MaterialSet,
        "Set a material property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path", required: true),
        Parameter("property", "string", "Property name (e.g. _Color, _MainTex)", required: true),
        Parameter("propertyType", "string", "Property type: color, float, texture, vector, int", required: true),
        Parameter("value", "string", "New value (JSON for color/vector, path for texture, number for float/int)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition MaterialSetShaderCmd = Define(
        WellKnownCommands.MaterialSetShader,
        "Change a material's shader",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path", required: true),
        Parameter("shader", "string", "Shader name (e.g. Standard, Universal Render Pipeline/Lit)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    // Write API — Phase C-5: Animation + UI
    public static readonly CommandDefinition AnimationCreateClipCmd = Define(
        WellKnownCommands.AnimationCreateClip,
        "Create an AnimationClip asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path for the clip (e.g. Assets/Animations/Walk.anim)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition AnimationCreateControllerCmd = Define(
        WellKnownCommands.AnimationCreateController,
        "Create an AnimatorController asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path (e.g. Assets/Animations/Player.controller)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition UiCanvasCreateCmd = Define(
        WellKnownCommands.UiCanvasCreate,
        "Create a Canvas with CanvasScaler and GraphicRaycaster",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("name", "string", "Canvas GameObject name (default: Canvas)", required: false),
        Parameter("renderMode", "string", "Render mode: ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace (default: ScreenSpaceOverlay)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition UiElementCreateCmd = Define(
        WellKnownCommands.UiElementCreate,
        "Create a UI element (Button, Text, Image, Panel)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("type", "string", "Element type: Button, Text, Image, Panel, InputField, Toggle, Slider, Dropdown, ScrollView", required: true),
        Parameter("name", "string", "Element name (optional)", required: false),
        Parameter("parent", "string", "GlobalObjectId of parent (Canvas or UI element)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition UiSetRectCmd = Define(
        WellKnownCommands.UiSetRect,
        "Set RectTransform properties on a UI element",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the UI element", required: true),
        Parameter("anchoredPosition", "string", "Anchored position as JSON [x,y]", required: false),
        Parameter("sizeDelta", "string", "Size delta as JSON [w,h]", required: false),
        Parameter("anchorMin", "string", "Anchor min as JSON [x,y]", required: false),
        Parameter("anchorMax", "string", "Anchor max as JSON [x,y]", required: false),
        Parameter("pivot", "string", "Pivot as JSON [x,y]", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static CommandDefinition[] All { get; } =
    [
        Init,
        EditorList,
        Ping,
        Status,
        Build,
        Test,
        Check,
        Tools,
        Log,
        SessionList,
        SessionStop,
        SessionClean,
        Watch,
        SceneSnapshot,
        SceneDiff,
        Schema,
        Exec,
        Workflow,
        PlayMode,
        PlayerSettingsGet,
        PlayerSettingsSet,
        AssetRefresh,
        GameObjectCreate,
        GameObjectDelete,
        GameObjectSetActive,
        GameObjectMove,
        GameObjectRename,
        SceneSave,
        SceneOpen,
        SceneCreate,
        ComponentAdd,
        ComponentRemove,
        ComponentSetProperty,
        UndoCmd,
        RedoCmd,
        // Phase C-1: Asset CRUD
        AssetCreate,
        AssetCreateFolder,
        AssetCopy,
        AssetMoveCmd,
        AssetDeleteCmd,
        AssetImport,
        // Phase C-2: Prefab
        PrefabCreate,
        PrefabUnpack,
        PrefabApply,
        PrefabEditCmd,
        // Phase C-3: Package Manager + Project Settings
        PackageListCmd,
        PackageAddCmd,
        PackageRemoveCmd,
        ProjectSettingsGetCmd,
        ProjectSettingsSetCmd,
        // Phase C-4: Material/Shader
        MaterialGetCmd,
        MaterialSetCmd,
        MaterialSetShaderCmd,
        // Phase C-5: Animation + UI
        AnimationCreateClipCmd,
        AnimationCreateControllerCmd,
        UiCanvasCreateCmd,
        UiElementCreateCmd,
        UiSetRectCmd
    ];

    private static CommandDefinition Define(
        string name,
        string description,
        string category,
        params CommandParameterDefinition[] parameters)
    {
        return new CommandDefinition
        {
            Name = name,
            Description = description,
            Category = category,
            Parameters = parameters
        };
    }

    private static CommandParameterDefinition Parameter(
        string name,
        string type,
        string description,
        bool required)
    {
        return new CommandParameterDefinition
        {
            Name = name,
            Type = type,
            Description = description,
            Required = required
        };
    }
}

public sealed class CommandDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public CommandParameterDefinition[] Parameters { get; set; } = [];
}

public sealed class CommandParameterDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}
