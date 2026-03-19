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

    public static readonly CommandDefinition BuildProfileList = Define(
        WellKnownCommands.BuildProfileList,
        "List custom BuildProfile assets and synthesized platform profile rows",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("build-profile list");

    public static readonly CommandDefinition BuildProfileGetActive = Define(
        WellKnownCommands.BuildProfileGetActive,
        "Get the currently active build profile or active platform profile",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("build-profile get-active");

    public static readonly CommandDefinition BuildProfileSetActive = Define(
        WellKnownCommands.BuildProfileSetActive,
        "Set the active build profile and wait until the editor stabilizes",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("profile", "string", "Profile ref returned by build-profile list (asset path or platform:<target>)", required: true),
        Parameter("timeout", "int", "Timeout in seconds while waiting for stabilization (default: 900)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("build-profile set-active");

    public static readonly CommandDefinition BuildTargetSwitch = Define(
        WellKnownCommands.BuildTargetSwitch,
        "Switch the active build target and wait until the editor stabilizes",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("target", "string", "Target platform: StandaloneWindows64, StandaloneWindows, StandaloneOSX, StandaloneLinux64, Android, iOS, WebGL", required: true),
        Parameter("timeout", "int", "Timeout in seconds while waiting for stabilization (default: 900)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("build-target switch");

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

    public static readonly CommandDefinition Doctor = Define(
        "doctor",
        "Diagnose Unity project connectivity and plugin health",
        "meta",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false));

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
        Parameter("includeInactive", "bool", "Include inactive GameObjects in the snapshot", required: false),
        Parameter("json", "bool", "Output as JSON", required: false));

    public static readonly CommandDefinition SceneHierarchy = Define(
        WellKnownCommands.SceneHierarchy,
        "Capture a lightweight nested hierarchy tree for loaded scenes",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scenePath", "string", "Filter to a specific scene path", required: false),
        Parameter("includeInactive", "bool", "Include inactive GameObjects in the hierarchy", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("scene hierarchy");

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
        Parameter("json", "bool", "Output results as JSON", required: false)).WithCli("workflow run");

    public static readonly CommandDefinition BatchExecute = Define(
        WellKnownCommands.BatchExecute,
        "Execute multiple undo-backed edit commands in one IPC round-trip with rollback on partial failure",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("commands", "json-array", "Array of command objects: [{\"command\":\"gameobject-create\",\"parameters\":{...}}]", required: false),
        Parameter("file", "string", "Path to a JSON file containing the commands array", required: false),
        Parameter("rollbackOnFailure", "bool", "Rollback completed commands if any step fails (default: true)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("batch execute");

    public static readonly CommandDefinition PlayMode = Define(
        WellKnownCommands.PlayMode,
        "Control Unity play mode (start, stop, pause)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("action", "string", "Play mode action: start, stop, pause", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("play <start|stop|pause>");

    public static readonly CommandDefinition PlayerSettingsGet = Define(
        "player-settings-get",
        "Get a PlayerSettings property value",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("key", "string", "Property name (e.g. companyName, productName)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("player-settings get");

    public static readonly CommandDefinition PlayerSettingsSet = Define(
        "player-settings-set",
        "Set a PlayerSettings property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("key", "string", "Property name (e.g. companyName, productName)", required: true),
        Parameter("value", "string", "New value to set", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("player-settings set");

    public static readonly CommandDefinition AssetRefresh = Define(
        WellKnownCommands.AssetRefresh,
        "Refresh the Unity AssetDatabase (IPC-only, async)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("noWait", "bool", "Return immediately after Accepted (do not poll)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset refresh");

    public static readonly CommandDefinition AssetFind = Define(
        WellKnownCommands.AssetFind,
        "Find assets using Unity AssetDatabase.FindAssets filter syntax",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("filter", "string", "AssetDatabase.FindAssets filter (for example: t:Scene, l:tag)", required: true),
        Parameter("folder", "string", "Optional root folder to search under", required: false),
        Parameter("limit", "int", "Maximum number of results to return", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset find");

    public static readonly CommandDefinition AssetGetInfo = Define(
        WellKnownCommands.AssetGetInfo,
        "Get summary information for a single asset path",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path (for example: Assets/Scenes/Main.unity)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset get-info");

    public static readonly CommandDefinition AssetGetDependencies = Define(
        WellKnownCommands.AssetGetDependencies,
        "Get asset dependency paths using Unity AssetDatabase.GetDependencies",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path (for example: Assets/Scenes/Main.unity)", required: true),
        Parameter("recursive", "bool", "Include indirect dependencies (default: true)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset get-dependencies");

    public static readonly CommandDefinition AssetReferenceGraph = Define(
        WellKnownCommands.AssetReferenceGraph,
        "Find reverse references to an asset by scanning candidate assets and their dependencies",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Target asset path (for example: Assets/Materials/My.mat)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset reference-graph");

    public static readonly CommandDefinition BuildSettingsGetScenes = Define(
        WellKnownCommands.BuildSettingsGetScenes,
        "Get the current Build Settings scene list from EditorBuildSettings.scenes",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("build-settings get-scenes");

    public static readonly CommandDefinition GameObjectFind = Define(
        WellKnownCommands.GameObjectFind,
        "Find GameObjects in loaded scenes using narrow query filters",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("name", "string", "Case-insensitive partial match on GameObject name", required: false),
        Parameter("tag", "string", "Exact match on GameObject tag", required: false),
        Parameter("layer", "string", "Exact match on layer name or numeric layer index", required: false),
        Parameter("component", "string", "Exact match on component type name or full type name", required: false),
        Parameter("scene", "string", "Scene asset path filter", required: false),
        Parameter("includeInactive", "bool", "Include inactive GameObjects in the search", required: false),
        Parameter("limit", "int", "Maximum number of results to return", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject find");

    public static readonly CommandDefinition GameObjectGet = Define(
        WellKnownCommands.GameObjectGet,
        "Get summary details for a single GameObject by GlobalObjectId",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject get");

    public static readonly CommandDefinition ComponentGet = Define(
        WellKnownCommands.ComponentGet,
        "Get summary or serialized property details for a single component",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("componentId", "string", "GlobalObjectId of the component", required: true),
        Parameter("property", "string", "SerializedProperty path to read (optional, returns all if omitted)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("component get");

    public static readonly CommandDefinition GameObjectCreate = Define(
        WellKnownCommands.GameObjectCreate,
        "Create a new GameObject in a scene",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("name", "string", "Name for the new GameObject", required: true),
        Parameter("parent", "string", "Parent GlobalObjectId (optional)", required: false),
        Parameter("scene", "string", "Target scene path (optional, defaults to active scene)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject create");

    public static readonly CommandDefinition GameObjectDelete = Define(
        WellKnownCommands.GameObjectDelete,
        "Delete a GameObject by GlobalObjectId",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject to delete", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject delete");

    public static readonly CommandDefinition GameObjectSetActive = Define(
        WellKnownCommands.GameObjectSetActive,
        "Set a GameObject's active state",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("active", "bool", "Active state to set", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject set-active");

    public static readonly CommandDefinition GameObjectMove = Define(
        WellKnownCommands.GameObjectMove,
        "Reparent a GameObject (same-scene only)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject to move", required: true),
        Parameter("parent", "string", "GlobalObjectId of the new parent", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject move");

    public static readonly CommandDefinition GameObjectRename = Define(
        WellKnownCommands.GameObjectRename,
        "Rename a GameObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("name", "string", "New name for the GameObject", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject rename");

    public static readonly CommandDefinition SceneSave = Define(
        WellKnownCommands.SceneSave,
        "Save scene(s) to disk",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scene", "string", "Scene path to save (optional, defaults to active scene)", required: false),
        Parameter("all", "bool", "Save all dirty scenes", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("scene save");

    public static readonly CommandDefinition SceneOpen = Define(
        WellKnownCommands.SceneOpen,
        "Open a scene in the Unity Editor",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Scene asset path to open", required: true),
        Parameter("mode", "string", "Open mode: single or additive (default: single)", required: false),
        Parameter("force", "bool", "Discard dirty scene changes when opening in single mode", required: false),
        Parameter("saveCurrentModified", "bool", "Save dirty scenes before opening in single mode", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("scene open");

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
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("scene create");

    public static readonly CommandDefinition ComponentAdd = Define(
        WellKnownCommands.ComponentAdd,
        "Add a component to a GameObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the target GameObject", required: true),
        Parameter("type", "string", "Component type name (e.g. UnityEngine.Rigidbody)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("component add");

    public static readonly CommandDefinition ComponentRemove = Define(
        WellKnownCommands.ComponentRemove,
        "Remove a component by its GlobalObjectId",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("componentId", "string", "GlobalObjectId of the component to remove", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("component remove");

    public static readonly CommandDefinition ComponentSetProperty = Define(
        WellKnownCommands.ComponentSetProperty,
        "Set a serialized property on a component via SerializedObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("componentId", "string", "GlobalObjectId of the target component", required: true),
        Parameter("property", "string", "SerializedProperty path (e.g. m_Mass, m_LocalPosition.x)", required: true),
        Parameter("value", "string", "New value as JSON string", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("component set-property");

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
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset create");

    public static readonly CommandDefinition AssetCreateFolder = Define(
        WellKnownCommands.AssetCreateFolder,
        "Create a folder in the Assets directory",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("parent", "string", "Parent folder path (e.g. Assets/Data)", required: true),
        Parameter("name", "string", "New folder name", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset create-folder");

    public static readonly CommandDefinition AssetCopy = Define(
        WellKnownCommands.AssetCopy,
        "Copy an asset to a new path",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("source", "string", "Source asset path", required: true),
        Parameter("destination", "string", "Destination asset path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset copy");

    public static readonly CommandDefinition AssetMoveCmd = Define(
        WellKnownCommands.AssetMove,
        "Move or rename an asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("source", "string", "Source asset path", required: true),
        Parameter("destination", "string", "Destination asset path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset move");

    public static readonly CommandDefinition AssetDeleteCmd = Define(
        WellKnownCommands.AssetDelete,
        "Delete an asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path to delete", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset delete");

    public static readonly CommandDefinition AssetImport = Define(
        WellKnownCommands.AssetImport,
        "Force reimport an asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path to reimport", required: true),
        Parameter("options", "string", "Import options (e.g. ForceUpdate)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset import");

    // Write API — Phase C-2: Prefab
    public static readonly CommandDefinition PrefabCreate = Define(
        WellKnownCommands.PrefabCreate,
        "Save a scene GameObject as a Prefab asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("target", "string", "GlobalObjectId of the scene GameObject", required: true),
        Parameter("path", "string", "Prefab asset path (e.g. Assets/Prefabs/MyPrefab.prefab)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("prefab create");

    public static readonly CommandDefinition PrefabUnpack = Define(
        WellKnownCommands.PrefabUnpack,
        "Unpack a prefab instance in the scene",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the prefab instance", required: true),
        Parameter("mode", "string", "Unpack mode: completely or outermost (default: outermost)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("prefab unpack");

    public static readonly CommandDefinition PrefabApply = Define(
        WellKnownCommands.PrefabApply,
        "Apply prefab instance overrides back to the Prefab asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the prefab instance", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("prefab apply");

    public static readonly CommandDefinition PrefabEditCmd = Define(
        WellKnownCommands.PrefabEdit,
        "Edit a Prefab asset's contents (set property on root or child)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Prefab asset path", required: true),
        Parameter("property", "string", "SerializedProperty path on the root GameObject", required: true),
        Parameter("value", "string", "New value as JSON string", required: true),
        Parameter("childPath", "string", "Hierarchy path to child (e.g. Body/Arm)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("prefab edit");

    // Write API — Phase C-3: Package Manager + Project Settings
    public static readonly CommandDefinition PackageListCmd = Define(
        WellKnownCommands.PackageList,
        "List installed packages",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("package list");

    public static readonly CommandDefinition PackageAddCmd = Define(
        WellKnownCommands.PackageAdd,
        "Add a package to the project",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("package", "string", "Package identifier (e.g. com.unity.textmeshpro@3.0.6)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("package add");

    public static readonly CommandDefinition PackageRemoveCmd = Define(
        WellKnownCommands.PackageRemove,
        "Remove a package from the project",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("package", "string", "Package name (e.g. com.unity.textmeshpro)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("package remove");

    public static readonly CommandDefinition ProjectSettingsGetCmd = Define(
        WellKnownCommands.ProjectSettingsGet,
        "Get a project settings property value",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scope", "string", "Settings scope: editor, graphics, quality, physics, time, audio", required: true),
        Parameter("property", "string", "SerializedProperty path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("project-settings get");

    public static readonly CommandDefinition ProjectSettingsSetCmd = Define(
        WellKnownCommands.ProjectSettingsSet,
        "Set a project settings property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scope", "string", "Settings scope: editor, graphics, quality, physics, time, audio", required: true),
        Parameter("property", "string", "SerializedProperty path", required: true),
        Parameter("value", "string", "New value", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("project-settings set");

    // Write API — Phase C-4: Material/Shader
    public static readonly CommandDefinition MaterialCreateCmd = Define(
        WellKnownCommands.MaterialCreate,
        "Create a new Material asset with a specified shader",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path (e.g. Assets/Materials/MyMat.mat)", required: true),
        Parameter("shader", "string", "Shader name (default: Standard)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("material create");

    public static readonly CommandDefinition MaterialGetCmd = Define(
        WellKnownCommands.MaterialGet,
        "Get material properties",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path", required: true),
        Parameter("property", "string", "Property name (optional, returns all if omitted)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("material get");

    public static readonly CommandDefinition MaterialSetCmd = Define(
        WellKnownCommands.MaterialSet,
        "Set a material property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path", required: true),
        Parameter("property", "string", "Property name (e.g. _Color, _MainTex)", required: true),
        Parameter("propertyType", "string", "Property type: color, float, texture, vector, int", required: true),
        Parameter("value", "string", "New value (JSON for color/vector, path for texture, number for float/int)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("material set");

    public static readonly CommandDefinition MaterialSetShaderCmd = Define(
        WellKnownCommands.MaterialSetShader,
        "Change a material's shader",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Material asset path", required: true),
        Parameter("shader", "string", "Shader name (e.g. Standard, Universal Render Pipeline/Lit)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("material set-shader");

    // Write API — Phase C-5: Animation + UI
    public static readonly CommandDefinition AnimationCreateClipCmd = Define(
        WellKnownCommands.AnimationCreateClip,
        "Create an AnimationClip asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path for the clip (e.g. Assets/Animations/Walk.anim)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("animation create-clip");

    public static readonly CommandDefinition AnimationCreateControllerCmd = Define(
        WellKnownCommands.AnimationCreateController,
        "Create an AnimatorController asset",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path (e.g. Assets/Animations/Player.controller)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("animation create-controller");

    public static readonly CommandDefinition UiCanvasCreateCmd = Define(
        WellKnownCommands.UiCanvasCreate,
        "Create a Canvas with CanvasScaler and GraphicRaycaster",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("name", "string", "Canvas GameObject name (default: Canvas)", required: false),
        Parameter("renderMode", "string", "Render mode: ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace (default: ScreenSpaceOverlay)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("ui canvas-create");

    public static readonly CommandDefinition UiElementCreateCmd = Define(
        WellKnownCommands.UiElementCreate,
        "Create a UI element (Button, Text, Image, Panel)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("type", "string", "Element type: Button, Text, Image, Panel, InputField, Toggle, Slider, Dropdown, ScrollView", required: true),
        Parameter("name", "string", "Element name (optional)", required: false),
        Parameter("parent", "string", "GlobalObjectId of parent (Canvas or UI element)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("ui element-create");

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
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("ui set-rect");

    // Script Editing v1
    public static readonly CommandDefinition ScriptCreateCmd = Define(
        WellKnownCommands.ScriptCreate,
        "Create a new C# script file",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path for the script (e.g. Assets/Scripts/Player.cs)", required: true),
        Parameter("className", "string", "C# class name (must match filename)", required: true),
        Parameter("namespace", "string", "C# namespace", required: false),
        Parameter("baseType", "string", "Base class (default: MonoBehaviour). Known: MonoBehaviour, ScriptableObject, Editor, EditorWindow", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("script create");

    public static readonly CommandDefinition ScriptEditCmd = Define(
        WellKnownCommands.ScriptEdit,
        "Replace the contents of an existing C# script file",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path of the script to edit", required: true),
        Parameter("content", "string", "New file content (whole-file replace)", required: false),
        Parameter("contentFile", "string", "Local file path to read content from (CLI-only convenience)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("script edit");

    public static readonly CommandDefinition ScriptDeleteCmd = Define(
        WellKnownCommands.ScriptDelete,
        "Delete a C# script file",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path of the script to delete", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("script delete");

    public static readonly CommandDefinition ScriptValidateCmd = Define(
        WellKnownCommands.ScriptValidate,
        "Trigger script compilation and return diagnostics",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Script path to filter results by assembly", required: false),
        Parameter("wait", "bool", "Wait for compilation (default: true)", required: false),
        Parameter("timeout", "int", "Timeout in seconds (default: 300)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("script validate");

    // Script Editing v2
    public static readonly CommandDefinition ScriptPatchCmd = Define(
        WellKnownCommands.ScriptPatch,
        "Apply line-level patch to a C# script (insert/delete/replace lines)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path of the script to patch", required: true),
        Parameter("startLine", "int", "1-based line number to start patch at (0 = insert at beginning)", required: true),
        Parameter("deleteCount", "int", "Number of lines to delete starting at startLine (default: 0)", required: false),
        Parameter("insertContent", "string", "Content to insert at startLine (newline-separated, optional)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("script patch");

    // P0 잔여분: Asset Labels
    public static readonly CommandDefinition AssetGetLabels = Define(
        WellKnownCommands.AssetGetLabels,
        "Get all labels attached to an asset",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset get-labels");

    public static readonly CommandDefinition AssetSetLabels = Define(
        WellKnownCommands.AssetSetLabels,
        "Set labels on an asset (replaces all existing labels)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("path", "string", "Asset path", required: true),
        Parameter("labels", "string", "Comma-separated labels (replaces all existing labels)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("asset set-labels");

    // P0 잔여분: Build Settings Set Scenes
    public static readonly CommandDefinition BuildSettingsSetScenes = Define(
        WellKnownCommands.BuildSettingsSetScenes,
        "Set the Build Settings scene list",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("scenes", "string", "Comma-separated scene paths (e.g. Assets/Scenes/Main.unity,Assets/Scenes/Menu.unity)", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("build-settings set-scenes");

    // Screenshot / Visual Feedback — P3
    public static readonly CommandDefinition ScreenshotCapture = Define(
        WellKnownCommands.Screenshot,
        "Capture a screenshot of the Unity Scene View or Game View camera",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("view", "string", "View to capture: scene or game (default: scene)", required: false),
        Parameter("width", "int", "Image width in pixels (default: 1920)", required: false),
        Parameter("height", "int", "Image height in pixels (default: 1080)", required: false),
        Parameter("format", "string", "Image format: png or jpg (default: png)", required: false),
        Parameter("quality", "int", "JPG quality 1-100 (default: 75, ignored for png)", required: false),
        Parameter("output", "string", "File path to save the image (optional, base64-only by default)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("screenshot capture");

    // Tags & Layers
    public static readonly CommandDefinition TagList = Define(
        WellKnownCommands.TagList,
        "List all tags defined in the project",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("tag list");

    public static readonly CommandDefinition TagAdd = Define(
        WellKnownCommands.TagAdd,
        "Add a new tag to the project TagManager",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("name", "string", "Tag name to add", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("tag add");

    public static readonly CommandDefinition LayerListDef = Define(
        WellKnownCommands.LayerList,
        "List all 32 layer slots with names and built-in flags",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("layer list");

    public static readonly CommandDefinition LayerSetDef = Define(
        WellKnownCommands.LayerSet,
        "Set a user layer name (index 8-31)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("index", "int", "Layer index (8-31)", required: true),
        Parameter("name", "string", "Layer name to set", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("layer set");

    public static readonly CommandDefinition GameObjectSetTag = Define(
        WellKnownCommands.GameObjectSetTag,
        "Set the tag on a GameObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("tag", "string", "Tag to assign", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject set-tag");

    public static readonly CommandDefinition GameObjectSetLayer = Define(
        WellKnownCommands.GameObjectSetLayer,
        "Set the layer on a GameObject",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("id", "string", "GlobalObjectId of the GameObject", required: true),
        Parameter("layer", "string", "Layer name or index to assign", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("gameobject set-layer");

    // Editor Utility
    public static readonly CommandDefinition ConsoleClear = Define(
        WellKnownCommands.ConsoleClear,
        "Clear the Unity Editor console log",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("console clear");

    public static readonly CommandDefinition ConsoleGetCount = Define(
        WellKnownCommands.ConsoleGetCount,
        "Get the count of log messages, warnings, and errors in the Unity console",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("console get-count");

    public static readonly CommandDefinition DefineSymbolsGetDef = Define(
        WellKnownCommands.DefineSymbolsGet,
        "Get scripting define symbols for the active build target",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("target", "string", "Named build target (optional, defaults to active)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("define-symbols get");

    public static readonly CommandDefinition DefineSymbolsSetDef = Define(
        WellKnownCommands.DefineSymbolsSet,
        "Set scripting define symbols for the active build target",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("symbols", "string", "Semicolon-separated define symbols", required: true),
        Parameter("target", "string", "Named build target (optional, defaults to active)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("define-symbols set");

    // Lighting
    public static readonly CommandDefinition LightingBake = Define(
        WellKnownCommands.LightingBake,
        "Start an asynchronous lightmap bake",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("timeout", "int", "Timeout in seconds while waiting for bake completion (default: 3600)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("lighting bake");

    public static readonly CommandDefinition LightingCancel = Define(
        WellKnownCommands.LightingCancel,
        "Cancel a running lightmap bake",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("lighting cancel");

    public static readonly CommandDefinition LightingClear = Define(
        WellKnownCommands.LightingClear,
        "Clear baked lightmap data from the scene",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("lighting clear");

    public static readonly CommandDefinition LightingGetSettings = Define(
        WellKnownCommands.LightingGetSettings,
        "Get the current scene lighting settings",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("lighting get-settings");

    public static readonly CommandDefinition LightingSetSettings = Define(
        WellKnownCommands.LightingSetSettings,
        "Set a lighting settings property value",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("property", "string", "LightingSettings property path (e.g. m_LightmapResolution)", required: true),
        Parameter("value", "string", "New value", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("lighting set-settings");

    // NavMesh
    public static readonly CommandDefinition NavMeshBakeDef = Define(
        WellKnownCommands.NavMeshBake,
        "Build the NavMesh for the current scene",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("navmesh bake");

    public static readonly CommandDefinition NavMeshClearDef = Define(
        WellKnownCommands.NavMeshClear,
        "Clear all NavMesh data",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("navmesh clear");

    public static readonly CommandDefinition NavMeshGetSettingsDef = Define(
        WellKnownCommands.NavMeshGetSettings,
        "Get NavMesh build settings for all agent types",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("navmesh get-settings");

    // Editor Utility 확장
    public static readonly CommandDefinition EditorPauseDef = Define(
        WellKnownCommands.EditorPause,
        "Toggle or set the Unity Editor pause state",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("action", "string", "Pause action: toggle, pause, or unpause (default: toggle)", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("editor pause");

    public static readonly CommandDefinition EditorFocusGameViewDef = Define(
        WellKnownCommands.EditorFocusGameView,
        "Focus the Game View window in the Unity Editor",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("editor focus-gameview");

    public static readonly CommandDefinition EditorFocusSceneViewDef = Define(
        WellKnownCommands.EditorFocusSceneView,
        "Focus the Scene View window in the Unity Editor",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("editor focus-sceneview");

    // Script 확장
    public static readonly CommandDefinition ScriptListCmdDef = Define(
        WellKnownCommands.ScriptList,
        "List MonoScript assets in the project",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("folder", "string", "Root folder to search (default: Assets)", required: false),
        Parameter("filter", "string", "Case-insensitive name filter", required: false),
        Parameter("limit", "int", "Maximum number of results", required: false),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("script list");

    // Physics
    public static readonly CommandDefinition PhysicsGetSettings = Define(
        WellKnownCommands.PhysicsGetSettings,
        "Get all physics settings from DynamicsManager",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("physics get-settings");

    public static readonly CommandDefinition PhysicsSetSettings = Define(
        WellKnownCommands.PhysicsSetSettings,
        "Set a physics settings property value in DynamicsManager",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("property", "string", "DynamicsManager property path (e.g. m_Gravity, m_DefaultSolverIterations)", required: true),
        Parameter("value", "string", "New value", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("physics set-settings");

    public static readonly CommandDefinition PhysicsGetCollisionMatrix = Define(
        WellKnownCommands.PhysicsGetCollisionMatrix,
        "Get the 32x32 layer collision matrix showing which layers collide",
        "query",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("physics get-collision-matrix");

    public static readonly CommandDefinition PhysicsSetCollisionMatrix = Define(
        WellKnownCommands.PhysicsSetCollisionMatrix,
        "Set collision between two layers (no Undo support — runtime API)",
        "action",
        Parameter("project", "string", "Path to Unity project", required: true),
        Parameter("layer1", "string", "First layer (name or index 0-31)", required: true),
        Parameter("layer2", "string", "Second layer (name or index 0-31)", required: true),
        Parameter("ignore", "bool", "true to disable collision, false to enable", required: true),
        Parameter("json", "bool", "Output as JSON", required: false)).WithCli("physics set-collision-matrix");

    public static CommandDefinition[] All { get; } =
    [
        Init,
        EditorList,
        Ping,
        Status,
        Build,
        BuildProfileList,
        BuildProfileGetActive,
        BuildProfileSetActive,
        BuildTargetSwitch,
        Test,
        Check,
        Tools,
        Doctor,
        Log,
        SessionList,
        SessionStop,
        SessionClean,
        Watch,
        SceneSnapshot,
        SceneHierarchy,
        SceneDiff,
        Schema,
        Exec,
        Workflow,
        BatchExecute,
        PlayMode,
        PlayerSettingsGet,
        PlayerSettingsSet,
        AssetRefresh,
        AssetFind,
        AssetGetInfo,
        AssetGetDependencies,
        AssetReferenceGraph,
        BuildSettingsGetScenes,
        GameObjectFind,
        GameObjectGet,
        ComponentGet,
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
        MaterialCreateCmd,
        MaterialGetCmd,
        MaterialSetCmd,
        MaterialSetShaderCmd,
        // Phase C-5: Animation + UI
        AnimationCreateClipCmd,
        AnimationCreateControllerCmd,
        UiCanvasCreateCmd,
        UiElementCreateCmd,
        UiSetRectCmd,
        // Script Editing v1
        ScriptCreateCmd,
        ScriptEditCmd,
        ScriptDeleteCmd,
        ScriptValidateCmd,
        ScriptPatchCmd,
        // P0 잔여분: Asset Labels + Build Settings
        AssetGetLabels,
        AssetSetLabels,
        BuildSettingsSetScenes,
        // Screenshot / Visual Feedback — P3
        ScreenshotCapture,
        // Tags & Layers
        TagList,
        TagAdd,
        LayerListDef,
        LayerSetDef,
        GameObjectSetTag,
        GameObjectSetLayer,
        // Editor Utility
        ConsoleClear,
        ConsoleGetCount,
        DefineSymbolsGetDef,
        DefineSymbolsSetDef,
        // Lighting
        LightingBake,
        LightingCancel,
        LightingClear,
        LightingGetSettings,
        LightingSetSettings,
        // NavMesh
        NavMeshBakeDef,
        NavMeshClearDef,
        NavMeshGetSettingsDef,
        // Editor Utility 확장
        EditorPauseDef,
        EditorFocusGameViewDef,
        EditorFocusSceneViewDef,
        // Script 확장
        ScriptListCmdDef,
        // Physics
        PhysicsGetSettings,
        PhysicsSetSettings,
        PhysicsGetCollisionMatrix,
        PhysicsSetCollisionMatrix
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
            CliFlag = "--" + CamelToKebab(name),
            Type = type,
            Description = description,
            Required = required
        };
    }

    private static string CamelToKebab(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Contains('-'))
            return input;

        var sb = new System.Text.StringBuilder(input.Length + 4);
        for (var i = 0; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && i > 0)
            {
                sb.Append('-');
                sb.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                sb.Append(char.ToLowerInvariant(input[i]));
            }
        }
        return sb.ToString();
    }
}

public sealed class CommandDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cliName")]
    public string? CliName { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public CommandParameterDefinition[] Parameters { get; set; } = [];

    public CommandDefinition WithCli(string cliName)
    {
        CliName = cliName;
        return this;
    }
}

public sealed class CommandParameterDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("cliFlag")]
    public string CliFlag { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}
