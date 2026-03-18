using ConsoleAppFramework;
using Unityctl.Cli.Commands;

var app = ConsoleApp.Create();

app.Add("", () => Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — A deterministic control plane for Unity\nUse --help for available commands."));

app.Add("init", (string project, string? source = null) =>
    InitCommand.Execute(project, source));

app.Add("editor list", (bool json = false) =>
    EditorCommands.List(json));

app.Add("ping", (string project, bool json = false) =>
    PingCommand.Execute(project, json));

app.Add("status", (string project, bool wait = false, bool json = false) =>
    StatusCommand.Execute(project, wait, json));

app.Add("build", (string project, string target = "StandaloneWindows64", string? output = null, bool dryRun = false, bool json = false) =>
    BuildCommand.Execute(project, target, output, dryRun, json));

app.Add("test", (string project, string mode = "edit", string? filter = null, bool noWait = false, int timeout = 300, bool json = false) =>
    TestCommand.Execute(project, mode, filter, !noWait, timeout, json));

app.Add("check", (string project, string type = "compile", bool json = false) =>
    CheckCommand.Execute(project, type, json));

app.Add("tools", (bool json = false) =>
    ToolsCommand.Execute(json));

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

app.Add("scene snapshot", (string project, string? scenePath = null, bool json = false) =>
    SceneCommand.Snapshot(project, scenePath, json));

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

app.Add("component add", (string project, string id, string type, bool json = false) =>
    ComponentCommand.Add(project, id, type, json));

app.Add("component remove", (string project, string componentId, bool json = false) =>
    ComponentCommand.Remove(project, componentId, json));

app.Add("component set-property", (string project, string componentId, string property, string value, bool json = false) =>
    ComponentCommand.SetProperty(project, componentId, property, value, json));

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

app.Add("undo", (string project, bool json = false) =>
    UndoCommand.Undo(project, json));

app.Add("redo", (string project, bool json = false) =>
    UndoCommand.Redo(project, json));

app.Run(args);
