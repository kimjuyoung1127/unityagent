using Unityctl.Shared.Commands;
using Xunit;

namespace Unityctl.Shared.Tests;

public class CommandCatalogTests
{
    [Fact]
    public void All_HasStableCommandNames()
    {
        var names = CommandCatalog.All.Select(command => command.Name).ToArray();

        Assert.Equal(
            ["init", "editor list", "ping", "status", "build", "test", "check", "tools", "log"],
            names);
    }

    [Fact]
    public void All_HasNoDuplicateNames()
    {
        var names = CommandCatalog.All.Select(command => command.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RequiredParameters_AreMarkedCorrectly()
    {
        var init = CommandCatalog.All.Single(command => command.Name == "init");
        var status = CommandCatalog.All.Single(command => command.Name == "status");

        Assert.Contains(init.Parameters, parameter => parameter.Name == "project" && parameter.Required);
        Assert.Contains(status.Parameters, parameter => parameter.Name == "project" && parameter.Required);
        Assert.DoesNotContain(status.Parameters, parameter => parameter.Name == "json" && parameter.Required);
    }
}
