using Unityctl.Cli.Commands;
using Unityctl.Core.Discovery;
using Unityctl.Core.EditorRouting;
using Unityctl.Core.Platform;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class EditorCommandTests
{
    [CliTestFact]
    public void CreatePauseRequest_HasCorrectCommand()
    {
        var request = EditorCommand.CreatePauseRequest();
        Assert.Equal(WellKnownCommands.EditorPause, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [CliTestFact]
    public void CreatePauseRequest_DefaultAction_IsToggle()
    {
        var request = EditorCommand.CreatePauseRequest();
        Assert.Equal("toggle", request.Parameters!["action"]!.ToString());
    }

    [CliTestFact]
    public void CreatePauseRequest_SetsActionParameter()
    {
        var request = EditorCommand.CreatePauseRequest("pause");
        Assert.Equal("pause", request.Parameters!["action"]!.ToString());
    }

    [CliTestFact]
    public void CreateFocusGameViewRequest_HasCorrectCommand()
    {
        var request = EditorCommand.CreateFocusGameViewRequest();
        Assert.Equal(WellKnownCommands.EditorFocusGameView, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [CliTestFact]
    public void CreateFocusSceneViewRequest_HasCorrectCommand()
    {
        var request = EditorCommand.CreateFocusSceneViewRequest();
        Assert.Equal(WellKnownCommands.EditorFocusSceneView, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [CliTestFact]
    public void BuildCurrentPayload_WhenNoSelection_ReturnsSelectedFalse()
    {
        var payload = EditorCommands.BuildCurrentPayload(null, null);

        Assert.False(payload["selected"]!.GetValue<bool>());
    }

    [CliTestFact]
    public void TryResolveSelectionArguments_RejectsProjectAndPidTogether()
    {
        var ok = EditorCommands.TryResolveSelectionArguments(
            new UnityProcessDetector(new FakePlatform([])),
            @"C:\Project",
            1234,
            out _,
            out _,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("either --project or --pid", error);
    }

    [CliTestFact]
    public void TryNormalizeProjectPathForPid_ResolvesUniqueProject()
    {
        var processDetector = new UnityProcessDetector(new FakePlatform(
        [
            new Unityctl.Core.Platform.UnityProcessInfo
            {
                ProcessId = 55028,
                ProjectPath = @"C:\Users\ezen601\Desktop\Jason\My project",
                Version = "6000.0.64f1"
            }
        ]));

        var ok = EditorCommands.TryNormalizeProjectPathForPid(processDetector, 55028, out var projectPath, out var error);

        Assert.True(ok);
        Assert.Equal(Constants.NormalizeProjectPath(@"C:\Users\ezen601\Desktop\Jason\My project"), projectPath);
        Assert.Equal(string.Empty, error);
    }

    [CliTestFact]
    public void TryNormalizeProjectPathForPid_RejectsSameProjectDuplicates()
    {
        var processDetector = new UnityProcessDetector(new FakePlatform(
        [
            new Unityctl.Core.Platform.UnityProcessInfo
            {
                ProcessId = 100,
                ProjectPath = @"C:\Users\ezen601\Desktop\Jason\My project"
            },
            new Unityctl.Core.Platform.UnityProcessInfo
            {
                ProcessId = 101,
                ProjectPath = @"C:\Users\ezen601\Desktop\Jason\My project"
            }
        ]));

        var ok = EditorCommands.TryNormalizeProjectPathForPid(processDetector, 100, out _, out var error);

        Assert.False(ok);
        Assert.Contains("not supported yet", error);
    }

    [CliTestFact]
    public void TryNormalizeUnityProjectPath_RequiresProjectVersionFile()
    {
        using var temp = new TemporaryDirectory();

        var ok = EditorCommands.TryNormalizeUnityProjectPath(temp.Path, out _, out var error);

        Assert.False(ok);
        Assert.Contains("ProjectVersion.txt", error);
    }

    [CliTestFact]
    public void SelectCore_PersistsSelection_AndCurrentCoreReadsIt()
    {
        using var temp = new TemporaryDirectory();
        var projectPath = Path.Combine(temp.Path, "MyProject");
        Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));
        File.WriteAllText(Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.64f1");
        var configDir = Path.Combine(temp.Path, "config");
        var store = new EditorSelectionStore(configDir);
        var target = new EditorTargetInfo
        {
            ProjectPath = Constants.NormalizeProjectPath(projectPath),
            PipeName = Constants.GetPipeName(projectPath),
            EditorVersion = "6000.0.64f1",
            EditorLocation = "/Editors/6000.0.64f1",
            IsRunning = true,
            UnityPid = 1234
        };

        var previousOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            EditorCommands.SelectCore(
                store,
                _ => target,
                new UnityProcessDetector(new FakePlatform([])),
                projectPath,
                null,
                json: true);
            var selectOutput = sw.ToString();
            Assert.Contains("\"selected\": true", selectOutput, StringComparison.OrdinalIgnoreCase);

            sw.GetStringBuilder().Clear();
            EditorCommands.CurrentCore(store, _ => target, json: true);
            var currentOutput = sw.ToString();
            Assert.Contains(target.ProjectPath, currentOutput);
            Assert.Contains(target.PipeName, currentOutput);
            Assert.Contains("\"unityPid\": 1234", currentOutput);
        }
        finally
        {
            Console.SetOut(previousOut);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-editor-cmd-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { }
        }
    }

    private sealed class FakePlatform : IPlatformServices
    {
        private readonly IReadOnlyList<Unityctl.Core.Platform.UnityProcessInfo> _processes;

        public FakePlatform(IReadOnlyList<Unityctl.Core.Platform.UnityProcessInfo> processes)
        {
            _processes = processes;
        }

        public string GetUnityHubEditorsJsonPath() => string.Empty;
        public IEnumerable<string> GetDefaultEditorSearchPaths() => [];
        public string GetUnityExecutablePath(string editorBasePath) => editorBasePath;
        public IEnumerable<Unityctl.Core.Platform.UnityProcessInfo> FindRunningUnityProcesses() => _processes;
        public bool IsProjectLocked(string projectPath) => false;
        public Stream CreateIpcClientStream(string projectPath) => throw new NotSupportedException();
        public string GetTempResponseFilePath() => Path.GetTempFileName();
    }
}
