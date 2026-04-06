using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;
using Unityctl.Cli.Execution;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

public static class SceneCommand
{
    public static void Save(string project, string? scene = null, bool all = false, bool json = false)
    {
        var request = CreateSaveRequest(scene, all);
        CommandRunner.Execute(project, request, json);
    }

    public static void Open(
        string project,
        string path,
        string mode = "single",
        string? dirtyPolicy = null,
        bool force = false,
        bool saveCurrentModified = false,
        bool json = false)
    {
        var request = CreateOpenRequest(path, mode, dirtyPolicy, force, saveCurrentModified);
        CommandRunner.Execute(project, request, json);
    }

    public static void Create(
        string project,
        string path,
        string template = "default",
        string mode = "single",
        string? dirtyPolicy = null,
        bool force = false,
        bool saveCurrentModified = false,
        bool json = false)
    {
        var request = CreateCreateRequest(path, template, mode, dirtyPolicy, force, saveCurrentModified);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateSaveRequest(string? scene, bool all)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrEmpty(scene)) parameters["scene"] = scene;
        if (all) parameters["all"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.SceneSave,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateOpenRequest(
        string path,
        string mode = "single",
        string? dirtyPolicy = null,
        bool force = false,
        bool saveCurrentModified = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject
        {
            ["path"] = path
        };

        if (!string.IsNullOrWhiteSpace(mode))
            parameters["mode"] = mode;

        var resolvedDirtyPolicy = ResolveDirtyPolicy(dirtyPolicy, force, saveCurrentModified);
        if (!string.IsNullOrWhiteSpace(resolvedDirtyPolicy))
            parameters["dirtyPolicy"] = resolvedDirtyPolicy;

        if (force)
            parameters["force"] = true;

        if (saveCurrentModified)
            parameters["saveCurrentModified"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.SceneOpen,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateCreateRequest(
        string path,
        string template = "default",
        string mode = "single",
        string? dirtyPolicy = null,
        bool force = false,
        bool saveCurrentModified = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject
        {
            ["path"] = path
        };

        if (!string.IsNullOrWhiteSpace(template))
            parameters["template"] = template;

        if (!string.IsNullOrWhiteSpace(mode))
            parameters["mode"] = mode;

        var resolvedDirtyPolicy = ResolveDirtyPolicy(dirtyPolicy, force, saveCurrentModified);
        if (!string.IsNullOrWhiteSpace(resolvedDirtyPolicy))
            parameters["dirtyPolicy"] = resolvedDirtyPolicy;

        if (force)
            parameters["force"] = true;

        if (saveCurrentModified)
            parameters["saveCurrentModified"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.SceneCreate,
            Parameters = parameters
        };
    }

    internal static string ResolveDirtyPolicy(string? dirtyPolicy, bool force, bool saveCurrentModified)
    {
        if (!string.IsNullOrWhiteSpace(dirtyPolicy))
            return dirtyPolicy.Trim().ToLowerInvariant();
        if (saveCurrentModified)
            return "save";
        if (force)
            return "discard";
        return "fail";
    }

    public static void Snapshot(string project, string? scenePath = null, bool includeInactive = false, bool json = false)
    {
        var exitCode = SnapshotAsync(project, scenePath, includeInactive, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    public static void Hierarchy(string project, string? scenePath = null, bool includeInactive = false,
        int maxDepth = -1, bool summary = false, bool json = false)
    {
        var request = CreateHierarchyRequest(scenePath, includeInactive, maxDepth, summary);
        CommandRunner.Execute(project, request, json);
    }

    public static void Diff(
        string snap1 = "",
        string snap2 = "",
        string? project = null,
        bool live = false,
        double epsilon = 1e-6,
        bool json = false)
    {
        if (live && !string.IsNullOrEmpty(project))
        {
            var exitCode = DiffLiveAsync(project, epsilon, json).GetAwaiter().GetResult();
            Environment.Exit(exitCode);
        }
        else if (!string.IsNullOrEmpty(snap1) && !string.IsNullOrEmpty(snap2))
        {
            DiffFiles(snap1, snap2, epsilon, json);
        }
        else
        {
            Console.Error.WriteLine(
                "Error: Provide --snap1 and --snap2 for file diff, or --live --project for live diff.");
            Environment.Exit(1);
        }
    }

    private static async Task<int> SnapshotAsync(string project, string? scenePath, bool includeInactive, bool json)
    {
        var request = CreateSnapshotRequest(scenePath, includeInactive);
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var response = await executor.ExecuteAsync(project, request);

        if (response.Success && response.Data != null)
        {
            try
            {
                SaveSnapshot(project, response.Data.ToJsonString());
            }
            catch
            {
                // Snapshot saving must not crash the CLI
            }
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    private static async Task<int> DiffLiveAsync(string project, double epsilon, bool json)
    {
        // Get latest snapshot from disk
        var latestPath = GetLatestSnapshotPath(project);

        // Take a new snapshot from Unity
        var request = CreateSnapshotRequest(null);
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var response = await executor.ExecuteAsync(project, request);
        if (!response.Success)
        {
            CommandRunner.PrintResponse(response, json);
            return 1;
        }

        string? headJson = response.Data?.ToJsonString();
        if (headJson == null)
        {
            Console.Error.WriteLine("Error: Snapshot response contained no data.");
            return 1;
        }

        // Save the new snapshot
        try
        {
            SaveSnapshot(project, headJson);
        }
        catch
        {
            // Snapshot saving must not crash the CLI
        }

        if (latestPath == null)
        {
            Console.Error.WriteLine(
                "No previous snapshot found. Run 'scene snapshot' first to establish a baseline.");
            return 1;
        }

        string baseJson;
        try
        {
            baseJson = File.ReadAllText(latestPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading baseline snapshot: {ex.Message}");
            return 1;
        }

        return RunLocalDiff(baseJson, headJson, latestPath, "live", epsilon, json);
    }

    internal static void DiffFiles(string snap1Path, string snap2Path, double epsilon, bool json)
    {
        string baseJson, headJson;
        try
        {
            baseJson = File.ReadAllText(snap1Path);
            headJson = File.ReadAllText(snap2Path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading snapshot file: {ex.Message}");
            Environment.Exit(1);
            return;
        }

        var exitCode = RunLocalDiff(baseJson, headJson, snap1Path, snap2Path, epsilon, json);
        Environment.Exit(exitCode);
    }

    private static int RunLocalDiff(
        string baseJson,
        string headJson,
        string baseLabel,
        string headLabel,
        double epsilon,
        bool json)
    {
        SceneSnapshot? baseSnap, headSnap;
        try
        {
            baseSnap = JsonSerializer.Deserialize(baseJson, UnityctlJsonContext.Default.SceneSnapshot);
            headSnap = JsonSerializer.Deserialize(headJson, UnityctlJsonContext.Default.SceneSnapshot);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing snapshot JSON: {ex.Message}");
            return 1;
        }

        if (baseSnap == null || headSnap == null)
        {
            Console.Error.WriteLine("Error: Could not deserialize one or both snapshot files.");
            return 1;
        }

        var result = ComputeDiff(baseSnap, headSnap, baseLabel, headLabel, epsilon);

        if (json)
        {
            Console.WriteLine(
                JsonSerializer.Serialize(result, UnityctlJsonContext.Default.SceneDiffResult));
        }
        else
        {
            PrintDiffText(result);
        }

        return 0;
    }

    /// <summary>
    /// Computes a property-level diff between two snapshots.
    /// Exposed as internal for unit testing.
    /// </summary>
    internal static SceneDiffResult ComputeDiff(
        SceneSnapshot baseSnap,
        SceneSnapshot headSnap,
        string baseLabel,
        string headLabel,
        double epsilon = 1e-6)
    {
        var result = new SceneDiffResult
        {
            Timestamp = DateTime.UtcNow.ToString("o"),
            BaseSnapshot = baseLabel,
            HeadSnapshot = headLabel,
        };

        // Build scene lookup by path from head
        var headScenes = headSnap.Scenes.ToDictionary(s => s.Path, StringComparer.Ordinal);
        var baseScenes = baseSnap.Scenes.ToDictionary(s => s.Path, StringComparer.Ordinal);

        var scenePaths = baseScenes.Keys.Union(headScenes.Keys).Distinct().ToList();
        var sceneDiffs = new List<SceneDiffEntry>();

        foreach (var scenePath in scenePaths)
        {
            baseScenes.TryGetValue(scenePath, out var baseScene);
            headScenes.TryGetValue(scenePath, out var headScene);

            if (baseScene == null || headScene == null)
                continue; // Only diff scenes present in both snapshots

            var entry = DiffScene(baseScene, headScene, epsilon);
            if (HasAnyChanges(entry))
                sceneDiffs.Add(entry);
        }

        result.Scenes = [.. sceneDiffs];
        return result;
    }

    private static SceneDiffEntry DiffScene(SceneEntry baseScene, SceneEntry headScene, double epsilon)
    {
        var baseObjects = baseScene.GameObjects
            .ToDictionary(o => o.GlobalObjectId, StringComparer.Ordinal);
        var headObjects = headScene.GameObjects
            .ToDictionary(o => o.GlobalObjectId, StringComparer.Ordinal);

        var added = headObjects.Keys.Except(baseObjects.Keys)
            .Select(id => new DiffObjectEntry
            {
                GlobalObjectId = id,
                Name = headObjects[id].Name,
                ScenePath = headObjects[id].ScenePath
            }).ToArray();

        var removed = baseObjects.Keys.Except(headObjects.Keys)
            .Select(id => new DiffObjectEntry
            {
                GlobalObjectId = id,
                Name = baseObjects[id].Name,
                ScenePath = baseObjects[id].ScenePath
            }).ToArray();

        var modified = new List<ModifiedObjectEntry>();
        foreach (var id in baseObjects.Keys.Intersect(headObjects.Keys))
        {
            var objDiff = DiffGameObject(baseObjects[id], headObjects[id], epsilon);
            if (objDiff != null)
                modified.Add(objDiff);
        }

        return new SceneDiffEntry
        {
            ScenePath = baseScene.Path,
            AddedObjects = added,
            RemovedObjects = removed,
            ModifiedObjects = [.. modified]
        };
    }

    private static ModifiedObjectEntry? DiffGameObject(
        GameObjectEntry baseObj,
        GameObjectEntry headObj,
        double epsilon)
    {
        var baseComps = baseObj.Components
            .ToDictionary(c => c.TypeName, StringComparer.Ordinal);
        var headComps = headObj.Components
            .ToDictionary(c => c.TypeName, StringComparer.Ordinal);

        var addedComps = headComps.Keys.Except(baseComps.Keys)
            .Select(t => new ModifiedComponentEntry
            {
                TypeName = t,
                GlobalObjectId = headComps[t].GlobalObjectId
            }).ToArray();

        var removedComps = baseComps.Keys.Except(headComps.Keys)
            .Select(t => new ModifiedComponentEntry
            {
                TypeName = t,
                GlobalObjectId = baseComps[t].GlobalObjectId
            }).ToArray();

        var modifiedComps = new List<ComponentDiffEntry>();
        foreach (var typeName in baseComps.Keys.Intersect(headComps.Keys))
        {
            var propDiff = DiffComponent(baseComps[typeName], headComps[typeName], epsilon);
            if (propDiff != null)
                modifiedComps.Add(propDiff);
        }

        if (addedComps.Length == 0 && removedComps.Length == 0 && modifiedComps.Count == 0)
            return null;

        return new ModifiedObjectEntry
        {
            GlobalObjectId = baseObj.GlobalObjectId,
            Name = baseObj.Name,
            AddedComponents = addedComps,
            RemovedComponents = removedComps,
            ModifiedComponents = [.. modifiedComps]
        };
    }

    private static ComponentDiffEntry? DiffComponent(
        ComponentEntry baseComp,
        ComponentEntry headComp,
        double epsilon)
    {
        if (baseComp.Properties == null && headComp.Properties == null)
            return null;

        var changes = new List<PropertyChange>();

        var baseProps = baseComp.Properties ?? new JsonObject();
        var headProps = headComp.Properties ?? new JsonObject();

        // Check all keys in base
        foreach (var kvp in baseProps)
        {
            var path = kvp.Key;
            var baseVal = kvp.Value;
            headProps.TryGetPropertyValue(path, out var headVal);

            if (!AreJsonNodesEqual(baseVal, headVal, epsilon))
            {
                changes.Add(new PropertyChange
                {
                    PropertyPath = path,
                    OldValue = NodeToString(baseVal),
                    NewValue = NodeToString(headVal),
                    ValueType = GetValueType(baseVal)
                });
            }
        }

        // Check keys only in head (newly added properties)
        foreach (var kvp in headProps)
        {
            if (!baseProps.ContainsKey(kvp.Key))
            {
                changes.Add(new PropertyChange
                {
                    PropertyPath = kvp.Key,
                    OldValue = string.Empty,
                    NewValue = NodeToString(kvp.Value),
                    ValueType = GetValueType(kvp.Value)
                });
            }
        }

        if (changes.Count == 0)
            return null;

        return new ComponentDiffEntry
        {
            TypeName = baseComp.TypeName,
            PropertyChanges = [.. changes]
        };
    }

    /// <summary>Compares two JSON nodes with float epsilon tolerance. Exposed for testing.</summary>
    internal static bool AreJsonNodesEqual(JsonNode? a, JsonNode? b, double epsilon)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;

        // Numeric comparison with epsilon
        if (a is JsonValue aVal && b is JsonValue bVal)
        {
            if (aVal.TryGetValue<double>(out var da) && bVal.TryGetValue<double>(out var db))
                return Math.Abs(da - db) <= epsilon;
        }

        return a.ToJsonString() == b.ToJsonString();
    }

    private static string NodeToString(JsonNode? node)
    {
        if (node is null) return string.Empty;
        if (node is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s)) return s;
            return val.ToJsonString();
        }
        return node.ToJsonString();
    }

    private static string GetValueType(JsonNode? node)
    {
        if (node is null) return "null";
        if (node is JsonValue val)
        {
            if (val.TryGetValue<bool>(out _)) return "bool";
            if (val.TryGetValue<int>(out _)) return "int";
            if (val.TryGetValue<double>(out _)) return "float";
            if (val.TryGetValue<string>(out _)) return "string";
        }
        if (node is JsonObject) return "object";
        return "unknown";
    }

    private static bool HasAnyChanges(SceneDiffEntry entry)
    {
        return entry.AddedObjects.Length > 0
            || entry.RemovedObjects.Length > 0
            || entry.ModifiedObjects.Length > 0;
    }

    private static void PrintDiffText(SceneDiffResult result)
    {
        if (result.Scenes.Length == 0)
        {
            Console.WriteLine("No changes detected.");
            return;
        }

        var console = ConsoleOutput.CreateOut();

        foreach (var scene in result.Scenes)
        {
            var tree = new Tree(new Markup($"[bold]Scene:[/] {Markup.Escape(scene.ScenePath)}"));

            foreach (var obj in scene.AddedObjects)
                tree.AddNode(new Markup($"[green]+ {Markup.Escape(obj.Name)}[/] [dim]({Markup.Escape(obj.ScenePath)}) ADDED[/]"));

            foreach (var obj in scene.RemovedObjects)
                tree.AddNode(new Markup($"[red]- {Markup.Escape(obj.Name)}[/] [dim]({Markup.Escape(obj.ScenePath)}) REMOVED[/]"));

            foreach (var obj in scene.ModifiedObjects)
            {
                var objNode = tree.AddNode(new Markup($"[yellow]~ {Markup.Escape(obj.Name)}[/]"));

                foreach (var comp in obj.AddedComponents)
                    objNode.AddNode(new Markup($"[green]+ component: {Markup.Escape(comp.TypeName)}[/]"));

                foreach (var comp in obj.RemovedComponents)
                    objNode.AddNode(new Markup($"[red]- component: {Markup.Escape(comp.TypeName)}[/]"));

                foreach (var comp in obj.ModifiedComponents)
                {
                    var compNode = objNode.AddNode(new Markup($"[yellow]~ {Markup.Escape(comp.TypeName)}[/]"));
                    foreach (var change in comp.PropertyChanges)
                        compNode.AddNode(new Markup(
                            $"[dim]{Markup.Escape(change.PropertyPath)}:[/] {Markup.Escape(change.OldValue)} [dim]\u2192[/] {Markup.Escape(change.NewValue)}"));
                }
            }

            console.Write(tree);
        }
    }

    /// <summary>Creates a scene-snapshot CommandRequest. Exposed for testing.</summary>
    internal static CommandRequest CreateSnapshotRequest(string? scenePath, bool includeInactive = false)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrEmpty(scenePath))
            parameters["scenePath"] = scenePath;
        if (includeInactive)
            parameters["includeInactive"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.SceneSnapshot,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateHierarchyRequest(string? scenePath, bool includeInactive = false,
        int maxDepth = -1, bool summary = false)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrEmpty(scenePath))
            parameters["scenePath"] = scenePath;
        if (includeInactive)
            parameters["includeInactive"] = true;
        if (maxDepth >= 0)
            parameters["maxDepth"] = maxDepth;
        if (summary)
            parameters["summary"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.SceneHierarchy,
            Parameters = parameters
        };
    }

    /// <summary>Creates a scene-diff CommandRequest with two snapshot payloads. Exposed for testing.</summary>
    internal static CommandRequest CreateDiffRequest(JsonObject baseSnap, JsonObject headSnap, double epsilon)
    {
        var parameters = new JsonObject
        {
            ["base"] = baseSnap,
            ["head"] = headSnap,
            ["epsilon"] = epsilon
        };

        return new CommandRequest
        {
            Command = WellKnownCommands.SceneDiff,
            Parameters = parameters
        };
    }

    /// <summary>Returns the snapshot storage directory for the given project. Exposed for testing.</summary>
    internal static string GetSnapshotDirectory(string project)
    {
        var pipeHash = Constants.GetPipeName(project)[Constants.PipePrefix.Length..];
        return Path.Combine(Constants.GetConfigDirectory(), "snapshots", pipeHash);
    }

    private static void SaveSnapshot(string project, string snapshotJson)
    {
        var dir = GetSnapshotDirectory(project);
        Directory.CreateDirectory(dir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var filePath = Path.Combine(dir, $"snap-{timestamp}.json");
        File.WriteAllText(filePath, snapshotJson);
    }

    private static string? GetLatestSnapshotPath(string project)
    {
        var dir = GetSnapshotDirectory(project);
        if (!Directory.Exists(dir)) return null;

        return Directory.GetFiles(dir, "snap-*.json")
            .OrderByDescending(f => f, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
