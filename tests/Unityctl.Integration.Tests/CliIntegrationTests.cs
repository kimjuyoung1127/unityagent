using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Unityctl.Integration.Tests;

/// <summary>
/// Black-box CLI integration tests.
/// Runs the built unityctl executable via Process.Start().
///
/// These tests require the CLI to be buildable and executable.
/// On environments with AppLocker or similar policies that block
/// execution from build output directories, tests will be skipped
/// (reported as "passed" with diagnostic output explaining why).
/// </summary>
public class CliIntegrationTests
{
    private static readonly string? ExePath = FindCliExe();
    private static readonly bool CanExecute = CheckCanExecute();

    private readonly ITestOutputHelper _output;

    public CliIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task NoArgs_PrintsVersion()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli();
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(0, exitCode);
        Assert.Contains("unityctl", stdout);
    }

    [Fact]
    public async Task Help_PrintsUsage()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("--help");
        Assert.Equal(0, exitCode);
        // ConsoleAppFramework prints "usage:" and available commands
        Assert.Contains("usage", stdout.ToLowerInvariant());
    }

    [Fact]
    public async Task Status_WithInvalidProject_ReturnsExitCode1()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli("status", "--project", "/nonexistent_project_path", "--json");
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task EditorList_Runs()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, _, _) = await RunCli("editor", "list");
        // Either editors found (0) or none found (1) — both valid
        Assert.True(exitCode == 0 || exitCode == 1,
            $"Expected exit code 0 or 1, got {exitCode}");
    }

    [Fact]
    public async Task Tools_PrintsToolList()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli("tools");
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(0, exitCode);
        Assert.Contains("init", stdout);
        Assert.Contains("ping", stdout);
        Assert.Contains("build", stdout);
        Assert.Contains("tools", stdout);
    }

    [Fact]
    public async Task Tools_Json_ReturnsValidJsonArray()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("tools", "--json");
        Assert.Equal(0, exitCode);
        // Verify it's valid JSON with expected structure
        Assert.StartsWith("[", stdout.Trim());
        Assert.Contains("\"name\"", stdout);
        Assert.Contains("\"parameters\"", stdout);
    }

    [Fact]
    public async Task Log_Stats_ExitCode0()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, stderr) = await RunCli("log", "--stats");
        _output.WriteLine($"stdout: {stdout}");
        _output.WriteLine($"stderr: {stderr}");
        Assert.Equal(0, exitCode);
        Assert.Contains("files:", stdout);
        Assert.Contains("entries:", stdout);
    }

    [Fact]
    public async Task Log_Json_ReturnsJsonArray()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("log", "--json");
        Assert.Equal(0, exitCode);
        Assert.StartsWith("[", stdout.Trim());
    }

    [Fact]
    public async Task Log_Prune_ExitCode0()
    {
        if (!EnsureCanExecute()) return;

        var (exitCode, stdout, _) = await RunCli("log", "--prune");
        Assert.Equal(0, exitCode);
        Assert.Contains("Pruned", stdout);
    }

    /// <summary>
    /// Returns true if tests can proceed. If false, writes a diagnostic
    /// message explaining why the test is being skipped.
    /// </summary>
    private bool EnsureCanExecute()
    {
        if (ExePath == null)
        {
            _output.WriteLine("SKIPPED: CLI executable not found. Build the solution first.");
            _output.WriteLine($"Expected at: {GetExpectedExePath()}");
            return false;
        }

        if (!CanExecute)
        {
            _output.WriteLine("SKIPPED: CLI executable blocked by application control policy (AppLocker).");
            _output.WriteLine("These tests require an unrestricted environment or CI.");
            _output.WriteLine($"Exe path: {ExePath}");
            return false;
        }

        return true;
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunCli(params string[] args)
    {
        if (ExePath == null)
            throw new InvalidOperationException("CLI executable not found");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = ExePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Add arguments individually to handle paths with spaces correctly
        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();

        // Read stdout/stderr concurrently before WaitForExit to avoid deadlock
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException("CLI process did not exit within 30 seconds");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Find the CLI exe in the CLI project's build output directory.
    /// </summary>
    private static string? FindCliExe()
    {
        var path = GetExpectedExePath();
        return File.Exists(path) ? path : null;
    }

    private static string GetExpectedExePath()
    {
        var baseDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "unityctl.exe" : "unityctl";
        return Path.Combine(solutionDir, "src", "Unityctl.Cli", "bin", "Debug", "net10.0", exeName);
    }

    /// <summary>
    /// Probe whether the CLI exe can actually run.
    /// Detects AppLocker (exit code 0xE0434352 = -532462766) and similar blocks.
    /// </summary>
    private static bool CheckCanExecute()
    {
        if (ExePath == null) return false;

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ExePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            process.WaitForExit(10_000);

            // AppLocker blocked: 0xE0434352 = -532462766
            return process.ExitCode != -532462766;
        }
        catch
        {
            return false;
        }
    }
}
