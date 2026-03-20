using Unityctl.Shared.Commands;
using Xunit;

namespace Unityctl.Shared.Tests;

public class CommandCatalogTests
{
    [Fact]
    public void All_HasStableCommandNames()
    {
        var names = CommandCatalog.All.Select(command => command.Name).ToArray();

        Assert.Equal(
            ["init", "editor list", "editor instances", "editor current", "editor select", "ping", "status", "build",
             "build-profile-list", "build-profile-get-active", "build-profile-set-active", "build-target-switch",
             "test", "check", "tools", "doctor", "log",
             "session list", "session stop", "session clean", "watch",
             "scene snapshot", "scene-hierarchy", "scene diff",
             "schema", "exec", "workflow", "workflow-verify", "batch-execute",
             "play-mode", "player-settings-get", "player-settings-set", "asset-refresh",
             "asset-find", "asset-get-info", "asset-get-dependencies", "asset-reference-graph",
              "build-settings-get-scenes", "gameobject-find", "gameobject-get", "component-get",
              "gameobject-create", "gameobject-delete", "gameobject-set-active",
              "gameobject-move", "gameobject-rename", "scene-save", "scene-open", "scene-create",
              "component-add", "component-remove", "component-set-property", "undo", "redo",
              // Phase C-1: Asset CRUD
              "asset-create", "asset-create-folder", "asset-copy", "asset-move", "asset-delete", "asset-import",
             // Phase C-2: Prefab
             "prefab-create", "prefab-unpack", "prefab-apply", "prefab-edit",
             // Phase C-3: Package Manager + Project Settings
             "package-list", "package-add", "package-remove", "project-settings-get", "project-settings-set",
             // Phase C-4: Material/Shader
             "material-create", "material-get", "material-set", "material-set-shader",
             // Phase C-5: Animation + UI
              "animation-create-clip", "animation-create-controller",
              "ui-canvas-create", "ui-element-create", "ui-set-rect", "ui-find", "ui-get", "ui-toggle", "ui-input",
             // Script Editing v1
             "script-create", "script-edit", "script-delete", "script-validate",
             "script-patch",
             // P0 잔여분: Asset Labels + Build Settings
             "asset-get-labels", "asset-set-labels", "build-settings-set-scenes",
             // Screenshot / Visual Feedback — P3
             "screenshot",
             // Tags & Layers
             "tag-list", "tag-add", "layer-list", "layer-set",
             "gameobject-set-tag", "gameobject-set-layer",
             // Editor Utility
             "console-clear", "console-get-count",
             "define-symbols-get", "define-symbols-set",
             // Lighting
             "lighting-bake", "lighting-cancel", "lighting-clear",
             "lighting-get-settings", "lighting-set-settings",
             // NavMesh
             "navmesh-bake", "navmesh-clear", "navmesh-get-settings",
             // Editor Utility 확장
             "editor-pause", "editor-focus-gameview", "editor-focus-sceneview",
             // Script 확장
             "script-list",
             // Physics
             "physics-get-settings", "physics-set-settings",
             "physics-get-collision-matrix", "physics-set-collision-matrix",
             // Script v2: diagnostics + refactoring
             "script-get-errors", "script-find-refs", "script-rename-symbol",
             // Mesh Primitives
             "mesh-create-primitive",
             // Project Validation
             "project-validate",
             // Camera
             "camera-list", "camera-get",
             // Texture Import
             "texture-get-import-settings", "texture-set-import-settings",
             // ScriptableObject
             "scriptableobject-find", "scriptableobject-get", "scriptableobject-set-property",
             // Shader
             "shader-find", "shader-get-properties"],
            names);
    }

    [Fact]
    public void All_HasNoDuplicateNames()
    {
        var names = CommandCatalog.All.Select(command => command.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RequiredParameters_AreMarkedCorrectly()
    {
        var init = CommandCatalog.All.Single(command => command.Name == "init");
        var status = CommandCatalog.All.Single(command => command.Name == "status");

        Assert.Contains(init.Parameters, parameter => parameter.Name == "project" && parameter.Required);
        Assert.Contains(status.Parameters, parameter => parameter.Name == "project" && parameter.Required);
        Assert.DoesNotContain(status.Parameters, parameter => parameter.Name == "json" && parameter.Required);
    }

    [Fact]
    public void Build_HasDryRunParameter_AsOptional()
    {
        var build = CommandCatalog.All.Single(command => command.Name == "build");

        Assert.Contains(build.Parameters, p => p.Name == "dryRun");
        Assert.DoesNotContain(build.Parameters, p => p.Name == "dryRun" && p.Required);
    }

    [Fact]
    public void SceneSnapshot_HasProjectParameter_AsRequired()
    {
        var sceneSnapshot = CommandCatalog.All.Single(command => command.Name == "scene snapshot");

        Assert.Contains(sceneSnapshot.Parameters, p => p.Name == "project" && p.Required);
        Assert.Contains(sceneSnapshot.Parameters, p => p.Name == "scenePath" && !p.Required);
        Assert.Contains(sceneSnapshot.Parameters, p => p.Name == "includeInactive" && !p.Required);
    }

    [Fact]
    public void SceneHierarchy_HasScenePathAndIncludeInactive_AsOptional()
    {
        var sceneHierarchy = CommandCatalog.All.Single(command => command.Name == "scene-hierarchy");

        Assert.Contains(sceneHierarchy.Parameters, p => p.Name == "project" && p.Required);
        Assert.Contains(sceneHierarchy.Parameters, p => p.Name == "scenePath" && !p.Required);
        Assert.Contains(sceneHierarchy.Parameters, p => p.Name == "includeInactive" && !p.Required);
    }

    [Fact]
    public void AssetReferenceGraph_HasPathParameter_AsRequired()
    {
        var assetReferenceGraph = CommandCatalog.All.Single(command => command.Name == "asset-reference-graph");

        Assert.Contains(assetReferenceGraph.Parameters, p => p.Name == "project" && p.Required);
        Assert.Contains(assetReferenceGraph.Parameters, p => p.Name == "path" && p.Required);
        Assert.DoesNotContain(assetReferenceGraph.Parameters, p => p.Name == "json" && p.Required);
    }

    [Fact]
    public void SceneDiff_HasEpsilonParameter_AsOptional()
    {
        var sceneDiff = CommandCatalog.All.Single(command => command.Name == "scene diff");

        Assert.Contains(sceneDiff.Parameters, p => p.Name == "epsilon");
        Assert.DoesNotContain(sceneDiff.Parameters, p => p.Name == "epsilon" && p.Required);
    }

    [Fact]
    public void SceneDiff_HasLiveParameter_AsOptional()
    {
        var sceneDiff = CommandCatalog.All.Single(command => command.Name == "scene diff");

        Assert.Contains(sceneDiff.Parameters, p => p.Name == "live");
        Assert.DoesNotContain(sceneDiff.Parameters, p => p.Name == "live" && p.Required);
    }

    [Fact]
    public void BatchExecute_HasCommandsOrFile_WithOptionalRollback()
    {
        var batchExecute = CommandCatalog.All.Single(command => command.Name == "batch-execute");

        Assert.Contains(batchExecute.Parameters, p => p.Name == "project" && p.Required);
        Assert.Contains(batchExecute.Parameters, p => p.Name == "commands" && !p.Required);
        Assert.Contains(batchExecute.Parameters, p => p.Name == "file" && !p.Required);
        Assert.Contains(batchExecute.Parameters, p => p.Name == "rollbackOnFailure" && !p.Required);
    }
}
