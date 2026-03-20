using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json.Nodes;

namespace Unityctl.Core.Verification;

public sealed class ImageDiffEngine
{
    public ImageDiffResult Diff(string baselinePath, string candidatePath, string diffImagePath, double maxChangedPixelRatio)
    {
        using var baseline = Image.Load<Rgba32>(baselinePath);
        using var candidate = Image.Load<Rgba32>(candidatePath);

        if (baseline.Width != candidate.Width || baseline.Height != candidate.Height)
        {
            return new ImageDiffResult
            {
                Passed = false,
                Message = "Baseline and candidate image dimensions do not match.",
                ChangedPixelRatio = 1.0,
                ChangedPixels = -1,
                TotalPixels = baseline.Width * baseline.Height
            };
        }

        var diff = new Image<Rgba32>(baseline.Width, baseline.Height);
        var totalPixels = baseline.Width * baseline.Height;
        var changedPixels = 0;

        for (var y = 0; y < baseline.Height; y++)
        {
            for (var x = 0; x < baseline.Width; x++)
            {
                var left = baseline[x, y];
                var right = candidate[x, y];
                if (left.Equals(right))
                {
                    diff[x, y] = new Rgba32(right.R, right.G, right.B, 80);
                    continue;
                }

                changedPixels++;
                diff[x, y] = new Rgba32(255, 0, 0, 255);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(diffImagePath)!);
        diff.SaveAsPng(diffImagePath);

        var changedPixelRatio = totalPixels == 0 ? 0 : (double)changedPixels / totalPixels;
        return new ImageDiffResult
        {
            Passed = changedPixelRatio <= maxChangedPixelRatio,
            Message = changedPixelRatio <= maxChangedPixelRatio
                ? "Image diff passed."
                : "Image diff exceeded threshold.",
            ChangedPixelRatio = changedPixelRatio,
            ChangedPixels = changedPixels,
            TotalPixels = totalPixels,
            DiffImagePath = diffImagePath
        };
    }
}

public sealed class ImageDiffResult
{
    public bool Passed { get; set; }

    public string Message { get; set; } = string.Empty;

    public double ChangedPixelRatio { get; set; }

    public int ChangedPixels { get; set; }

    public int TotalPixels { get; set; }

    public string? DiffImagePath { get; set; }

    public JsonObject ToJson()
    {
        return new JsonObject
        {
            ["changedPixelRatio"] = ChangedPixelRatio,
            ["changedPixels"] = ChangedPixels,
            ["totalPixels"] = TotalPixels,
            ["diffImagePath"] = DiffImagePath
        };
    }
}
