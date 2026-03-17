using Unityctl.Cli.Infrastructure;
using Xunit;

namespace Unityctl.Cli.Tests;

public class UnityEditorDiscoveryTests
{
    [Theory]
    [InlineData("m_EditorVersion: 2021.3.11f1\nm_EditorVersionWithRevision: 2021.3.11f1 (abc123)", "2021.3.11f1")]
    [InlineData("m_EditorVersion: 6000.0.64f1\n", "6000.0.64f1")]
    [InlineData("nothing here", null)]
    [InlineData("", null)]
    public void ParseProjectVersion_ExtractsVersion(string content, string? expected)
    {
        var result = UnityEditorDiscovery.ParseProjectVersion(content);
        Assert.Equal(expected, result);
    }
}
