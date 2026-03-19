namespace Unityctl.Core.Setup;

public static class PluginSourceLocator
{
    private const string PluginPackageFileName = "package.json";
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    public static bool TryResolvePackageSource(
        string? source,
        out string packageSource,
        out string? resolvedDirectory,
        out string? error,
        string? baseDirectory = null)
    {
        packageSource = string.Empty;
        resolvedDirectory = null;
        error = null;

        if (!string.IsNullOrWhiteSpace(source) && LooksLikeRemotePackageSource(source.Trim()))
            return TryResolveRemotePackageSource(source, out packageSource, out error);

        var candidateDirectory = string.IsNullOrWhiteSpace(source)
            ? TryResolveWorkspacePluginDirectory(baseDirectory)
            : GetCandidateDirectory(source, baseDirectory);

        if (candidateDirectory == null)
        {
            error = string.IsNullOrWhiteSpace(source)
                ? "Could not locate src/Unityctl.Plugin from the current unityctl workspace."
                : $"Plugin source '{source}' could not be resolved.";
            return false;
        }

        if (!TryValidatePluginDirectory(candidateDirectory, out resolvedDirectory, out error))
            return false;

        packageSource = $"file:{resolvedDirectory!.Replace('\\', '/')}";
        return true;
    }

    public static string? TryResolveWorkspacePluginDirectory(string? baseDirectory = null)
    {
        foreach (var startDirectory in EnumerateSearchRoots(baseDirectory))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current != null)
            {
                var workspaceSolutionPath = Path.Combine(current.FullName, "unityctl.slnx");
                if (!File.Exists(workspaceSolutionPath))
                {
                    current = current.Parent;
                    continue;
                }

                var candidateDirectory = Path.Combine(current.FullName, "src", "Unityctl.Plugin");
                if (TryValidatePluginDirectory(candidateDirectory, out var resolvedDirectory, out _))
                    return resolvedDirectory;

                current = current.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots(string? baseDirectory)
    {
        var seen = new HashSet<string>(PathComparer);

        foreach (var candidate in new[] { baseDirectory, AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var fullPath = Path.GetFullPath(candidate);
            if (seen.Add(fullPath))
                yield return fullPath;
        }
    }

    private static string? GetCandidateDirectory(string source, string? baseDirectory)
    {
        var pathPart = source.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? source["file:".Length..]
            : source;

        if (string.IsNullOrWhiteSpace(pathPart))
            return null;

        return string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.GetFullPath(pathPart)
            : Path.GetFullPath(pathPart, Path.GetFullPath(baseDirectory));
    }

    private static bool TryResolveRemotePackageSource(
        string? source,
        out string packageSource,
        out string? error)
    {
        packageSource = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(source))
            return false;

        var trimmed = source.Trim();
        if (!LooksLikeRemotePackageSource(trimmed))
            return false;

        if (!TryValidateRemotePackageSource(trimmed, out error))
            return false;

        packageSource = trimmed;
        return true;
    }

    private static bool LooksLikeRemotePackageSource(string source)
    {
        if (source.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return false;

        return source.Contains("://", StringComparison.Ordinal) || source.StartsWith("git@", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateRemotePackageSource(string source, out string? error)
    {
        error = null;

        if (source.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
        {
            if (!source.Contains(":", StringComparison.Ordinal) || !source.Contains(".git", StringComparison.OrdinalIgnoreCase))
            {
                error = "Remote plugin source must be a valid Git URL. Expected '.git' and a repository path.";
                return false;
            }

            return true;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            error = $"Plugin source '{source}' could not be parsed as a local path or Git URL.";
            return false;
        }

        if (uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals("ssh", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Remote plugin source scheme '{uri.Scheme}' is not supported. Use https://, http://, ssh://, or a local path.";
            return false;
        }

        if (!source.Contains(".git", StringComparison.OrdinalIgnoreCase))
        {
            error = "Remote plugin source must be a Unity-compatible Git URL ending in '.git' (query and fragment are allowed).";
            return false;
        }

        return true;
    }

    private static bool TryValidatePluginDirectory(
        string candidateDirectory,
        out string? resolvedDirectory,
        out string? error)
    {
        resolvedDirectory = null;
        error = null;

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidateDirectory));
        if (!Directory.Exists(fullPath))
        {
            error = $"Plugin source directory not found: {fullPath}";
            return false;
        }

        var packageJsonPath = Path.Combine(fullPath, PluginPackageFileName);
        if (!File.Exists(packageJsonPath))
        {
            error = $"Plugin source directory is missing {PluginPackageFileName}: {fullPath}";
            return false;
        }

        resolvedDirectory = fullPath;
        return true;
    }
}
