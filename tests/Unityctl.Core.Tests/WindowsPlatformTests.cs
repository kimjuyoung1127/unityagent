using Unityctl.Core.Platform;
using Xunit;

namespace Unityctl.Core.Tests;

public sealed class WindowsPlatformTests
{
    [Fact]
    public void TryParseProjectPath_ParsesQuotedProjectPath()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var commandLine = "\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.64f1\\Editor\\Unity.exe\" -projectPath \"C:\\Users\\ezen601\\Desktop\\Jason\\My project\"";

        var projectPath = WindowsPlatform.TryParseProjectPath(commandLine);

        Assert.Equal(@"C:\Users\ezen601\Desktop\Jason\My project", projectPath);
    }

    [Fact]
    public void TryParseProjectPath_ReturnsNull_WhenArgumentMissing()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var projectPath = WindowsPlatform.TryParseProjectPath("\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.64f1\\Editor\\Unity.exe\"");

        Assert.Null(projectPath);
    }

    [Fact]
    public void TryParseVersionFromExecutablePath_ExtractsVersionDirectory()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var version = WindowsPlatform.TryParseVersionFromExecutablePath(
            @"C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe");

        Assert.Equal("6000.0.64f1", version);
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.64f1\\Editor\\Unity.exe\" -projectPath \"C:\\Project\" -batchmode -nographics", true)]
    [InlineData("\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.64f1\\Editor\\Unity.exe\" -projectPath \"C:\\Project\"", false)]
    public void IsBatchModeCommandLine_DetectsHeadlessProcesses(string commandLine, bool expected)
    {
        if (!OperatingSystem.IsWindows())
            return;

        Assert.Equal(expected, WindowsPlatform.IsBatchModeCommandLine(commandLine));
    }
}
