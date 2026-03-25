using System.Reflection;
using System.Text.RegularExpressions;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Shared.Tests;

public class CommandSyncGuardrailTests
{
    private static readonly Regex AppAddRegex = new(@"app\.Add\(""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex WellKnownRefRegex = new(@"WellKnownCommands\.(\w+)", RegexOptions.Compiled);
    private static readonly Regex PluginConstRegex = new(@"public const string (\w+) = ""([^""]+)"";", RegexOptions.Compiled);
    private static readonly Regex PluginHandlerRegex = new(@"CommandName\s*=>\s*WellKnownCommands\.(\w+)", RegexOptions.Compiled);

    [Fact]
    public void PluginSharedWellKnownCommands_CopyMatchesSharedDefinition()
    {
        var expected = GetSharedWellKnownConstants()
            .Where(pair => pair.Key is not nameof(WellKnownCommands.Schema)
                and not nameof(WellKnownCommands.Workflow))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var pluginCopy = ParsePluginWellKnownConstants();

        Assert.Equal(expected, pluginCopy);
    }

    [Fact]
    public void PluginCommandHandlers_CoverAllTransportCommands()
    {
        var expectedFields = ParsePluginWellKnownConstants()
            .Keys
            .Where(field => field is not nameof(WellKnownCommands.Watch))
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToArray();

        var actualFields = ParsePluginHandlerFieldNames()
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedFields, actualFields);
    }

    [Fact]
    public void WatchCommand_UsesDedicatedIpcPath_InPlugin()
    {
        var source = ReadRepoFile(@"src\Unityctl.Plugin\Editor\Ipc\IpcServer.cs");

        Assert.Contains("WellKnownCommands.Watch", source);
        Assert.Contains("watch session started", source);
    }

    [Fact]
    public void ScriptCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("script get-errors", cliCommands);
        Assert.Contains("script find-refs", cliCommands);
        Assert.Contains("script rename-symbol", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ScriptGetErrors), queryAllowlist);
        Assert.Contains(nameof(WellKnownCommands.ScriptFindRefs), queryAllowlist);
        Assert.DoesNotContain(nameof(WellKnownCommands.ScriptRenameSymbol), queryAllowlist);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ScriptRenameSymbol), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.ScriptGetErrors), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.ScriptFindRefs), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.ScriptRenameSymbol), pluginHandlers);
    }

    [Fact]
    public void UiReadCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("ui find", cliCommands);
        Assert.Contains("ui get", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.UiFind), queryAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UiGet), queryAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.UiFind), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UiGet), pluginHandlers);
    }

    [Fact]
    public void UiInteractionCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("ui click", cliCommands);
        Assert.Contains("ui toggle", cliCommands);
        Assert.Contains("ui input", cliCommands);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.UiClick), runAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UiToggle), runAllowlist);
        Assert.Contains(nameof(WellKnownCommands.UiInput), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.UiClick), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UiToggle), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.UiInput), pluginHandlers);
    }

    [Fact]
    public void ExecStructuredCommands_AreRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("exec list-callables", cliCommands);
        Assert.Contains("exec invoke", cliCommands);

        var queryAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\QueryTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ExecListCallables), queryAllowlist);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.ExecInvoke), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.ExecListCallables), pluginHandlers);
        Assert.Contains(nameof(WellKnownCommands.ExecInvoke), pluginHandlers);
    }

    [Fact]
    public void MeshCreatePrimitive_IsRegisteredAcrossCliMcpAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("mesh create-primitive", cliCommands);

        var runAllowlist = ParseWellKnownFieldReferences(@"src\Unityctl.Mcp\Tools\RunTool.cs");
        Assert.Contains(nameof(WellKnownCommands.MeshCreatePrimitive), runAllowlist);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.MeshCreatePrimitive), pluginHandlers);
    }

    [Fact]
    public void TestResult_IsRegisteredAcrossCliAndPlugin()
    {
        var cliCommands = ParseCliCommands();
        Assert.Contains("test-result", cliCommands);

        var pluginHandlers = ParsePluginHandlerFieldNames();
        Assert.Contains(nameof(WellKnownCommands.TestResult), pluginHandlers);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.Combine(GetRepoRoot(), normalized);
        return File.ReadAllText(path);
    }

    private static string GetRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
    }

    private static Dictionary<string, string> GetSharedWellKnownConstants()
    {
        return typeof(WellKnownCommands)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(
                field => field.Name,
                field => (string)field.GetRawConstantValue()!,
                StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ParsePluginWellKnownConstants()
    {
        var source = ReadRepoFile(@"src\Unityctl.Plugin\Editor\Shared\WellKnownCommands.cs");

        return PluginConstRegex
            .Matches(source)
            .Select(match => (Field: match.Groups[1].Value, Value: match.Groups[2].Value))
            .ToDictionary(item => item.Field, item => item.Value, StringComparer.Ordinal);
    }

    private static HashSet<string> ParsePluginHandlerFieldNames()
    {
        var commandsDir = Path.Combine(GetRepoRoot(), "src", "Unityctl.Plugin", "Editor", "Commands");
        var files = Directory.GetFiles(commandsDir, "*Handler.cs", SearchOption.TopDirectoryOnly);

        return files
            .SelectMany(path => PluginHandlerRegex.Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> ParseWellKnownFieldReferences(string relativePath)
    {
        var source = ReadRepoFile(relativePath);
        return WellKnownRefRegex
            .Matches(source)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> ParseCliCommands()
    {
        var source = ReadRepoFile(@"src\Unityctl.Cli\Program.cs");
        return AppAddRegex
            .Matches(source)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}
