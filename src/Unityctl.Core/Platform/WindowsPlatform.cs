using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using Unityctl.Shared.Models;

namespace Unityctl.Core.Platform;

public sealed class WindowsPlatform : PlatformServicesBase
{
    private static readonly Regex ProjectPathRegex = new(
        @"(?:^|\s)-projectPath\s+(?:""(?<quoted>[^""]+)""|(?<plain>[^\s]+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override string GetUnityHubEditorsJsonPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "UnityHub", "editors.json");
    }

    public override IEnumerable<string> GetDefaultEditorSearchPaths()
    {
        yield return @"C:\Program Files\Unity\Hub\Editor";
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Unity", "Hub", "Editor");
    }

    public override string GetUnityExecutablePath(string editorBasePath)
        => Path.Combine(editorBasePath, "Editor", "Unity.exe");

    public override IEnumerable<UnityProcessInfo> FindRunningUnityProcesses()
    {
        if (!OperatingSystem.IsWindows())
            return [];

        var processes = new List<UnityProcessInfo>();
        ManagementObjectCollection? results = null;
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine, ExecutablePath FROM Win32_Process WHERE Name = 'Unity.exe'");
            results = searcher.Get();
            foreach (ManagementObject process in results)
            {
                var commandLine = process["CommandLine"] as string;
                var executablePath = process["ExecutablePath"] as string;
                var processId = Convert.ToInt32(process["ProcessId"]);

                processes.Add(new UnityProcessInfo
                {
                    ProcessId = processId,
                    ProjectPath = TryParseProjectPath(commandLine),
                    Version = TryParseVersionFromExecutablePath(executablePath),
                    ExecutablePath = executablePath,
                    IsBatchMode = IsBatchModeCommandLine(commandLine),
                    HasMainWindow = TryDetectMainWindow(processId),
                    CommandLineSource = commandLine
                });
            }
        }
        catch
        {
            return [];
        }
        finally
        {
            results?.Dispose();
        }

        return processes;
    }

    internal static string? TryParseProjectPath(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return null;

        var match = ProjectPathRegex.Match(commandLine);
        if (!match.Success)
            return null;

        var value = match.Groups["quoted"].Success
            ? match.Groups["quoted"].Value
            : match.Groups["plain"].Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return Path.GetFullPath(value);
        }
        catch
        {
            return null;
        }
    }

    internal static string? TryParseVersionFromExecutablePath(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return null;

        try
        {
            var editorDirectory = Path.GetDirectoryName(executablePath);
            if (string.IsNullOrWhiteSpace(editorDirectory))
                return null;

            var versionDirectory = Directory.GetParent(editorDirectory);
            return versionDirectory?.Name;
        }
        catch
        {
            return null;
        }
    }

    internal static bool IsBatchModeCommandLine(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return false;

        return ContainsSwitch(commandLine, "-batchmode")
               || ContainsSwitch(commandLine, "-nographics")
               || ContainsSwitch(commandLine, "-adb2");
    }

    private static bool ContainsSwitch(string commandLine, string switchName)
    {
        return commandLine.IndexOf($" {switchName} ", StringComparison.OrdinalIgnoreCase) >= 0
               || commandLine.IndexOf($" {switchName}\"", StringComparison.OrdinalIgnoreCase) >= 0
               || commandLine.IndexOf($"\"{switchName}\"", StringComparison.OrdinalIgnoreCase) >= 0
               || commandLine.EndsWith($" {switchName}", StringComparison.OrdinalIgnoreCase)
               || commandLine.StartsWith($"{switchName} ", StringComparison.OrdinalIgnoreCase)
               || commandLine.IndexOf(switchName, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryDetectMainWindow(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.MainWindowHandle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }
}
