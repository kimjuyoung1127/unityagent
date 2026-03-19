using Unityctl.Core.Setup;
using Xunit;

namespace Unityctl.Core.Tests;

public class PluginSourceLocatorTests
{
    [Fact]
    public void TryResolvePackageSource_UsesWorkspacePluginDirectoryWhenSourceIsMissing()
    {
        using var tempDirectory = new TemporaryDirectory();
        var repoRoot = tempDirectory.Path;
        var pluginDirectory = Path.Combine(repoRoot, "src", "Unityctl.Plugin");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(repoRoot, "unityctl.slnx"), string.Empty);
        File.WriteAllText(Path.Combine(pluginDirectory, "package.json"), "{}");

        var toolBinDirectory = Path.Combine(repoRoot, "artifacts", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(toolBinDirectory);

        var success = PluginSourceLocator.TryResolvePackageSource(
            source: null,
            packageSource: out var packageSource,
            resolvedDirectory: out var resolvedDirectory,
            error: out var error,
            baseDirectory: toolBinDirectory);

        Assert.True(success, error);
        Assert.Equal(Path.GetFullPath(pluginDirectory), resolvedDirectory);
        Assert.Equal($"file:{resolvedDirectory!.Replace('\\', '/')}", packageSource);
    }

    [Fact]
    public void TryResolvePackageSource_NormalizesCustomRelativeSource()
    {
        using var tempDirectory = new TemporaryDirectory();
        var repoRoot = tempDirectory.Path;
        var pluginDirectory = Path.Combine(repoRoot, "custom-plugin");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "package.json"), "{}");

        var workspaceDir = Path.Combine(repoRoot, "workspace");
        Directory.CreateDirectory(workspaceDir);

        var success = PluginSourceLocator.TryResolvePackageSource(
            source: ".." + Path.DirectorySeparatorChar + "custom-plugin",
            packageSource: out var packageSource,
            resolvedDirectory: out var resolvedDirectory,
            error: out var error,
            baseDirectory: workspaceDir);

        Assert.True(success, error);
        Assert.Equal(Path.GetFullPath(pluginDirectory), resolvedDirectory);
        Assert.Equal($"file:{resolvedDirectory!.Replace('\\', '/')}", packageSource);
    }

    [Fact]
    public void TryResolvePackageSource_AcceptsExplicitGitUrl()
    {
        const string gitUrl = "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0";

        var success = PluginSourceLocator.TryResolvePackageSource(
            source: gitUrl,
            packageSource: out var packageSource,
            resolvedDirectory: out var resolvedDirectory,
            error: out var error);

        Assert.True(success, error);
        Assert.Equal(gitUrl, packageSource);
        Assert.Null(resolvedDirectory);
    }

    [Fact]
    public void TryResolvePackageSource_RejectsRemoteUrlWithoutGitSuffix()
    {
        const string invalidUrl = "https://github.com/kimjuyoung1127/unityctl?path=/src/Unityctl.Plugin";

        var success = PluginSourceLocator.TryResolvePackageSource(
            source: invalidUrl,
            packageSource: out _,
            resolvedDirectory: out _,
            error: out var error);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains(".git", error);
    }

    [Fact]
    public void TryResolvePackageSource_RejectsDirectoriesWithoutPackageJson()
    {
        using var tempDirectory = new TemporaryDirectory();
        var pluginDirectory = Path.Combine(tempDirectory.Path, "broken-plugin");
        Directory.CreateDirectory(pluginDirectory);

        var success = PluginSourceLocator.TryResolvePackageSource(
            source: pluginDirectory,
            packageSource: out _,
            resolvedDirectory: out _,
            error: out var error);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("package.json", error);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
