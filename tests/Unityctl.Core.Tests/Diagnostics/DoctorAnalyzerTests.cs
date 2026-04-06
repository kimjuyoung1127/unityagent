using Unityctl.Core.Diagnostics;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Core.Tests.Diagnostics;

public sealed class DoctorAnalyzerTests
{
    [Fact]
    public void Analyze_IpcConnectedWithProjectLock_ClassifiesHealthyWithInformationalLock()
    {
        var snapshot = CreateSnapshot(ipcConnected: true, projectLocked: true);

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", [], []);

        Assert.Equal("healthy", analysis.Classification);
        Assert.Equal("informational", analysis.LockSeverity);
        Assert.Contains("healthy", analysis.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_ProjectLockedWithoutIpc_ClassifiesStartingOrReloading()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: true);

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", [], []);

        Assert.Equal("starting-or-reloading", analysis.Classification);
        Assert.Contains("status --project", string.Join(Environment.NewLine, analysis.Recommendations));
    }

    [Fact]
    public void Analyze_HeadlessProcessesWithoutInteractiveEditor_ClassifiesHeadlessBlocking()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: true);
        snapshot.HeadlessProcessCount = 1;
        snapshot.RunningProcessCount = 1;

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", [], []);

        Assert.Equal("headless-process-blocking", analysis.Classification);
        Assert.Contains("headless", string.Join(Environment.NewLine, analysis.Recommendations), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_ScriptGetErrorsBusyFailure_AddsValidateRecommendation()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: true);
        var entries = new[]
        {
            MakeFailure(@"C:\Users\gmdqn\robotapp", 103, "script-get-errors", "Unity Editor is running but IPC is not ready yet.")
        };

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", entries, []);

        Assert.Contains("script validate", string.Join(Environment.NewLine, analysis.Recommendations));
    }

    [Fact]
    public void Analyze_ScriptRenameBusyFailure_AddsIpcPreferredRecommendation()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: false);
        var entries = new[]
        {
            MakeFailure(@"C:\Users\gmdqn\robotapp", 500, "script-rename-symbol", "Unity exited with code 1 but no response file was written.")
        };

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", entries, []);

        Assert.Contains("IPC", string.Join(Environment.NewLine, analysis.Recommendations), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("script rename-symbol", string.Join(Environment.NewLine, analysis.Recommendations), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_UiInputBusyFailure_AddsDeterministicUiRecommendation()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: true);
        var entries = new[]
        {
            MakeFailure(@"C:\Users\gmdqn\robotapp", 103, "ui-input", "Unity Editor is still compiling or reloading.")
        };

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", entries, []);

        Assert.Contains("InputField.text deterministically", string.Join(Environment.NewLine, analysis.Recommendations));
        Assert.Contains("batch fallback is not guaranteed", string.Join(Environment.NewLine, analysis.Recommendations), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_UiClickBusyFailure_AddsButtonRecommendation()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: true);
        var entries = new[]
        {
            MakeFailure(@"C:\Users\gmdqn\robotapp", 103, "ui-click", "Unity Editor is still compiling or reloading.")
        };

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", entries, []);

        Assert.Contains("Button.onClick", string.Join(Environment.NewLine, analysis.Recommendations));
        Assert.Contains("batch fallback is not guaranteed", string.Join(Environment.NewLine, analysis.Recommendations), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_CommandNotFoundFailureWithoutIpc_ClassifiesPluginMismatch()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: false);
        var entries = new[]
        {
            MakeFailure(@"C:\Users\gmdqn\robotapp", 501, "script-patch", "Unknown command: script-patch")
        };

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\robotapp", entries, []);

        Assert.Equal("plugin-mismatch-suspected", analysis.Classification);
    }

    [Fact]
    public void Analyze_BatchFallbackSignature_ClassifiesTransportDegraded()
    {
        var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: false);
        var entries = new[]
        {
            MakeFailure(@"C:\Users\gmdqn\unityagent\tests\Unityctl.Integration\SampleUnityProject", 500, "ping", "Unity exited with code 1 but no response file was written.")
        };

        var analysis = DoctorAnalyzer.Analyze(snapshot, @"C:\Users\gmdqn\unityagent\tests\Unityctl.Integration\SampleUnityProject", entries, []);

        Assert.Equal("transport-degraded", analysis.Classification);
        Assert.True(analysis.HasBatchFallbackSignature);
    }

    [Fact]
    public void Analyze_MixedPathEntriesAndStaleSessions_AreIncludedInSummary()
    {
        var tempProject = Path.Combine(Path.GetTempPath(), "unityctl-doctor-analysis-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempProject);

        try
        {
            var normalized = Unityctl.Shared.Constants.NormalizeProjectPath(tempProject);
            // On Windows, raw uses backslashes; on Unix, raw is the same as tempProject
            var raw = OperatingSystem.IsWindows()
                ? tempProject.Replace('/', '\\')
                : tempProject;
            var snapshot = CreateSnapshot(ipcConnected: false, projectLocked: false);
            var entries = new[]
            {
                MakeSuccess(raw, "status"),
                MakeFailure(normalized, 103, "ping", "Unity Editor is running but IPC is not ready yet.")
            };
            var sessions = new[]
            {
                new Session
                {
                    Id = Guid.NewGuid().ToString("N"),
                    State = SessionState.Running,
                    ProjectPath = normalized,
                    Command = "asset-refresh",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"),
                    UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
                    CliPid = int.MaxValue
                }
            };

            var analysis = DoctorAnalyzer.Analyze(snapshot, raw, entries, sessions);

            Assert.NotNull(analysis.LastSuccess);
            Assert.Single(analysis.RecentFailures);
            Assert.Single(analysis.ActiveSessions);
            Assert.True(analysis.ActiveSessions[0].StaleSuspected);
        }
        finally
        {
            try { Directory.Delete(tempProject, recursive: true); } catch { }
        }
    }

    private static DoctorSnapshot CreateSnapshot(bool ipcConnected, bool projectLocked)
    {
        return new DoctorSnapshot
        {
            EditorFound = true,
            EditorVersion = "6000.0.64f1",
            PluginInstalled = true,
            PluginSource = "file:C:/repo/src/Unityctl.Plugin",
            PluginSourceKind = "local-file",
            IpcConnected = ipcConnected,
            IpcPipePresent = ipcConnected,
            BridgeLoaded = true,
            ProjectLocked = projectLocked,
            IsCompiling = false,
            IsDomainReloading = projectLocked && !ipcConnected,
            LockFilePath = @"C:\project\Temp\UnityLockfile",
            BuildStateDirectory = @"C:\project\Library\Unityctl\build-state",
            UnityctlLogLines = ["[unityctl] IPC connection error: Pipe closed before full message was read."]
        };
    }

    private static FlightEntry MakeFailure(string project, int statusCode, string operation, string error)
    {
        return new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Project = project,
            Operation = operation,
            StatusCode = statusCode,
            DurationMs = 1234,
            ExitCode = 1,
            Error = error,
            Level = "error",
            V = "0.2.0"
        };
    }

    private static FlightEntry MakeSuccess(string project, string operation)
    {
        return new FlightEntry
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Project = project,
            Operation = operation,
            StatusCode = 0,
            DurationMs = 200,
            ExitCode = 0,
            Level = "info",
            V = "0.2.0"
        };
    }
}
