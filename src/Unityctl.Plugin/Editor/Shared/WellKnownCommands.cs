namespace Unityctl.Plugin.Editor.Shared
{
    public static class WellKnownCommands
    {
        public const string Ping = "ping";
        public const string Status = "status";
        public const string Build = "build";
        public const string BuildProfileList = "build-profile-list";
        public const string BuildProfileGetActive = "build-profile-get-active";
        public const string BuildProfileSetActive = "build-profile-set-active";
        public const string BuildProfileSetActiveResult = "build-profile-set-active-result";
        public const string BuildTargetSwitch = "build-target-switch";
        public const string BuildTargetSwitchResult = "build-target-switch-result";
        public const string Test = "test";
        public const string Check = "check";
        public const string TestResult = "test-result";
        public const string Watch = "watch";
        public const string SceneSnapshot = "scene-snapshot";
        public const string SceneHierarchy = "scene-hierarchy";
        public const string SceneDiff = "scene-diff";
        public const string Exec = "exec";
        public const string BatchExecute = "batch-execute";

        // Write API — Phase A
        public const string PlayMode = "play-mode";
        public const string PlayerSettings = "player-settings";
        public const string AssetRefresh = "asset-refresh";
        public const string AssetRefreshResult = "asset-refresh-result";

        // Exploration API — Phase P0
        public const string AssetFind = "asset-find";
        public const string AssetGetInfo = "asset-get-info";
        public const string AssetGetDependencies = "asset-get-dependencies";
        public const string AssetReferenceGraph = "asset-reference-graph";
        public const string GameObjectFind = "gameobject-find";
        public const string GameObjectGet = "gameobject-get";
        public const string ComponentGet = "component-get";
        public const string BuildSettingsGetScenes = "build-settings-get-scenes";

        // Write API — Phase B
        public const string GameObjectCreate = "gameobject-create";
        public const string GameObjectDelete = "gameobject-delete";
        public const string GameObjectSetActive = "gameobject-set-active";
        public const string GameObjectMove = "gameobject-move";
        public const string GameObjectRename = "gameobject-rename";
        public const string SceneSave = "scene-save";
        public const string SceneOpen = "scene-open";
        public const string SceneCreate = "scene-create";

        // Write API — Phase B.5
        public const string ComponentAdd = "component-add";
        public const string ComponentRemove = "component-remove";
        public const string ComponentSetProperty = "component-set-property";

        // Write API — Phase B.75
        public const string Undo = "undo";
        public const string Redo = "redo";

        // Write API — Phase C-1: Asset CRUD
        public const string AssetCreate = "asset-create";
        public const string AssetCreateFolder = "asset-create-folder";
        public const string AssetCopy = "asset-copy";
        public const string AssetMove = "asset-move";
        public const string AssetDelete = "asset-delete";
        public const string AssetImport = "asset-import";

        // Write API — Phase C-2: Prefab
        public const string PrefabCreate = "prefab-create";
        public const string PrefabUnpack = "prefab-unpack";
        public const string PrefabApply = "prefab-apply";
        public const string PrefabEdit = "prefab-edit";

        // Write API — Phase C-3: Package Manager + Project Settings
        public const string PackageList = "package-list";
        public const string PackageAdd = "package-add";
        public const string PackageRemove = "package-remove";
        public const string ProjectSettingsGet = "project-settings-get";
        public const string ProjectSettingsSet = "project-settings-set";

        // Write API — Phase C-4: Material/Shader
        public const string MaterialCreate = "material-create";
        public const string MaterialGet = "material-get";
        public const string MaterialSet = "material-set";
        public const string MaterialSetShader = "material-set-shader";

        // Write API — Phase C-5: Animation + UI
        public const string AnimationCreateClip = "animation-create-clip";
        public const string AnimationCreateController = "animation-create-controller";
    public const string UiCanvasCreate = "ui-canvas-create";
    public const string UiElementCreate = "ui-element-create";
    public const string UiSetRect = "ui-set-rect";
    public const string UiFind = "ui-find";
    public const string UiGet = "ui-get";
    public const string UiToggle = "ui-toggle";
    public const string UiInput = "ui-input";

        // Script Editing v1
        public const string ScriptCreate = "script-create";
        public const string ScriptEdit = "script-edit";
        public const string ScriptDelete = "script-delete";
        public const string ScriptValidate = "script-validate";
        public const string ScriptValidateResult = "script-validate-result";

        // Script Editing v2
        public const string ScriptPatch = "script-patch";

        // Script Editing 확장
        public const string ScriptList = "script-list";

        // Script v2: diagnostics + refactoring
        public const string ScriptGetErrors = "script-get-errors";
        public const string ScriptFindRefs = "script-find-refs";
        public const string ScriptRenameSymbol = "script-rename-symbol";

        // P0 잔여분: Asset Labels + Build Settings
        public const string AssetGetLabels = "asset-get-labels";
        public const string AssetSetLabels = "asset-set-labels";
        public const string BuildSettingsSetScenes = "build-settings-set-scenes";

        // Screenshot / Visual Feedback — P3
    public const string Screenshot = "screenshot";

        // Tags & Layers
        public const string TagList = "tag-list";
        public const string TagAdd = "tag-add";
        public const string LayerList = "layer-list";
        public const string LayerSet = "layer-set";
        public const string GameObjectSetTag = "gameobject-set-tag";
        public const string GameObjectSetLayer = "gameobject-set-layer";

        // Editor Utility
        public const string ConsoleClear = "console-clear";
        public const string ConsoleGetCount = "console-get-count";
        public const string DefineSymbolsGet = "define-symbols-get";
        public const string DefineSymbolsSet = "define-symbols-set";
        public const string EditorPause = "editor-pause";
        public const string EditorFocusGameView = "editor-focus-gameview";
        public const string EditorFocusSceneView = "editor-focus-sceneview";

        // Lighting
        public const string LightingBake = "lighting-bake";
        public const string LightingBakeResult = "lighting-bake-result";
        public const string LightingCancel = "lighting-cancel";
        public const string LightingClear = "lighting-clear";
        public const string LightingGetSettings = "lighting-get-settings";
        public const string LightingSetSettings = "lighting-set-settings";

        // NavMesh
        public const string NavMeshBake = "navmesh-bake";
        public const string NavMeshClear = "navmesh-clear";
        public const string NavMeshGetSettings = "navmesh-get-settings";

        // Mesh Primitives
        public const string MeshCreatePrimitive = "mesh-create-primitive";

        // Project Validation
        public const string ProjectValidate = "project-validate";

        // Physics
        public const string PhysicsGetSettings = "physics-get-settings";
        public const string PhysicsSetSettings = "physics-set-settings";
        public const string PhysicsGetCollisionMatrix = "physics-get-collision-matrix";
        public const string PhysicsSetCollisionMatrix = "physics-set-collision-matrix";

        // Camera
        public const string CameraList = "camera-list";
        public const string CameraGet = "camera-get";

        // Texture Import
        public const string TextureGetImportSettings = "texture-get-import-settings";
        public const string TextureSetImportSettings = "texture-set-import-settings";

        // ScriptableObject
        public const string ScriptableObjectFind = "scriptableobject-find";
        public const string ScriptableObjectGet = "scriptableobject-get";
        public const string ScriptableObjectSetProperty = "scriptableobject-set-property";

        // Shader
        public const string ShaderFind = "shader-find";
        public const string ShaderGetProperties = "shader-get-properties";
    }
}
