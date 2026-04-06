using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.EditorRouting;
using Unityctl.Core.Platform;

namespace Unityctl.Cli.Commands;

public static class EditorCommands
{
    public static void Current(bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var resolver = new EditorTargetResolver(discovery, new UnityProcessDetector(platform));
        CurrentCore(new EditorSelectionStore(), resolver.Resolve, json);
    }

    public static void Select(string? project = null, int? pid = null, bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var processDetector = new UnityProcessDetector(platform);
        var resolver = new EditorTargetResolver(discovery, processDetector);
        var success = SelectCore(new EditorSelectionStore(), resolver.Resolve, processDetector, project, pid, json);
        if (!success)
            Environment.Exit(1);
    }

    public static void Instances(bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        InstancesCore(discovery.FindRunningEditorInstances(probeIpc: true), json);
    }

    public static void List(bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var editors = discovery.FindEditors();

        if (editors.Count == 0)
        {
            Console.Error.WriteLine("No Unity Editors found.");
            Console.Error.WriteLine("Tip: Install Unity via Unity Hub, or check search paths.");
            Environment.Exit(1);
            return;
        }

        if (json)
        {
            var jsonStr = JsonSerializer.Serialize(editors, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonStr);
        }
        else
        {
            var console = ConsoleOutput.CreateOut();
            console.MarkupLine($"Found [cyan]{editors.Count}[/] Unity Editor(s):");
            console.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Version")
                .AddColumn("Running")
                .AddColumn("Location");

            foreach (var editor in editors)
            {
                table.AddRow(
                    new Markup($"[cyan]{Markup.Escape(editor.Version)}[/]"),
                    new Text(editor.IsRunning ? "yes" : "no"),
                    new Text(editor.Location));
            }

            console.Write(table);
        }
    }

