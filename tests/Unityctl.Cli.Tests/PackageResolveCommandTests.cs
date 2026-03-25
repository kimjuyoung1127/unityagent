using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class PackageResolveCommandTests
{
    [CliTestFact]
    public void InterpretManifestTarget_ParsesGitTagVersion()
    {
        var expectation = PackageResolveCommand.InterpretManifestTarget(
            "https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.3.2");

        Assert.Equal("git", expectation.SourceKind);
        Assert.Equal("0.3.2", expectation.ExpectedVersion);
    }

    [CliTestFact]
    public void InterpretManifestTarget_ParsesLocalFile()
    {
        var expectation = PackageResolveCommand.InterpretManifestTarget(
            "file:C:/repo/src/Unityctl.Plugin");

        Assert.Equal("local-file", expectation.SourceKind);
        Assert.Null(expectation.ExpectedVersion);
    }

    [CliTestFact]
    public async Task ResolveAsync_ReportsManifestAndResolvedMismatch()
    {
        using var tempProject = new TemporaryPackageProject(
            """
            {
              "dependencies": {
                "com.unityctl.bridge": "https://github.com/Jason-hub-star/unityctl.git?path=/src/Unityctl.Plugin#v0.3.2"
              }
            }
            """,
            """
            {
              "dependencies": {
                "com.unityctl.bridge": {
                  "version": "0.3.1"
                }
              }
            }
            """,
            """
            {
              "dependencies": {
                "com.unityctl.bridge": {
                  "version": "0.3.1"
                }
              }
            }
            """);

        var response = await PackageResolveCommand.ResolveAsync(
            tempProject.Path,
            "com.unityctl.bridge",
            _ => Task.FromResult(CommandResponse.Ok("packages", new JsonObject
            {
                ["packages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "com.unityctl.bridge",
                        ["version"] = "0.3.1",
                        ["source"] = "Git"
                    }
                }
            })));

        Assert.True(response.Success);
        Assert.True(response.Data!["hasMismatch"]!.GetValue<bool>());

        var package = response.Data["packages"]!.AsArray()[0]!.AsObject();
        Assert.True(package["hasMismatch"]!.GetValue<bool>());
        Assert.True(package["mismatch"]!["manifestVersionVsResolved"]!.GetValue<bool>());
    }

    [CliTestFact]
    public async Task ResolveAsync_DoesNotFlagLocalLockVersionAsMismatch()
    {
        using var tempProject = new TemporaryPackageProject(
            """
            {
              "dependencies": {
                "com.unityctl.bridge": "file:C:/repo/src/Unityctl.Plugin"
              }
            }
            """,
            """
            {
              "dependencies": {
                "com.unityctl.bridge": {
                  "version": "file:C:/repo/src/Unityctl.Plugin",
                  "source": "local"
                }
              }
            }
            """,
            "{}");

        var response = await PackageResolveCommand.ResolveAsync(
            tempProject.Path,
            "com.unityctl.bridge",
            _ => Task.FromResult(CommandResponse.Ok("packages", new JsonObject
            {
                ["packages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "com.unityctl.bridge",
                        ["version"] = "0.3.2",
                        ["source"] = "Local"
                    }
                }
            })));

        var package = response.Data!["packages"]!.AsArray()[0]!.AsObject();
        Assert.False(package["mismatch"]!["resolvedVsLockVersion"]!.GetValue<bool>());
    }

    private sealed class TemporaryPackageProject : IDisposable
    {
        public TemporaryPackageProject(string manifestJson, string packagesLockJson, string projectResolutionJson)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unityctl-package-resolve-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Packages"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Library", "PackageManager"));
            File.WriteAllText(System.IO.Path.Combine(Path, "Packages", "manifest.json"), manifestJson);
            File.WriteAllText(System.IO.Path.Combine(Path, "Packages", "packages-lock.json"), packagesLockJson);
            File.WriteAllText(System.IO.Path.Combine(Path, "Library", "PackageManager", "projectResolution.json"), projectResolutionJson);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); } catch { }
        }
    }
}
