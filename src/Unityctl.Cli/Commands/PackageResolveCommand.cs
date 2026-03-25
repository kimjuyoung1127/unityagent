using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PackageResolveCommand
{
    public static void Execute(string? project = null, string? package = null, bool json = false)
    {
        if (!CommandRunner.TryResolveProject(project, out var resolvedProject, out var failureResponse))
        {
            CommandRunner.PrintResponse(failureResponse!, json);
            Environment.ExitCode = 1;
            return;
        }

        var response = ResolveAsync(resolvedProject, package).GetAwaiter().GetResult();
        CommandRunner.PrintResponse(resolvedProject, response, json);
        Environment.ExitCode = CommandRunner.GetExitCode(response);
    }

    internal static async Task<CommandResponse> ResolveAsync(
        string project,
        string? package = null,
        Func<string, Task<CommandResponse>>? packageListAsync = null)
    {
        packageListAsync ??= DefaultPackageListAsync;

        var manifestDependencies = ReadManifestDependencies(project);
        var lockDependencies = ReadDependencyObject(Path.Combine(project, "Packages", "packages-lock.json"));
        var projectResolution = ReadJsonObject(Path.Combine(project, "Library", "PackageManager", "projectResolution.json"));

        var packageListResponse = await packageListAsync(project).ConfigureAwait(false);
        if (!packageListResponse.Success)
            return packageListResponse;

        var resolvedPackages = ParseResolvedPackages(packageListResponse.Data?["packages"] as JsonArray);

        IEnumerable<string> packageNames = manifestDependencies.Keys
            .Concat(lockDependencies.Keys)
            .Concat(resolvedPackages.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(package))
            packageNames = packageNames.Where(name => string.Equals(name, package, StringComparison.OrdinalIgnoreCase));

        var packages = new JsonArray();
        var mismatchCount = 0;

        foreach (var packageName in packageNames)
        {
            manifestDependencies.TryGetValue(packageName, out var manifestTarget);
            lockDependencies.TryGetValue(packageName, out var lockEntry);
            resolvedPackages.TryGetValue(packageName, out var resolvedEntry);
            var resolutionEntry = FindPackageEntry(projectResolution, packageName);

            var manifestExpectation = InterpretManifestTarget(manifestTarget);
            var resolvedVersion = resolvedEntry?["version"]?.GetValue<string>();
            var resolvedSource = resolvedEntry?["source"]?.GetValue<string>();
            var resolvedSourceKind = NormalizeResolvedSourceKind(resolvedSource);
            var lockVersion = lockEntry?["version"]?.GetValue<string>();
            var lockSource = lockEntry?["source"]?.GetValue<string>();
            var resolutionVersion = GetVersionFromNode(resolutionEntry);

            var mismatch = new JsonObject
            {
                ["manifestVersionVsResolved"] = manifestExpectation.ExpectedVersion != null
                    && resolvedVersion != null
                    && !VersionsMatch(manifestExpectation.ExpectedVersion, resolvedVersion),
                ["resolvedVsLockVersion"] = lockVersion != null
                    && ShouldCompareLockVersion(lockVersion, lockSource)
                    && resolvedVersion != null
                    && !VersionsMatch(lockVersion, resolvedVersion),
                ["resolutionVsResolvedVersion"] = resolutionVersion != null
                    && resolvedVersion != null
                    && !VersionsMatch(resolutionVersion, resolvedVersion),
                ["manifestSourceVsResolved"] = manifestExpectation.SourceKind != null
                    && resolvedSourceKind != null
                    && !string.Equals(manifestExpectation.SourceKind, resolvedSourceKind, StringComparison.OrdinalIgnoreCase)
            };

            var hasMismatch = mismatch.AsObject().Any(pair => pair.Value?.GetValue<bool>() == true);
            if (hasMismatch)
                mismatchCount++;

            var result = new JsonObject
            {
                ["name"] = packageName,
                ["manifest"] = new JsonObject
                {
                    ["target"] = manifestTarget,
                    ["sourceKind"] = manifestExpectation.SourceKind,
                    ["expectedVersion"] = manifestExpectation.ExpectedVersion
                },
                ["resolved"] = resolvedEntry?.DeepClone(),
                ["lock"] = lockEntry?.DeepClone(),
                ["projectResolution"] = resolutionEntry?.DeepClone(),
                ["mismatch"] = mismatch,
                ["hasMismatch"] = hasMismatch,
                ["recommendedNextCommand"] = hasMismatch
                    ? $"Restart Unity and re-run `unityctl package resolve --project \"{project}\"{BuildPackageArgument(packageName)}`."
                    : "No package drift detected."
            };

            packages.Add(result);
        }

        return CommandResponse.Ok(
            mismatchCount == 0
                ? $"Resolved {packages.Count} package(s) with no manifest/cache drift."
                : $"Resolved {packages.Count} package(s); detected {mismatchCount} package mismatch(es).",
            new JsonObject
            {
                ["package"] = package,
                ["packages"] = packages,
                ["count"] = packages.Count,
                ["hasMismatch"] = mismatchCount > 0
            });
    }

    internal static PackageManifestExpectation InterpretManifestTarget(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new PackageManifestExpectation(null, null);

        var trimmed = target.Trim();
        if (trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return new PackageManifestExpectation("local-file", null);

        if (trimmed.StartsWith("git+", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains(".git", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("?path=", StringComparison.OrdinalIgnoreCase))
        {
            return new PackageManifestExpectation("git", ExtractFragmentVersion(trimmed));
        }

        return new PackageManifestExpectation("registry", trimmed);
    }

    private static Dictionary<string, string> ReadManifestDependencies(string project)
    {
        var manifestPath = Path.Combine(project, "Packages", "manifest.json");
        var manifest = ReadJsonObject(manifestPath);
        var dependencies = manifest?["dependencies"] as JsonObject;

        if (dependencies == null)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return dependencies
            .Where(pair => pair.Value != null)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value!.GetValue<string>(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, JsonObject> ReadDependencyObject(string path)
    {
        var root = ReadJsonObject(path);
        var dependencies = root?["dependencies"] as JsonObject;

        if (dependencies == null)
            return new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);

        return dependencies
            .Where(pair => pair.Value is JsonObject)
            .ToDictionary(
                pair => pair.Key,
                pair => (JsonObject)pair.Value!,
                StringComparer.OrdinalIgnoreCase);
    }

    private static JsonObject? ReadJsonObject(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, JsonObject> ParseResolvedPackages(JsonArray? packages)
    {
        var results = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        if (packages == null)
            return results;

        foreach (var node in packages)
        {
            if (node is not JsonObject packageObject)
                continue;

            var name = packageObject["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            results[name] = packageObject;
        }

        return results;
    }

    private static JsonNode? FindPackageEntry(JsonNode? node, string packageName)
    {
        if (node is JsonObject jsonObject)
        {
            if (jsonObject.TryGetPropertyValue(packageName, out var direct))
                return direct;

            foreach (var property in jsonObject)
            {
                var found = FindPackageEntry(property.Value, packageName);
                if (found != null)
                    return found;
            }
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                var found = FindPackageEntry(item, packageName);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static string? GetVersionFromNode(JsonNode? node)
    {
        if (node is JsonObject jsonObject
            && jsonObject.TryGetPropertyValue("version", out var versionNode)
            && versionNode != null)
        {
            return versionNode.GetValue<string>();
        }

        return null;
    }

    private static string? ExtractFragmentVersion(string manifestTarget)
    {
        var fragmentIndex = manifestTarget.LastIndexOf('#');
        if (fragmentIndex < 0 || fragmentIndex == manifestTarget.Length - 1)
            return null;

        return manifestTarget[(fragmentIndex + 1)..].TrimStart('v', 'V');
    }

    private static bool VersionsMatch(string left, string right)
    {
        return string.Equals(
            left.Trim().TrimStart('v', 'V'),
            right.Trim().TrimStart('v', 'V'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeResolvedSourceKind(string? source)
    {
        return source?.Trim().ToLowerInvariant() switch
        {
            "local" => "local-file",
            "git" => "git",
            "registry" => "registry",
            "builtin" => "builtin",
            "embedded" => "embedded",
            _ => source
        };
    }

    private static bool ShouldCompareLockVersion(string? lockVersion, string? lockSource)
    {
        if (string.IsNullOrWhiteSpace(lockVersion))
            return false;

        if (string.Equals(lockSource, "local", StringComparison.OrdinalIgnoreCase))
            return false;

        return LooksLikeVersion(lockVersion);
    }

    private static bool LooksLikeVersion(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[1..];

        return trimmed.Length > 0 && char.IsDigit(trimmed[0]);
    }

    private static string BuildPackageArgument(string packageName)
    {
        return string.IsNullOrWhiteSpace(packageName)
            ? string.Empty
            : $" --package {packageName}";
    }

    private static async Task<CommandResponse> DefaultPackageListAsync(string project)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        return await executor.ExecuteAsync(
            project,
            PackageCommand.CreateListRequest(),
            retry: false).ConfigureAwait(false);
    }
}

internal sealed record PackageManifestExpectation(string? SourceKind, string? ExpectedVersion);
