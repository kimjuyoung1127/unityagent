using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Unityctl.Core.Verification;
using Xunit;

namespace Unityctl.Core.Tests.Verification;

public sealed class ImageDiffEngineTests : IDisposable
{
    private readonly string _tempDir;

    public ImageDiffEngineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"unityctl-image-diff-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Diff_WhenImagesAreIdentical_Passes()
    {
        var baseline = Path.Combine(_tempDir, "baseline.png");
        var candidate = Path.Combine(_tempDir, "candidate.png");
        var diffPath = Path.Combine(_tempDir, "diff.png");
        CreateSolidImage(baseline, new Rgba32(0, 255, 0));
        CreateSolidImage(candidate, new Rgba32(0, 255, 0));

        var result = new ImageDiffEngine().Diff(baseline, candidate, diffPath, 0.0);

        Assert.True(result.Passed);
        Assert.Equal(0, result.ChangedPixels);
        Assert.True(File.Exists(diffPath));
    }

    [Fact]
    public void Diff_WhenImagesDiffer_FailsAboveThreshold()
    {
        var baseline = Path.Combine(_tempDir, "baseline.png");
        var candidate = Path.Combine(_tempDir, "candidate.png");
        var diffPath = Path.Combine(_tempDir, "diff.png");
        CreateSolidImage(baseline, new Rgba32(0, 255, 0));
        CreateSolidImage(candidate, new Rgba32(255, 0, 0));

        var result = new ImageDiffEngine().Diff(baseline, candidate, diffPath, 0.0);

        Assert.False(result.Passed);
        Assert.True(result.ChangedPixels > 0);
        Assert.True(File.Exists(diffPath));
    }

    private static void CreateSolidImage(string path, Rgba32 color)
    {
        using var image = new Image<Rgba32>(8, 8, color);
        image.SaveAsPng(path);
    }
}
