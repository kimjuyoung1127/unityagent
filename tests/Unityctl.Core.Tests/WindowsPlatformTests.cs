using Unityctl.Core.Platform;
using Xunit;

namespace Unityctl.Core.Tests;

public sealed class WindowsPlatformTests
{
    [Fact]
    public void TryParseProjectPath_ParsesQuotedProjectPath()
    {
        var commandLine = "\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.64f1\\Editor\\Unity.exe\" -projectPath \"C:\\Users\\ezen601\\Desktop\\Jason\\My project\"";

        var projectPath = WindowsPlatform.TryParseProjectPath(commandLine);

        Assert.Equal(@"C:\Users\ezen601\Desktop\Jason\My project", projectPath);
    }

    [Fact]
    public void TryParseProjectPath_ReturnsNull_WhenArgumentMissing()
    {
        var projectPath = WindowsPlatform.TryParseProjectPath("\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.0.64f1\\Editor\\Unity.exe\"");

        Assert.Null(projectPath);
    }

    [Fact]
    public void TryParseVersionFromExecutablePath_ExtractsVersionDirectory()
    {
        var version = WindowsPlatform.TryParseVersionFromExecutablePath(
            @"C:\Program Files\Unity\Hub\Editor\6000.0.64f1\Editor\Unity.exe");

        Assert.Equal("6000.0.64f1", version);
    }
}
