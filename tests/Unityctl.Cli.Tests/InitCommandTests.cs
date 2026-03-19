using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Xunit;

namespace Unityctl.Cli.Tests;

[Collection("ConsoleOutput")]
public class InitCommandTests
{
    [CliTestFact]
    public void Execute_WithExplicitGitSource_WritesManifestDependency()
    {
        using var tempProject = new TemporaryProject();
        const string gitSource = "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0";

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            InitCommand.Execute(tempProject.Path, gitSource);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var manifest = JsonNode.Parse(File.ReadAllText(tempProject.ManifestPath))!.AsObject();
        var dependencies = manifest["dependencies"]!.AsObject();

        Assert.Equal(gitSource, dependencies["com.unityctl.bridge"]?.GetValue<string>());

        var output = writer.ToString();
        Assert.Contains("Added com.unityctl.bridge", output);
        Assert.Contains($"Package source: {gitSource}", output);
        Assert.DoesNotContain("Resolved plugin directory:", output);
    }

    private sealed class TemporaryProject : IDisposable
    {
        public TemporaryProject()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-init-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Packages"));
            ManifestPath = System.IO.Path.Combine(Path, "Packages", "manifest.json");
            File.WriteAllText(ManifestPath, """
{
  "dependencies": {
    "com.unity.ugui": "2.0.0"
  }
}
""");
        }

        public string ManifestPath { get; }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
