using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class TextureCommandTests
{
    [Fact]
    public void GetImportSettingsRequest_HasCorrectCommand()
    {
        var request = TextureCommand.CreateGetImportSettingsRequest("Assets/Textures/icon.png");
        Assert.Equal(WellKnownCommands.TextureGetImportSettings, request.Command);
    }

    [Fact]
    public void GetImportSettingsRequest_SetsPath()
    {
        var request = TextureCommand.CreateGetImportSettingsRequest("Assets/Textures/icon.png");
        Assert.Equal("Assets/Textures/icon.png", request.Parameters!["path"]!.ToString());
    }

    [Fact]
    public void GetImportSettingsRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => TextureCommand.CreateGetImportSettingsRequest(""));
    }

    [Fact]
    public void SetImportSettingsRequest_HasCorrectCommand()
    {
        var request = TextureCommand.CreateSetImportSettingsRequest("Assets/Textures/icon.png", "maxTextureSize", "512");
        Assert.Equal(WellKnownCommands.TextureSetImportSettings, request.Command);
    }

    [Fact]
    public void SetImportSettingsRequest_SetsAllParameters()
    {
        var request = TextureCommand.CreateSetImportSettingsRequest("Assets/Textures/icon.png", "filterMode", "Trilinear");
        Assert.Equal("Assets/Textures/icon.png", request.Parameters!["path"]!.ToString());
        Assert.Equal("filterMode", request.Parameters!["property"]!.ToString());
        Assert.Equal("Trilinear", request.Parameters!["value"]!.ToString());
    }

    [Fact]
    public void SetImportSettingsRequest_EmptyPath_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TextureCommand.CreateSetImportSettingsRequest("", "maxTextureSize", "512"));
    }

    [Fact]
    public void SetImportSettingsRequest_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            TextureCommand.CreateSetImportSettingsRequest("Assets/Textures/icon.png", "", "512"));
    }

    [Fact]
    public void SetImportSettingsRequest_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TextureCommand.CreateSetImportSettingsRequest("Assets/Textures/icon.png", "maxTextureSize", null!));
    }

    [Fact]
    public void SetImportSettingsRequest_EmptyValue_Allowed()
    {
        var request = TextureCommand.CreateSetImportSettingsRequest("Assets/Textures/icon.png", "maxTextureSize", "");
        Assert.Equal("", request.Parameters!["value"]!.ToString());
    }
}
