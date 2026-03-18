using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit.Sdk;

namespace Unityctl.Integration.Tests.Support;

internal static class TestEnvironment
{
    private static readonly Lazy<string> RepoRootLazy = new(ResolveRepoRoot);
    private static readonly Lazy<string> CliExePathLazy = new(ResolveCliExePath);
    private static readonly Lazy<string> SampleProjectRootLazy = new(ResolveSampleProjectRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    public static string CliExePath => CliExePathLazy.Value;

    public static string SampleUnityProjectRoot => SampleProjectRootLazy.Value;

    public static void EnsureCliCanRun()
    {
        if (!File.Exists(CliExePath))
            throw SkipException.ForSkip($"SKIPPED: CLI executable not found at {CliExePath}");

        try
        {
            using var process = StartCliProcess("--help");
            if (!process.WaitForExit(15_000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                throw SkipException.ForSkip("SKIPPED: CLI executable did not respond to --help within 15 seconds");
            }

            if (process.ExitCode == -532462766)
                throw SkipException.ForSkip("SKIPPED: CLI executable blocked by application control policy.");
        }
        catch (SkipException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw SkipException.ForSkip($"SKIPPED: CLI executable cannot be executed: {ex.Message}");
        }
    }

    public static void EnsureSampleProjectReady()
    {
        var requiredFiles = new[]
        {
            Path.Combine(SampleUnityProjectRoot, "Packages", "manifest.json"),
            Path.Combine(SampleUnityProjectRoot, "ProjectSettings", "ProjectVersion.txt"),
            Path.Combine(SampleUnityProjectRoot, "ProjectSettings", "EditorBuildSettings.asset"),
            Path.Combine(SampleUnityProjectRoot, "Assets", "Scenes", "SampleScene.unity"),
            Path.Combine(SampleUnityProjectRoot, "Assets", "Scenes", "SampleScene.unity.meta")
        };

        var missing = requiredFiles.Where(path => !File.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw SkipException.ForSkip(
                $"SKIPPED: sample Unity project is incomplete. Missing: {string.Join(", ", missing)}");
        }
    }

    public static Process StartCliProcess(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = CliExePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = RepoRoot
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        return Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CLI process.");
    }

    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunCliAsync(
        TimeSpan timeout,
        params string[] args)
    {
        using var process = StartCliProcess(args);
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"CLI process did not exit within {timeout.TotalSeconds:n0} seconds.");
        }

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static string ResolveRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
    }

    private static string ResolveCliExePath()
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "unityctl.exe" : "unityctl";
        var candidates = new[]
        {
            Path.Combine(RepoRoot, "src", "Unityctl.Cli", "bin", "Release", "net10.0", exeName),
            Path.Combine(RepoRoot, "tests", "Unityctl.Integration.Tests", "bin", "Release", "net10.0", exeName),
            Path.Combine(RepoRoot, "src", "Unityctl.Cli", "bin", "Debug", "net10.0", exeName),
            Path.Combine(RepoRoot, "tests", "Unityctl.Integration.Tests", "bin", "Debug", "net10.0", exeName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate) && HasRuntimeConfig(candidate))
                return candidate;
        }

        return candidates[0];
    }

    private static bool HasRuntimeConfig(string exePath)
    {
        var directory = Path.GetDirectoryName(exePath);
        var exeName = Path.GetFileNameWithoutExtension(exePath);
        if (directory == null || string.IsNullOrWhiteSpace(exeName))
            return false;

        return File.Exists(Path.Combine(directory, $"{exeName}.runtimeconfig.json"));
    }

    private static string ResolveSampleProjectRoot()
    {
        var overridePath = Environment.GetEnvironmentVariable("UNITYCTL_HEADLESS_PROJECT");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return Path.GetFullPath(overridePath);

        return Path.Combine(RepoRoot, "tests", "Unityctl.Integration", "SampleUnityProject");
    }
}