    internal static void CurrentCore(
        EditorSelectionStore store,
        Func<string, EditorTargetInfo> resolveTarget,
        bool json)
    {
        var selection = store.GetCurrent();
        if (selection == null)
        {
            if (json)
            {
                Console.WriteLine(BuildCurrentPayload(null, null).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("No current editor selection.");
                Console.WriteLine("Use `unityctl editor select --project <path>` to pin a Unity project.");
            }

            return;
        }

        var target = resolveTarget(selection.ProjectPath);
        var payload = BuildCurrentPayload(selection, target);

        if (json)
        {
            Console.WriteLine(payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine("Current editor selection:");
        Console.WriteLine($"  Project: {target.ProjectPath}");
        Console.WriteLine($"  Selected: {selection.SelectedAt}");
        if (!string.IsNullOrWhiteSpace(selection.SelectionMode))
            Console.WriteLine($"  Selection mode: {selection.SelectionMode}");
        if (selection.UnityPid.HasValue)
            Console.WriteLine($"  Selected PID: {selection.UnityPid.Value}");
        Console.WriteLine($"  Pipe: {target.PipeName}");
        Console.WriteLine($"  Running: {(target.IsRunning ? "yes" : "no")}");
        if (target.UnityPid.HasValue)
            Console.WriteLine($"  Unity PID: {target.UnityPid.Value}");
        Console.WriteLine(target.EditorVersion != null
            ? $"  Editor: {target.EditorVersion} ({target.EditorLocation})"
            : "  Editor: not resolved from ProjectVersion.txt");
    }

    internal static bool SelectCore(
        EditorSelectionStore store,
        Func<string, EditorTargetInfo> resolveTarget,
        UnityProcessDetector processDetector,
        string? project,
        int? pid,
        bool json)
    {
        if (!TryResolveSelectionArguments(processDetector, project, pid, out var normalizedProjectPath, out var selectedPid, out var selectionMode, out var error))
        {
            if (json)
            {
                var failure = new JsonObject
                {
                    ["selected"] = false,
                    ["error"] = error
                };
                Console.WriteLine(failure.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.Error.WriteLine($"Error: {error}");
            }

            return false;
        }

        if (selectedPid.HasValue)
            store.SaveProcess(normalizedProjectPath, selectedPid.Value);
        else
            store.SaveSelection(normalizedProjectPath, null, selectionMode);
        var selection = store.GetCurrent();
        var target = resolveTarget(normalizedProjectPath);
        var payload = BuildCurrentPayload(selection, target);

        if (json)
        {
            Console.WriteLine(payload.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }

        Console.WriteLine("Selected editor target:");
        Console.WriteLine($"  Project: {target.ProjectPath}");
        if (!string.IsNullOrWhiteSpace(selection?.SelectionMode))
            Console.WriteLine($"  Selection mode: {selection.SelectionMode}");
        if (selection?.UnityPid is int savedPid)
            Console.WriteLine($"  Selected PID: {savedPid}");
        Console.WriteLine($"  Pipe: {target.PipeName}");
        Console.WriteLine($"  Running: {(target.IsRunning ? "yes" : "no")}");
        if (target.UnityPid.HasValue)
            Console.WriteLine($"  Unity PID: {target.UnityPid.Value}");
        Console.WriteLine(target.EditorVersion != null
            ? $"  Editor: {target.EditorVersion} ({target.EditorLocation})"
            : "  Editor: not resolved from ProjectVersion.txt");
        return true;
    }

    internal static bool TryNormalizeUnityProjectPath(
        string project,
        out string normalizedProjectPath,
        out string error)
    {
        normalizedProjectPath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(project))
        {
            error = "project must not be empty";
            return false;
        }

        var fullPath = Path.GetFullPath(project);
        if (!Directory.Exists(fullPath))
        {
            error = $"Unity project directory not found: {fullPath}";
            return false;
        }

        var versionFilePath = Path.Combine(fullPath, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(versionFilePath))
        {
            error = $"ProjectVersion.txt not found under {fullPath}";
            return false;
        }

        normalizedProjectPath = Unityctl.Shared.Constants.NormalizeProjectPath(fullPath);
        return true;
    }

    internal static bool TryResolveSelectionArguments(
        UnityProcessDetector processDetector,
        string? project,
        int? pid,
        out string normalizedProjectPath,
        out int? selectedPid,
        out string selectionMode,
        out string error)
    {
        normalizedProjectPath = string.Empty;
        selectedPid = null;
        selectionMode = string.Empty;
        error = string.Empty;

        if (!string.IsNullOrWhiteSpace(project) && pid.HasValue)
        {
            error = "specify either --project or --pid, not both";
            return false;
        }

        if (pid.HasValue)
        {
            if (!TryNormalizeProjectPathForPid(processDetector, pid.Value, out normalizedProjectPath, out error))
                return false;

            selectedPid = pid.Value;
            selectionMode = "pid";
            return true;
        }

        if (!TryNormalizeUnityProjectPath(project ?? string.Empty, out normalizedProjectPath, out error))
            return false;

        selectionMode = "project";
        return true;
    }

    internal static bool TryNormalizeProjectPathForPid(
        UnityProcessDetector processDetector,
        int pid,
        out string normalizedProjectPath,
        out string error)
    {
        normalizedProjectPath = string.Empty;
        error = string.Empty;

        var process = processDetector.FindProcessById(pid);
        if (process == null)
        {
            error = $"Unity process not found for pid {pid}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(process.ProjectPath))
        {
            error = $"Unity process {pid} does not expose a projectPath";
            return false;
        }

        normalizedProjectPath = Unityctl.Shared.Constants.NormalizeProjectPath(process.ProjectPath);
        var matchingProcesses = processDetector.FindProcessesForProject(normalizedProjectPath);
        if (matchingProcesses.Count > 1)
        {
            error = $"Multiple Unity processes are running for {normalizedProjectPath}. True same-project pid pinning is not supported yet; use --project for now.";
            return false;
        }

        return true;
    }

    internal static JsonObject BuildCurrentPayload(EditorSelection? selection, EditorTargetInfo? target)
    {
        if (selection == null || target == null)
        {
            return new JsonObject
            {
                ["selected"] = false,
                ["message"] = "No current editor selection."
            };
        }

        return new JsonObject
        {
            ["selected"] = true,
            ["selection"] = new JsonObject
            {
                ["projectPath"] = selection.ProjectPath,
                ["unityPid"] = selection.UnityPid,
                ["selectionMode"] = selection.SelectionMode,
                ["selectedAt"] = selection.SelectedAt
            },
            ["target"] = new JsonObject
            {
                ["projectPath"] = target.ProjectPath,
                ["pipeName"] = target.PipeName,
                ["isRunning"] = target.IsRunning,
                ["unityPid"] = target.UnityPid,
                ["editorVersion"] = target.EditorVersion,
                ["editorLocation"] = target.EditorLocation
            }
        };
    }

    internal static void InstancesCore(
        IReadOnlyList<Unityctl.Shared.Models.UnityEditorInstanceInfo> instances,
        bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(instances.ToArray(), Unityctl.Shared.Serialization.UnityctlJsonContext.Default.UnityEditorInstanceInfoArray));
            return;
        }

        if (instances.Count == 0)
        {
            Console.WriteLine("No running Unity Editor instances found.");
            return;
        }

        var console = ConsoleOutput.CreateOut();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("PID")
            .AddColumn("Version")
            .AddColumn("Kind")
            .AddColumn("IPC")
            .AddColumn("Project")
            .AddColumn("Location");

        foreach (var instance in instances)
        {
            table.AddRow(
                new Text(instance.ProcessId.ToString()),
                new Text(instance.Version ?? "unknown"),
                new Text(instance.ProcessKind ?? "unknown"),
                new Text(instance.IpcReady ? "ready" : "down"),
                new Text(instance.ProjectPath ?? "unknown"),
                new Text(instance.EditorLocation ?? "unknown"));
        }

        console.Write(table);
    }
}
