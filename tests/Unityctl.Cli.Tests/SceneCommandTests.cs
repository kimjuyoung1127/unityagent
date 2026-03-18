using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class SceneCommandTests
{
    [CliTestFact]
    public void CreateSnapshotRequest_Default_HasCorrectCommand()
    {
        var request = SceneCommand.CreateSnapshotRequest(null);

        Assert.Equal(WellKnownCommands.SceneSnapshot, request.Command);
    }

    [CliTestFact]
    public void CreateSnapshotRequest_WithScenePath_IncludesParameter()
    {
        var request = SceneCommand.CreateSnapshotRequest("Assets/Scenes/Main.unity");

        Assert.NotNull(request.Parameters);
        Assert.Equal("Assets/Scenes/Main.unity",
            request.Parameters!["scenePath"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateSnapshotRequest_NoScenePath_NoScenePathParameter()
    {
        var request = SceneCommand.CreateSnapshotRequest(null);

        Assert.NotNull(request.Parameters);
        Assert.Null(request.Parameters!["scenePath"]);
    }

    [CliTestFact]
    public void CreateOpenRequest_HasCorrectCommandAndPath()
    {
        var request = SceneCommand.CreateOpenRequest("Assets/Scenes/Main.unity", mode: "additive");

        Assert.Equal(WellKnownCommands.SceneOpen, request.Command);
        Assert.Equal("Assets/Scenes/Main.unity", request.Parameters!["path"]?.GetValue<string>());
        Assert.Equal("additive", request.Parameters["mode"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateOpenRequest_IncludesSafetyFlags_WhenEnabled()
    {
        var request = SceneCommand.CreateOpenRequest(
            "Assets/Scenes/Main.unity",
            force: true,
            saveCurrentModified: true);

        Assert.True(request.Parameters!["force"]?.GetValue<bool>());
        Assert.True(request.Parameters["saveCurrentModified"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void CreateOpenRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => SceneCommand.CreateOpenRequest(""));
    }

    [CliTestFact]
    public void CreateCreateRequest_HasCorrectCommandAndParameters()
    {
        var request = SceneCommand.CreateCreateRequest(
            "Assets/Scenes/NewScene.unity",
            template: "empty",
            mode: "additive");

        Assert.Equal(WellKnownCommands.SceneCreate, request.Command);
        Assert.Equal("Assets/Scenes/NewScene.unity", request.Parameters!["path"]?.GetValue<string>());
        Assert.Equal("empty", request.Parameters["template"]?.GetValue<string>());
        Assert.Equal("additive", request.Parameters["mode"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateCreateRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => SceneCommand.CreateCreateRequest(""));
    }

    [CliTestFact]
    public void CreateDiffRequest_HasCorrectCommand()
    {
        var baseSnap = new JsonObject { ["timestamp"] = "2026-03-18T00:00:00Z" };
        var headSnap = new JsonObject { ["timestamp"] = "2026-03-18T00:01:00Z" };

        var request = SceneCommand.CreateDiffRequest(baseSnap, headSnap, 1e-6);

        Assert.Equal(WellKnownCommands.SceneDiff, request.Command);
    }

    [CliTestFact]
    public void CreateDiffRequest_IncludesEpsilon()
    {
        var baseSnap = new JsonObject { ["timestamp"] = "base" };
        var headSnap = new JsonObject { ["timestamp"] = "head" };

        var request = SceneCommand.CreateDiffRequest(baseSnap, headSnap, 1e-4);

        Assert.NotNull(request.Parameters);
        var epsilon = request.Parameters!["epsilon"]?.GetValue<double>();
        Assert.NotNull(epsilon);
        Assert.True(Math.Abs(epsilon!.Value - 1e-4) < 1e-10);
    }

    [CliTestFact]
    public void CreateDiffRequest_IncludesBaseAndHead()
    {
        var baseSnap = new JsonObject { ["timestamp"] = "2026-03-18T00:00:00Z", ["scenes"] = new JsonArray() };
        var headSnap = new JsonObject { ["timestamp"] = "2026-03-18T00:01:00Z", ["scenes"] = new JsonArray() };

        var request = SceneCommand.CreateDiffRequest(baseSnap, headSnap, 1e-6);

        Assert.NotNull(request.Parameters);
        var baseParam = request.Parameters!["base"] as JsonObject;
        var headParam = request.Parameters["head"] as JsonObject;
        Assert.NotNull(baseParam);
        Assert.NotNull(headParam);
        Assert.Equal("2026-03-18T00:00:00Z", baseParam!["timestamp"]?.GetValue<string>());
        Assert.Equal("2026-03-18T00:01:00Z", headParam!["timestamp"]?.GetValue<string>());
    }

    [CliTestFact]
    public void GetSnapshotDirectory_IsDeterministic()
    {
        const string project = "/home/dev/MyProject";

        var dir1 = SceneCommand.GetSnapshotDirectory(project);
        var dir2 = SceneCommand.GetSnapshotDirectory(project);

        Assert.Equal(dir1, dir2);
    }

    [CliTestFact]
    public void GetSnapshotDirectory_ContainsConfigDir()
    {
        const string project = "/home/dev/MyProject";

        var dir = SceneCommand.GetSnapshotDirectory(project);

        Assert.StartsWith(Constants.GetConfigDirectory(), dir, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("snapshots", dir, StringComparison.OrdinalIgnoreCase);
    }

    [CliTestFact]
    public void AreJsonNodesEqual_SameFloat_ReturnsTrue()
    {
        var a = JsonValue.Create(1.5);
        var b = JsonValue.Create(1.5);

        Assert.True(SceneCommand.AreJsonNodesEqual(a, b, 1e-6));
    }

    [CliTestFact]
    public void AreJsonNodesEqual_FloatWithinEpsilon_ReturnsTrue()
    {
        var a = JsonValue.Create(1.0);
        var b = JsonValue.Create(1.0 + 0.5e-6);

        Assert.True(SceneCommand.AreJsonNodesEqual(a, b, 1e-6));
    }

    [CliTestFact]
    public void AreJsonNodesEqual_FloatBeyondEpsilon_ReturnsFalse()
    {
        var a = JsonValue.Create(1.0);
        var b = JsonValue.Create(1.0 + 2e-6);

        Assert.False(SceneCommand.AreJsonNodesEqual(a, b, 1e-6));
    }

    [CliTestFact]
    public void AreJsonNodesEqual_BothNull_ReturnsTrue()
    {
        Assert.True(SceneCommand.AreJsonNodesEqual(null, null, 1e-6));
    }

    [CliTestFact]
    public void AreJsonNodesEqual_OneNull_ReturnsFalse()
    {
        var a = JsonValue.Create("hello");

        Assert.False(SceneCommand.AreJsonNodesEqual(a, null, 1e-6));
        Assert.False(SceneCommand.AreJsonNodesEqual(null, a, 1e-6));
    }

    [CliTestFact]
    public void AreJsonNodesEqual_SameString_ReturnsTrue()
    {
        var a = JsonValue.Create("Player");
        var b = JsonValue.Create("Player");

        Assert.True(SceneCommand.AreJsonNodesEqual(a, b, 1e-6));
    }

    [CliTestFact]
    public void AreJsonNodesEqual_DifferentStrings_ReturnsFalse()
    {
        var a = JsonValue.Create("Player");
        var b = JsonValue.Create("Enemy");

        Assert.False(SceneCommand.AreJsonNodesEqual(a, b, 1e-6));
    }

    [CliTestFact]
    public void ComputeDiff_NoChanges_ReturnsEmptyScenes()
    {
        var snapshot = BuildTestSnapshot("snap1");

        var result = SceneCommand.ComputeDiff(snapshot, snapshot, "snap1", "snap1");

        Assert.Empty(result.Scenes);
    }

    [CliTestFact]
    public void ComputeDiff_ChangedProperty_DetectsChange()
    {
        var baseSnap = BuildTestSnapshot("snap1", posX: 1.0);
        var headSnap = BuildTestSnapshot("snap2", posX: 5.0);

        var result = SceneCommand.ComputeDiff(baseSnap, headSnap, "snap1", "snap2");

        Assert.Single(result.Scenes);
        var scene = result.Scenes[0];
        Assert.Single(scene.ModifiedObjects);
        var obj = scene.ModifiedObjects[0];
        Assert.Single(obj.ModifiedComponents);
        var comp = obj.ModifiedComponents[0];
        Assert.Single(comp.PropertyChanges);
        Assert.Equal("m_LocalPosition.x", comp.PropertyChanges[0].PropertyPath);
    }

    [CliTestFact]
    public void ComputeDiff_PropertyWithinEpsilon_NoChange()
    {
        var baseSnap = BuildTestSnapshot("snap1", posX: 1.0);
        var headSnap = BuildTestSnapshot("snap2", posX: 1.0 + 0.5e-7);

        var result = SceneCommand.ComputeDiff(baseSnap, headSnap, "snap1", "snap2", epsilon: 1e-6);

        Assert.Empty(result.Scenes);
    }

    [CliTestFact]
    public void ComputeDiff_AddedGameObject_DetectsAddition()
    {
        var baseSnap = BuildTestSnapshot("snap1", posX: 0.0);
        var headSnap = BuildTestSnapshot("snap2", posX: 0.0);
        // Add an extra game object to head
        var extraGo = new GameObjectEntry
        {
            GlobalObjectId = "new-go-id",
            Name = "NewEnemy",
            ScenePath = "NewEnemy",
            Components = []
        };
        headSnap.Scenes[0] = new SceneEntry
        {
            Path = headSnap.Scenes[0].Path,
            Name = headSnap.Scenes[0].Name,
            IsDirty = false,
            GameObjects = [.. headSnap.Scenes[0].GameObjects, extraGo]
        };

        var result = SceneCommand.ComputeDiff(baseSnap, headSnap, "snap1", "snap2");

        Assert.Single(result.Scenes);
        Assert.Single(result.Scenes[0].AddedObjects);
        Assert.Equal("NewEnemy", result.Scenes[0].AddedObjects[0].Name);
    }

    [CliTestFact]
    public void ComputeDiff_RemovedGameObject_DetectsRemoval()
    {
        var baseSnap = BuildTestSnapshot("snap1", posX: 0.0);
        // Head has no game objects
        var headSnap = new SceneSnapshot
        {
            Timestamp = "snap2",
            UnityVersion = "2022.3.20f1",
            ProjectPath = "/project",
            SceneSetup = [],
            Scenes =
            [
                new SceneEntry
                {
                    Path = "Assets/Main.unity",
                    Name = "Main",
                    IsDirty = false,
                    GameObjects = []
                }
            ]
        };

        var result = SceneCommand.ComputeDiff(baseSnap, headSnap, "snap1", "snap2");

        Assert.Single(result.Scenes);
        Assert.Single(result.Scenes[0].RemovedObjects);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static SceneSnapshot BuildTestSnapshot(string timestamp, double posX = 1.5)
    {
        return new SceneSnapshot
        {
            Timestamp = timestamp,
            UnityVersion = "2022.3.20f1",
            ProjectPath = "/project",
            SceneSetup = [],
            Scenes =
            [
                new SceneEntry
                {
                    Path = "Assets/Main.unity",
                    Name = "Main",
                    IsDirty = false,
                    GameObjects =
                    [
                        new GameObjectEntry
                        {
                            GlobalObjectId = "go-id-001",
                            Name = "Player",
                            ActiveSelf = true,
                            Layer = 0,
                            Tag = "Player",
                            ScenePath = "Player",
                            Components =
                            [
                                new ComponentEntry
                                {
                                    GlobalObjectId = "comp-id-001",
                                    TypeName = "UnityEngine.Transform",
                                    Enabled = true,
                                    Properties = new JsonObject
                                    {
                                        ["m_LocalPosition.x"] = posX,
                                        ["m_LocalPosition.y"] = 0.0
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
