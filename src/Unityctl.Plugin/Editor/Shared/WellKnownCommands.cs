namespace Unityctl.Plugin.Editor.Shared
{
    public static class WellKnownCommands
    {
        public const string Ping = "ping";
        public const string Status = "status";
        public const string Build = "build";
        public const string Test = "test";
        public const string Check = "check";
        public const string TestResult = "test-result";
        public const string Watch = "watch";
        public const string SceneSnapshot = "scene-snapshot";
        public const string SceneDiff = "scene-diff";
        public const string Exec = "exec";

        // Write API — Phase A
        public const string PlayMode = "play-mode";
        public const string PlayerSettings = "player-settings";
        public const string AssetRefresh = "asset-refresh";
        public const string AssetRefreshResult = "asset-refresh-result";

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
        public const string MaterialGet = "material-get";
        public const string MaterialSet = "material-set";
        public const string MaterialSetShader = "material-set-shader";

        // Write API — Phase C-5: Animation + UI
        public const string AnimationCreateClip = "animation-create-clip";
        public const string AnimationCreateController = "animation-create-controller";
        public const string UiCanvasCreate = "ui-canvas-create";
        public const string UiElementCreate = "ui-element-create";
        public const string UiSetRect = "ui-set-rect";
    }
}
