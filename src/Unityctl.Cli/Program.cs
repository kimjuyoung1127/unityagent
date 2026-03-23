using ConsoleAppFramework;
using Unityctl.Cli.Commands;

var app = ConsoleApp.Create();

app.Add("", () => Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — A deterministic control plane for Unity\nUse --help for available commands."));

app.Add("init", (string project, string? source = null) =>
    InitCommand.Execute(project, source));

app.Add("editor list", (bool json = false) =>
    EditorCommands.List(json));

app.Add("editor instances", (bool json = false) =>
    EditorCommands.Instances(json));

app.Add("editor current", (bool json = false) =>
    EditorCommands.Current(json));

app.Add("editor select", (string? project = null, int? pid = null, bool json = false) =>
    EditorCommands.Select(project, pid, json));

app.Add("ping", (string? project = null, bool json = false) =>
    PingCommand.Execute(project, json));

app.Add("status", (string? project = null, bool wait = false, bool json = false) =>
    StatusCommand.Execute(project, wait, json));

app.Add("build", (string project, string target = "StandaloneWindows64", string? output = null, bool dryRun = false, bool json = false) =>
    BuildCommand.Execute(project, target, output, dryRun, json));

app.Add("build-profile list", (string project, bool json = false) =>
    BuildProfileCommand.List(project, json));

app.Add("build-profile get-active", (string project, bool json = false) =>
    BuildProfileCommand.GetActive(project, json));

app.Add("build-profile set-active", (string project, string profile, int timeout = 900, bool json = false) =>
    BuildProfileCommand.SetActive(project, profile, timeout, json));

app.Add("build-target switch", (string project, string target, int timeout = 900, bool json = false) =>
    BuildTargetCommand.Switch(project, target, timeout, json));

app.Add("test", (string project, string mode = "edit", string? filter = null, bool noWait = false, int timeout = 300, bool json = false) =>
    TestCommand.Execute(project, mode, filter, !noWait, timeout, json));

app.Add("check", (string? project = null, string type = "compile", bool json = false) =>
    CheckCommand.Execute(project, type, json));

app.Add("tools", (bool json = false) =>
    ToolsCommand.Execute(json));

app.Add("doctor", (string? project = null, bool json = false) =>
    DoctorCommand.Execute(project, json));

app.Add("project validate", (string project, bool json = false) =>
    ProjectValidateCommand.Execute(project, json));

app.Add("mesh create-primitive", (string project, string type, string? name = null,
    string? position = null, string? rotation = null, string? scale = null,
    string? material = null, string? parent = null, bool json = false) =>
    MeshCommand.CreatePrimitive(project, type, name, position, rotation, scale, material, parent, json));

app.Add("log", (
        int? last = null,
        bool tail = false,
        string? op = null,
        string? level = null,
        string? since = null,
        bool json = false,
        bool prune = false,
        bool stats = false) =>
    LogCommand.Execute(last, tail, op, level, since, json, prune, stats));

app.Add("session list", (bool json = false) =>
    SessionCommand.List(json));

app.Add("session stop", (string id, bool json = false) =>
    SessionCommand.Stop(id, json));

app.Add("session clean", () =>
    SessionCommand.Clean());

app.Add("watch", (string project, string channel = "all", string format = "text", bool noColor = false) =>
    WatchCommand.Execute(project, channel, format, noColor));

app.Add("scene snapshot", (string project, string? scenePath = null, bool includeInactive = false, bool json = false) =>
    SceneCommand.Snapshot(project, scenePath, includeInactive, json));

app.Add("scene hierarchy", (string project, string? scenePath = null, bool includeInactive = false, int maxDepth = -1, bool summary = false, bool json = false) =>
    SceneCommand.Hierarchy(project, scenePath, includeInactive, maxDepth, summary, json));

app.Add("scene diff", (
        string snap1 = "",
        string snap2 = "",
        string? project = null,
        bool live = false,
        double epsilon = 1e-6,
        bool json = false) =>
    SceneCommand.Diff(snap1, snap2, project, live, epsilon, json));

app.Add("schema", (string format = "json") =>
    SchemaCommand.Execute(format));

app.Add("exec", (string project, string? code = null, string? file = null, bool json = false) =>
    ExecCommand.Execute(project, code, file, json));

app.Add("workflow run", (string file, string? project = null, bool json = false) =>
    WorkflowCommand.Run(file, project, json));

app.Add("workflow verify", (string file, string project, string? artifactsDir = null, bool inlineEvidence = false, bool json = false) =>
    WorkflowCommand.Verify(file, project, artifactsDir, inlineEvidence, json));

app.Add("batch execute", (string project, string? commands = null, string? file = null, bool rollbackOnFailure = true, bool json = false) =>
    BatchCommand.Execute(project, commands, file, rollbackOnFailure, json));

app.Add("play start", (string project, bool json = false) =>
    PlayModeCommand.Execute(project, "start", json));

app.Add("play stop", (string project, bool json = false) =>
    PlayModeCommand.Execute(project, "stop", json));

app.Add("play pause", (string project, bool json = false) =>
    PlayModeCommand.Execute(project, "pause", json));

app.Add("player-settings get", (string project, string key, bool json = false) =>
    PlayerSettingsCommand.Get(project, key, json));

app.Add("player-settings set", (string project, string key, string value, bool json = false) =>
    PlayerSettingsCommand.Set(project, key, value, json));

app.Add("asset refresh", (string project, bool noWait = false, bool json = false) =>
    AssetCommand.Refresh(project, noWait, json));

app.Add("asset find", (string project, string filter, string? folder = null, int? limit = null, bool json = false) =>
    AssetCommand.Find(project, filter, folder, limit, json));

app.Add("asset get-info", (string project, string path, bool json = false) =>
    AssetCommand.GetInfo(project, path, json));

app.Add("asset get-dependencies", (string project, string path, string recursive = "true", bool json = false) =>
    AssetCommand.GetDependencies(project, path, recursive, json));

app.Add("asset reference-graph", (string project, string path, bool json = false) =>
    AssetCommand.ReferenceGraph(project, path, json));

app.Add("asset get-labels", (string project, string path, bool json = false) =>
    AssetCommand.GetLabels(project, path, json));

app.Add("asset set-labels", (string project, string path, string labels, bool json = false) =>
    AssetCommand.SetLabels(project, path, labels, json));

app.Add("build-settings get-scenes", (string project, bool json = false) =>
    BuildSettingsCommand.GetScenes(project, json));

app.Add("build-settings set-scenes", (string project, string scenes, bool json = false) =>
    BuildSettingsCommand.SetScenes(project, scenes, json));

app.Add("gameobject find", (
        string project,
        string? name = null,
        string? tag = null,
        string? layer = null,
        string? component = null,
        string? scene = null,
        bool includeInactive = false,
        int? limit = null,
        bool json = false) =>
    GameObjectCommand.Find(project, name, tag, layer, component, scene, includeInactive, limit, json));

app.Add("gameobject get", (string project, string id, bool json = false) =>
    GameObjectCommand.Get(project, id, json));

app.Add("gameobject create", (string project, string name, string? parent = null, string? scene = null, bool json = false) =>
    GameObjectCommand.Create(project, name, parent, scene, json));

app.Add("gameobject delete", (string project, string id, bool json = false) =>
    GameObjectCommand.Delete(project, id, json));

app.Add("gameobject set-active", (string project, string id, string active, bool json = false) =>
    GameObjectCommand.SetActive(project, id, active, json));

app.Add("gameobject activate", (string project, string id, bool json = false) =>
    GameObjectCommand.Activate(project, id, json));

app.Add("gameobject deactivate", (string project, string id, bool json = false) =>
    GameObjectCommand.Deactivate(project, id, json));

app.Add("gameobject move", (string project, string id, string parent, bool json = false) =>
    GameObjectCommand.Move(project, id, parent, json));

app.Add("gameobject rename", (string project, string id, string name, bool json = false) =>
    GameObjectCommand.Rename(project, id, name, json));

app.Add("scene save", (string project, string? scene = null, bool all = false, bool json = false) =>
    SceneCommand.Save(project, scene, all, json));

app.Add("scene open", (
        string project,
        string path,
        string mode = "single",
        bool force = false,
        bool saveCurrentModified = false,
        bool json = false) =>
    SceneCommand.Open(project, path, mode, force, saveCurrentModified, json));

app.Add("scene create", (
        string project,
        string path,
        string template = "default",
        string mode = "single",
        bool force = false,
        bool saveCurrentModified = false,
        bool json = false) =>
    SceneCommand.Create(project, path, template, mode, force, saveCurrentModified, json));

app.Add("component add", (string project, string type, string? id = null, string? name = null, bool json = false) =>
    ComponentCommand.Add(project, type, id, name, json));

app.Add("component get", (string project, string componentId, string? property = null, bool full = false, bool json = false) =>
    ComponentCommand.Get(project, componentId, property, full, json));

app.Add("component remove", (string project, string componentId, bool json = false) =>
    ComponentCommand.Remove(project, componentId, json));

app.Add("component set-property", (string project, string componentId, string property, string value, bool json = false) =>
    ComponentCommand.SetProperty(project, componentId, property, value, json));

app.Add("component enable", (string project, string componentId, bool json = false) =>
    ComponentCommand.Enable(project, componentId, json));

app.Add("component disable", (string project, string componentId, bool json = false) =>
    ComponentCommand.Disable(project, componentId, json));

// Phase C-1: Asset CRUD
app.Add("asset create", (string project, string path, string type, bool json = false) =>
    AssetCommand.Create(project, path, type, json));

app.Add("asset create-folder", (string project, string parent, string name, bool json = false) =>
    AssetCommand.CreateFolder(project, parent, name, json));

app.Add("asset copy", (string project, string source, string destination, bool json = false) =>
    AssetCommand.Copy(project, source, destination, json));

app.Add("asset move", (string project, string source, string destination, bool json = false) =>
    AssetCommand.Move(project, source, destination, json));

app.Add("asset delete", (string project, string path, bool json = false) =>
    AssetCommand.Delete(project, path, json));

app.Add("asset import", (string project, string path, string? options = null, bool json = false) =>
    AssetCommand.Import(project, path, options, json));

// Phase C-2: Prefab
app.Add("prefab create", (string project, string target, string path, bool json = false) =>
    PrefabCommand.Create(project, target, path, json));

app.Add("prefab unpack", (string project, string id, string mode = "outermost", bool json = false) =>
    PrefabCommand.Unpack(project, id, mode, json));

app.Add("prefab apply", (string project, string id, bool json = false) =>
    PrefabCommand.Apply(project, id, json));

app.Add("prefab instantiate", (string project, string path, string? name = null, string? parent = null, string? position = null, string? rotation = null, string? scale = null, bool json = false) =>
    PrefabCommand.Instantiate(project, path, name, parent, position, rotation, scale, json));

app.Add("prefab edit", (string project, string path, string property, string value, string? childPath = null, bool json = false) =>
    PrefabCommand.Edit(project, path, property, value, childPath, json));

// Phase C-3: Package Manager
app.Add("package list", (string project, bool json = false) =>
    PackageCommand.List(project, json));

app.Add("package add", (string project, string package_, bool json = false) =>
    PackageCommand.Add(project, package_, json));

app.Add("package remove", (string project, string package_, bool json = false) =>
    PackageCommand.Remove(project, package_, json));

// Phase C-3: Project Settings
app.Add("project-settings get", (string project, string scope, string property, bool json = false) =>
    ProjectSettingsCommand.Get(project, scope, property, json));

app.Add("project-settings set", (string project, string scope, string property, string value, bool json = false) =>
    ProjectSettingsCommand.Set(project, scope, property, value, json));

// Phase C-4: Material/Shader
app.Add("material create", (string project, string path, string shader = "Standard", bool json = false) =>
    MaterialCommand.Create(project, path, shader, json));

app.Add("material get", (string project, string path, string? property = null, bool json = false) =>
    MaterialCommand.Get(project, path, property, json));

app.Add("material set", (string project, string path, string property, string propertyType, string value, bool json = false) =>
    MaterialCommand.Set(project, path, property, propertyType, value, json));

app.Add("material set-shader", (string project, string path, string shader, bool json = false) =>
    MaterialCommand.SetShader(project, path, shader, json));

// Phase C-5: Animation
app.Add("animation create-clip", (string project, string path, bool json = false) =>
    AnimationCommand.CreateClip(project, path, json));

app.Add("animation create-controller", (string project, string path, bool json = false) =>
    AnimationCommand.CreateController(project, path, json));

// Phase C-5: UI
app.Add("ui canvas-create", (string project, string name = "Canvas", string? renderMode = null, bool json = false) =>
    UiCommand.CanvasCreate(project, name, renderMode, json));

app.Add("ui element-create", (string project, string type, string? name = null, string? parent = null, bool json = false) =>
    UiCommand.ElementCreate(project, type, name, parent, json));

app.Add("ui set-rect", (string project, string id, string? anchoredPosition = null, string? sizeDelta = null, string? anchorMin = null, string? anchorMax = null, string? pivot = null, bool json = false) =>
    UiCommand.SetRect(project, id, anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot, json));

app.Add("ui find", (
        string project,
        string? name = null,
        string? text = null,
        string? type = null,
        string? parent = null,
        string? canvas = null,
        string? interactable = null,
        string? active = null,
        bool includeInactive = false,
        int? limit = null,
        bool json = false) =>
    UiCommand.Find(project, name, text, type, parent, canvas, interactable, active, includeInactive, limit, json));

app.Add("ui get", (string project, string id, bool json = false) =>
    UiCommand.Get(project, id, json));

app.Add("ui toggle", (string project, string id, string value, string mode = "auto", bool json = false) =>
    UiCommand.Toggle(project, id, value, mode, json));

app.Add("ui input", (string project, string id, string text, string mode = "auto", bool json = false) =>
    UiCommand.Input(project, id, text, mode, json));

app.Add("undo", (string project, bool json = false) =>
    UndoCommand.Undo(project, json));

app.Add("redo", (string project, bool json = false) =>
    UndoCommand.Redo(project, json));

// Script 확장
app.Add("script list", (string project, string? folder = null, string? filter = null, int? limit = null, bool json = false) =>
    ScriptCommand.List(project, folder, filter, limit, json));

// Script Editing v1
app.Add("script create", (string project, string path, string className, string? ns = null, string baseType = "MonoBehaviour", bool json = false) =>
    ScriptCommand.Create(project, path, className, ns, baseType, json));

app.Add("script edit", (string project, string path, string? content = null, string? contentFile = null, bool json = false) =>
    ScriptCommand.Edit(project, path, content, contentFile, json));

app.Add("script delete", (string project, string path, bool json = false) =>
    ScriptCommand.Delete(project, path, json));

app.Add("script patch", (string project, string path, int startLine, int deleteCount = 0, string? insertContent = null, string? insertContentFile = null, bool json = false) =>
    ScriptCommand.Patch(project, path, startLine, deleteCount, insertContent, insertContentFile, json));

app.Add("script validate", (string project, string? path = null, bool wait = true, int timeout = 300, bool json = false) =>
    ScriptCommand.Validate(project, path, wait, timeout, json));

// Script v2: diagnostics + refactoring
app.Add("script get-errors", (string project, string? path = null, bool json = false) =>
    ScriptCommand.GetErrors(project, path, json));

app.Add("script find-refs", (string project, string symbol, string? folder = null, int? limit = null, bool json = false) =>
    ScriptCommand.FindRefs(project, symbol, folder, limit, json));

app.Add("script rename-symbol", (string project, string oldName, string newName, string? folder = null, bool dryRun = false, bool json = false) =>
    ScriptCommand.RenameSymbol(project, oldName, newName, folder, dryRun, json));

// Tags & Layers
app.Add("tag list", (string project, bool json = false) =>
    TagCommand.List(project, json));

app.Add("tag add", (string project, string name, bool json = false) =>
    TagCommand.Add(project, name, json));

app.Add("layer list", (string project, bool json = false) =>
    LayerCommand.List(project, json));

app.Add("layer set", (string project, int index, string name, bool json = false) =>
    LayerCommand.Set(project, index, name, json));

app.Add("gameobject set-tag", (string project, string id, string tag, bool json = false) =>
    GameObjectCommand.SetTag(project, id, tag, json));

app.Add("gameobject set-layer", (string project, string id, string layer, bool json = false) =>
    GameObjectCommand.SetLayer(project, id, layer, json));

// Editor Utility 확장
app.Add("editor pause", (string project, string action = "toggle", bool json = false) =>
    EditorCommand.Pause(project, action, json));

app.Add("editor focus-gameview", (string project, bool json = false) =>
    EditorCommand.FocusGameView(project, json));

app.Add("editor focus-sceneview", (string project, bool json = false) =>
    EditorCommand.FocusSceneView(project, json));

// Editor Utility
app.Add("console clear", (string project, bool json = false) =>
    ConsoleCommand.Clear(project, json));

app.Add("console get-count", (string project, bool json = false) =>
    ConsoleCommand.GetCount(project, json));

app.Add("console get-entries", (string project, int limit = 50, string? filter = null, bool json = false) =>
    ConsoleCommand.GetEntries(project, limit, filter, json));

app.Add("define-symbols get", (string project, string? target = null, bool json = false) =>
    DefineSymbolsCommand.Get(project, target, json));

app.Add("define-symbols set", (string project, string symbols, string? target = null, bool json = false) =>
    DefineSymbolsCommand.Set(project, symbols, target, json));

// Lighting
app.Add("lighting bake", (string project, int timeout = 3600, bool json = false) =>
    LightingCommand.Bake(project, timeout, json));

app.Add("lighting cancel", (string project, bool json = false) =>
    LightingCommand.Cancel(project, json));

app.Add("lighting clear", (string project, bool json = false) =>
    LightingCommand.Clear(project, json));

app.Add("lighting get-settings", (string project, bool json = false) =>
    LightingCommand.GetSettings(project, json));

app.Add("lighting set-settings", (string project, string property, string value, bool json = false) =>
    LightingCommand.SetSettings(project, property, value, json));

// NavMesh
app.Add("navmesh bake", (string project, bool json = false) =>
    NavMeshCommand.Bake(project, json));

app.Add("navmesh clear", (string project, bool json = false) =>
    NavMeshCommand.Clear(project, json));

app.Add("navmesh get-settings", (string project, bool json = false) =>
    NavMeshCommand.GetSettings(project, json));

// Physics
app.Add("physics get-settings", (string project, bool json = false) =>
    PhysicsCommand.GetSettings(project, json));

app.Add("physics set-settings", (string project, string property, string value, bool json = false) =>
    PhysicsCommand.SetSettings(project, property, value, json));

app.Add("physics get-collision-matrix", (string project, bool json = false) =>
    PhysicsCommand.GetCollisionMatrix(project, json));

app.Add("physics set-collision-matrix", (string project, string layer1, string layer2, string ignore, bool json = false) =>
    PhysicsCommand.SetCollisionMatrix(project, layer1, layer2, ignore, json));

// Camera
app.Add("camera list", (string project, bool includeInactive = false, bool json = false) =>
    CameraCommand.List(project, includeInactive, json));

app.Add("camera get", (string project, string id, bool json = false) =>
    CameraCommand.Get(project, id, json));

// Texture Import
app.Add("texture get-import-settings", (string project, string path, bool json = false) =>
    TextureCommand.GetImportSettings(project, path, json));

app.Add("texture set-import-settings", (string project, string path, string property, string value, bool json = false) =>
    TextureCommand.SetImportSettings(project, path, property, value, json));

// ScriptableObject
app.Add("scriptableobject find", (string project, string? type = null, string? folder = null, int? limit = null, bool json = false) =>
    ScriptableObjectCommand.Find(project, type, folder, limit, json));

app.Add("scriptableobject get", (string project, string path, string? property = null, bool json = false) =>
    ScriptableObjectCommand.Get(project, path, property, json));

app.Add("scriptableobject set-property", (string project, string path, string property, string value, bool json = false) =>
    ScriptableObjectCommand.SetProperty(project, path, property, value, json));

// Shader
app.Add("shader find", (string project, string? filter = null, bool includeBuiltin = false, int? limit = null, bool json = false) =>
    ShaderCommand.Find(project, filter, includeBuiltin, limit, json));

app.Add("shader get-properties", (string project, string name, bool json = false) =>
    ShaderCommand.GetProperties(project, name, json));

// UI Toolkit — Phase I-2
app.Add("uitk find", (string project, string? name = null, string? className = null, string? type = null, int? limit = null, bool json = false) =>
    UitkCommand.Find(project, name, className, type, limit, json));

app.Add("uitk get", (string project, string name, bool json = false) =>
    UitkCommand.Get(project, name, json));

app.Add("uitk set-value", (string project, string name, string value, bool json = false) =>
    UitkCommand.SetValue(project, name, value, json));

// Cinemachine — Phase E
app.Add("cinemachine list", (string project, bool includeInactive = false, bool json = false) =>
    CinemachineCommand.List(project, includeInactive, json));

app.Add("cinemachine get", (string project, string id, bool json = false) =>
    CinemachineCommand.Get(project, id, json));

app.Add("cinemachine set-property", (string project, string id, string property, string value, bool json = false) =>
    CinemachineCommand.SetProperty(project, id, property, value, json));

// Volume/PostProcessing — Phase D
app.Add("volume list", (string project, bool includeInactive = false, bool json = false) =>
    VolumeCommand.List(project, includeInactive, json));

app.Add("volume get", (string project, string id, bool json = false) =>
    VolumeCommand.Get(project, id, json));

app.Add("volume set-override", (string project, string id, string component, string property, string value, bool json = false) =>
    VolumeCommand.SetOverride(project, id, component, property, value, json));

app.Add("volume get-overrides", (string project, string id, string component, bool json = false) =>
    VolumeCommand.GetOverrides(project, id, component, json));

app.Add("renderer-feature list", (string project, bool json = false) =>
    RendererFeatureCommand.List(project, json));

// UGUI Enhancement — Phase I-1
app.Add("ui scroll", (string project, string id, string? x = null, string? y = null, string mode = "auto", bool json = false) =>
    UiCommand.Scroll(project, id, x, y, mode, json));

app.Add("ui slider-set", (string project, string id, string value, string mode = "auto", bool json = false) =>
    UiCommand.SliderSet(project, id, value, mode, json));

app.Add("ui dropdown-set", (string project, string id, string value, string mode = "auto", bool json = false) =>
    UiCommand.DropdownSet(project, id, value, mode, json));

// Profiler — Phase C
app.Add("profiler get-stats", (string project, bool detailed = false, bool json = false) =>
    ProfilerCommand.GetStats(project, detailed, json));

app.Add("profiler start", (string project, bool json = false) =>
    ProfilerCommand.Start(project, json));

app.Add("profiler stop", (string project, bool json = false) =>
    ProfilerCommand.Stop(project, json));

// Animation Workflow Extension — Phase H
app.Add("animation list-clips", (string project, string? folder = null, string? filter = null, int? limit = null, bool json = false) =>
    AnimationCommand.ListClips(project, folder, filter, limit, json));

app.Add("animation get-clip", (string project, string path, bool json = false) =>
    AnimationCommand.GetClip(project, path, json));

app.Add("animation get-controller", (string project, string path, bool json = false) =>
    AnimationCommand.GetController(project, path, json));

app.Add("animation add-curve", (string project, string path, string binding, string keys, bool json = false) =>
    AnimationCommand.AddCurve(project, path, binding, keys, json));

// Asset Import/Export Extension — Phase G
app.Add("asset export", (string project, string paths, string output, bool includeDependencies = true, bool json = false) =>
    AssetCommand.Export(project, paths, output, includeDependencies, json));

app.Add("model get-import-settings", (string project, string path, bool json = false) =>
    ModelCommand.GetImportSettings(project, path, json));

app.Add("audio get-import-settings", (string project, string path, bool json = false) =>
    AudioCommand.GetImportSettings(project, path, json));

// Screenshot / Visual Feedback — P3
app.Add("screenshot capture", (
        string project,
        string view = "scene",
        int width = 1920,
        int height = 1080,
        string format = "png",
        int quality = 75,
        string? output = null,
        bool json = false) =>
    ScreenshotCommand.Capture(project, view, width, height, format, quality, output, json));

app.Run(args);
