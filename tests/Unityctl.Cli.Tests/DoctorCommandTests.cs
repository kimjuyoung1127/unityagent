using Unityctl.Cli.Commands;
using Unityctl.Core.Diagnostics;
using Unityctl.Core.EditorRouting;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

[Collection("ConsoleOutput")]
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

    [CliTestFact]
    public void RenderText_HealthyLock_RendersInformationalLockAndRecommendations()
    {
        var snapshot = CreateSnapshot();
        var analysis = new DoctorAnalysis
        {
            Classification = "healthy",
            Summary = "Editor IPC is healthy right now.",
            LockSeverity = "informational",
            LastSuccess = new DoctorActivity
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = "status",
                StatusCode = 0,
                DurationMs = 250,
                Success = true
            },
            RecentFailures =
            [
                new DoctorActivity
                {
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds(),
                    Operation = "ping",
                    StatusCode = 103,
                    DurationMs = 19000,
                    Error = "Unity Editor is running but IPC is not ready yet.",
                    Success = false
                }
            ],
            Recommendations =
            [
                "The latest diagnostics suggest the Editor has recovered; retry the original command first."
            ]
        };

        var text = DoctorCommand.RenderText(@"C:\Users\gmdqn\robotapp", snapshot, analysis);

        Assert.Contains("Classification: healthy", text);
        Assert.Contains("Project lock detected but informational", text);
        Assert.Contains("Recent activity:", text);
        Assert.Contains("Recommended next steps:", text);
    }

    [CliTestFact]
    public void BuildJson_IncludesSummaryRecentActivitySessionsAndRecommendations()
    {
        var snapshot = CreateSnapshot();
        var selection = new EditorSelection
        {
            ProjectPath = @"c:/users/gmdqn/robotapp",
            SelectedAt = DateTimeOffset.UtcNow.ToString("O")
        };
        var analysis = new DoctorAnalysis
        {
            Classification = "transport-degraded",
            Summary = "Recent failures suggest batch fallback is unreliable.",
            LockSeverity = "warning",
            HasBatchFallbackSignature = true,
            HasRecentPipeErrors = true,
            LastSuccess = new DoctorActivity
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Operation = "status",
                StatusCode = 0,
                DurationMs = 999,
                Success = true
            },
            RecentFailures =
            [
                new DoctorActivity
                {
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2).ToUnixTimeMilliseconds(),
                    Operation = "ping",
                    StatusCode = 500,
                    DurationMs = 12000,
                    Error = "Unity exited with code 1 but no response file was written.",
                    Success = false
                }
            ],
            ActiveSessions =
            [
                new DoctorSessionSummary
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Command = "asset-refresh",
                    State = "Running",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-3).ToString("O"),
                    StaleSuspected = true
                }
            ],
            Recommendations =
            [
                "Prefer a running Unity Editor with IPC ready before retrying commands on this project."
            ]
        };

        var json = DoctorCommand.BuildJson(@"C:\Users\gmdqn\robotapp", snapshot, analysis, selection);

        Assert.Equal("transport-degraded", json["summary"]?["classification"]?.GetValue<string>());
        Assert.True(json["recentActivity"]?["batchFallbackSignature"]?.GetValue<bool>());
        Assert.True(json["recentActivity"]?["pipeErrorsDetected"]?.GetValue<bool>());
        Assert.Single(json["activeSessions"]!.AsArray());
        Assert.Single(json["recommendations"]!.AsArray());
        Assert.True(json["selection"]?["matchesRequestedProject"]?.GetValue<bool>());
    }

    [CliTestFact]
    public void RenderAutoDiagnosis_IncludesClassificationAndNextStep()
    {
        var snapshot = CreateSnapshot();
        var analysis = new DoctorAnalysis
        {
            Classification = "starting-or-reloading",
            Summary = "Unity appears to be compiling or reloading.",
            LockSeverity = "warning",
            Recommendations =
            [
                "Run `unityctl status --project \"C:/project\" --wait` and retry when it reports Ready."
            ]
        };

        var text = DoctorCommand.RenderAutoDiagnosis(snapshot, analysis);

        Assert.Contains("Classification: starting-or-reloading", text);
        Assert.Contains("Next step:", text);
    }

    private static DoctorSnapshot CreateSnapshot()
    {
        return new DoctorSnapshot
        {
            EditorFound = true,
            EditorVersion = "6000.0.64f1",
            PluginInstalled = true,
            PluginSource = "file:C:/Users/gmdqn/unityagent/src/Unityctl.Plugin",
            PluginSourceKind = "local-file",
            IpcConnected = true,
            PipeName = "unityctl_deadbeefdeadbeef",
            ProjectLocked = true,
            LockFilePath = @"C:\Users\gmdqn\robotapp\Temp\UnityLockfile",
            BuildStateDirectory = @"C:\Users\gmdqn\robotapp\Library\Unityctl\build-state",
            EditorLogErrors = [],
            UnityctlLogLines = ["[unityctl] IPC connection error: Pipe closed before full message was read."]
        };
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
