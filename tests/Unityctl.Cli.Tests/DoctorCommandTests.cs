using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class DoctorCommandTests
{
    [CliTestFact]
    public void ShouldAutoDiagnose_ProjectLocked_ReturnsTrue()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.ProjectLocked,
            Success = false,
            Message = "locked"
        };

        Assert.True(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_CommandNotFound_ReturnsTrue()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.CommandNotFound,
            Success = false,
            Message = "Unknown command: gameobject-find"
        };

        Assert.True(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_UnknownErrorWithPipeMessage_ReturnsTrue()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.UnknownError,
            Success = false,
            Message = "IPC communication error: Pipe closed before full message was read."
        };

        Assert.True(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_NotFound_ReturnsFalse()
    {
        var response = new CommandResponse
        {
            StatusCode = StatusCode.NotFound,
            Success = false,
            Message = "Asset not found"
        };

        Assert.False(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void ShouldAutoDiagnose_Success_ReturnsFalse()
    {
        var response = CommandResponse.Ok("ok");
        Assert.False(DoctorCommand.ShouldAutoDiagnose(response));
    }

    [CliTestFact]
    public void Diagnose_IncludesBuildStateDirectory()
    {
        var tempProject = Path.Combine(Path.GetTempPath(), "unityctl-doctor-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempProject, "Packages"));
        File.WriteAllText(Path.Combine(tempProject, "Packages", "manifest.json"), "{ }");

        try
        {
            var result = DoctorCommand.Diagnose(tempProject);

            Assert.False(string.IsNullOrWhiteSpace(result.BuildStateDirectory));
            Assert.True(Path.IsPathRooted(result.BuildStateDirectory));
        }
        finally
        {
            try { Directory.Delete(tempProject, recursive: true); } catch { }
        }
    }

    [CliTestFact]
    public void Diagnose_ReportsLocalPluginSource()
    {
        using var tempProject = new TemporaryProject("""
{
  "dependencies": {
    "com.unityctl.bridge": "file:C:/repo/src/Unityctl.Plugin"
  }
}
""");

        var result = DoctorCommand.Diagnose(tempProject.Path);

        Assert.True(result.PluginInstalled);
        Assert.Equal("file:C:/repo/src/Unityctl.Plugin", result.PluginSource);
        Assert.Equal("local-file", result.PluginSourceKind);
        Assert.False(string.IsNullOrWhiteSpace(result.LockFilePath));
    }

    [CliTestFact]
    public void Diagnose_ReportsGitPluginSource()
    {
        using var tempProject = new TemporaryProject("""
{
  "dependencies": {
    "com.unityctl.bridge": "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0"
  }
}
""");

        var result = DoctorCommand.Diagnose(tempProject.Path);

        Assert.True(result.PluginInstalled);
        Assert.Equal("git", result.PluginSourceKind);
        Assert.Contains(".git", result.PluginSource);
    }

    private sealed class TemporaryProject : IDisposable
    {
        public TemporaryProject(string manifestJson)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unityctl-doctor-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Packages"));
            File.WriteAllText(System.IO.Path.Combine(Path, "Packages", "manifest.json"), manifestJson);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { }
        }
    }
}
