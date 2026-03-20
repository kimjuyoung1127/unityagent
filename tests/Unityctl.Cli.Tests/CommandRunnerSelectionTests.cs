using Unityctl.Cli.Execution;
using Unityctl.Core.EditorRouting;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class CommandRunnerSelectionTests : IDisposable
{
    private readonly string _configDir;
    private readonly EditorSelectionStore _store;

    public CommandRunnerSelectionTests()
    {
        _configDir = Path.Combine(Path.GetTempPath(), $"unityctl-cli-selection-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_configDir);
        _store = new EditorSelectionStore(_configDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_configDir, recursive: true); } catch { }
    }

    [CliTestFact]
    public void TryResolveProject_WithExplicitProject_ReturnsFullPath()
    {
        var projectPath = Path.Combine(_configDir, "ProjectA");
        Directory.CreateDirectory(projectPath);

        var ok = CommandRunner.TryResolveProject(projectPath, out var resolvedProject, out var failureResponse, _store);

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(projectPath), resolvedProject);
        Assert.Null(failureResponse);
    }

    [CliTestFact]
    public void TryResolveProject_WithoutSelection_ReturnsFailure()
    {
        var ok = CommandRunner.TryResolveProject(null, out var resolvedProject, out var failureResponse, _store);

        Assert.False(ok);
        Assert.Equal(string.Empty, resolvedProject);
        Assert.NotNull(failureResponse);
        Assert.Equal(StatusCode.InvalidParameters, failureResponse!.StatusCode);
    }

    [CliTestFact]
    public void TryResolveProject_UsesSelectedProject()
    {
        var projectPath = Path.Combine(_configDir, "SelectedProject");
        Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));
        File.WriteAllText(Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.64f1");

        _store.SaveProject(projectPath);

        var ok = CommandRunner.TryResolveProject(null, out var resolvedProject, out var failureResponse, _store);

        Assert.True(ok);
        Assert.Equal(Unityctl.Shared.Constants.NormalizeProjectPath(projectPath), resolvedProject);
        Assert.Null(failureResponse);
    }
}
