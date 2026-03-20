using Unityctl.Core.EditorRouting;
using Xunit;

namespace Unityctl.Core.Tests.EditorRouting;

public sealed class EditorSelectionStoreTests : IDisposable
{
    private readonly string _tempDir;

    public EditorSelectionStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-editor-selection-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void GetCurrent_WhenMissing_ReturnsNull()
    {
        var store = new EditorSelectionStore(_tempDir);

        var selection = store.GetCurrent();

        Assert.Null(selection);
    }

    [Fact]
    public void SaveProject_RoundTripsNormalizedProjectPath()
    {
        var store = new EditorSelectionStore(_tempDir);
        var projectPath = Path.Combine(_tempDir, "MyProject");
        Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));
        File.WriteAllText(Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.64f1");

        store.SaveProject(projectPath);
        var selection = store.GetCurrent();

        Assert.NotNull(selection);
        Assert.Equal(Unityctl.Shared.Constants.NormalizeProjectPath(projectPath), selection!.ProjectPath);
        Assert.False(string.IsNullOrWhiteSpace(selection.SelectedAt));
    }
}
