using System.Diagnostics;
using System.Runtime.InteropServices;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Unityctl.Mcp.Tests;

public class McpBlackBoxTests
{
    private static readonly IReadOnlyCollection<string> ExpectedToolNames =
    [
        "unityctl_build",
        "unityctl_check",
        "unityctl_exec",
        "unityctl_log",
        "unityctl_ping",
        "unityctl_query",
        "unityctl_run",
        "unityctl_schema",
        "unityctl_session_list",
        "unityctl_status",
        "unityctl_test",
        "unityctl_watch"
    ];

    [Fact]
    public async Task Initialize_Completes_And_ListsExpectedTools()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        Assert.NotNull(harness.Client.ServerInfo);
        Assert.NotNull(harness.Client.ServerCapabilities);

        var tools = await harness.Client.ListToolsAsync(new RequestOptions(), CancellationToken.None);

        var names = tools.Select(tool => tool.Name).ToArray();

        Assert.Equal(ExpectedToolNames.Count, names.Length);
        Assert.Equal(ExpectedToolNames.OrderBy(name => name), names.OrderBy(name => name));
        Assert.All(tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.Description)));
    }

    [Fact]
    public async Task SchemaTool_ReturnsCommandSchema()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_schema",
            arguments: new Dictionary<string, object?>(),
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
        var payload = GetToolResultText(result);

        Assert.False(string.IsNullOrWhiteSpace(payload));
        Assert.Contains("\"version\"", payload);
        Assert.Contains("\"commands\"", payload);
    }

    [Fact]
    public async Task SchemaToolWithCategory_ReturnsFilteredResults()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_schema",
            arguments: new Dictionary<string, object?> { ["category"] = "query" },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
        var payload = GetToolResultText(result);

        Assert.Contains("\"version\"", payload);
        Assert.Contains("\"commands\"", payload);
        // query category should contain status, ping, etc.
        Assert.Contains("status", payload);
    }

    [Fact]
    public async Task SchemaToolWithUnknownCategory_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_schema",
            arguments: new Dictionary<string, object?> { ["category"] = "nonexistent" },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.Contains("error", payload);
        Assert.Contains("nonexistent", payload);
    }

    [Fact]
    public async Task InvalidToolName_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        await AssertMcpToolCallFailsAsync(
            async () => await harness.Client.CallToolAsync(
                "unityctl_nope",
                arguments: new Dictionary<string, object?>(),
                progress: null,
                options: new RequestOptions(),
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task MissingRequiredArgument_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        await AssertMcpToolCallFailsAsync(
            async () => await harness.Client.CallToolAsync(
                "unityctl_status",
                arguments: new Dictionary<string, object?>(),
                progress: null,
                options: new RequestOptions(),
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task SchemaToolWithCommand_ReturnsSingleDefinition()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_schema",
            arguments: new Dictionary<string, object?> { ["command"] = "gameobject-create" },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
        var payload = GetToolResultText(result);

        Assert.Contains("\"name\"", payload);
        Assert.Contains("gameobject-create", payload);
        Assert.DoesNotContain("\"commands\"", payload);
    }

    [Fact]
    public async Task SchemaToolWithUnknownCommand_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_schema",
            arguments: new Dictionary<string, object?> { ["command"] = "nonexistent" },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.Contains("error", payload);
        Assert.Contains("nonexistent", payload);
    }

    [Fact]
    public async Task RunTool_DisallowedCommand_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_run",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/path",
                ["command"] = "exec"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.Contains("not in the allowlist", payload);
    }

    [Fact]
    public async Task RunTool_InvalidParametersJson_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_run",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/path",
                ["command"] = "play-mode",
                ["parameters"] = "not-valid-json{"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.Contains("Invalid JSON", payload);
    }

    [Fact]
    public async Task RunTool_AllowedCommand_PassesAllowlistCheck()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        // play-mode is allowlisted — will fail on transport (no Unity running)
        // but should NOT fail on allowlist check
        var result = await harness.Client.CallToolAsync(
            "unityctl_run",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/nonexistent/path",
                ["command"] = "play-mode",
                ["parameters"] = "{\"action\":\"start\"}"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        // Should not contain allowlist error — will fail for transport/project reasons instead
        Assert.DoesNotContain("not in the allowlist", payload);
    }

    [Fact]
    public async Task RunTool_BatchExecute_AcceptsNestedCommandArray()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_run",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/nonexistent/path",
                ["command"] = "batch-execute",
                ["parameters"] =
                    "{\"rollbackOnFailure\":true,\"commands\":[{\"command\":\"gameobject-create\",\"parameters\":{\"name\":\"BatchProbe\"}}]}"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.DoesNotContain("not in the allowlist", payload);
        Assert.DoesNotContain("Invalid JSON", payload);
    }

    [Fact]
    public async Task QueryTool_DisallowedCommand_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_query",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/path",
                ["command"] = "play-mode"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.Contains("not in the query allowlist", payload);
    }

    [Fact]
    public async Task QueryTool_AllowedCommand_PassesAllowlistCheck()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_query",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/nonexistent/path",
                ["command"] = "asset-find",
                ["parameters"] = "{\"filter\":\"t:Scene\"}"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.DoesNotContain("not in the query allowlist", payload);
    }

    [Fact]
    public async Task QueryTool_InvalidParametersJson_ReturnsError()
    {
        await using var harness = await UnityctlMcpHarness.StartAsync();

        var result = await harness.Client.CallToolAsync(
            "unityctl_query",
            arguments: new Dictionary<string, object?>
            {
                ["project"] = "/fake/path",
                ["command"] = "asset-find",
                ["parameters"] = "not-valid-json{"
            },
            progress: null,
            options: new RequestOptions(),
            cancellationToken: CancellationToken.None);

        var payload = GetToolResultText(result);
        Assert.Contains("Invalid JSON", payload);
    }

    private static async Task AssertMcpToolCallFailsAsync(Func<ValueTask<CallToolResult>> call)
    {
        try
        {
            var result = await call();
            Assert.True(result.IsError, "Expected tool call to fail, but it succeeded.");
        }
        catch (McpException)
        {
            // Expected: invalid tool names / arguments are allowed to surface as MCP protocol errors.
        }
    }

    private static string GetToolResultText(CallToolResult result)
    {
        var structured = result.StructuredContent?.ToString();
        if (!string.IsNullOrWhiteSpace(structured))
        {
            return structured;
        }

        return string.Join(
            Environment.NewLine,
            result.Content.Select(block => block?.ToString()).Where(text => !string.IsNullOrWhiteSpace(text)));
    }
}

internal sealed class UnityctlMcpHarness : IAsyncDisposable
{
    private readonly List<string> _stderrLines = [];

    private UnityctlMcpHarness(McpClient client, StdioClientTransport transport, string executablePath, List<string> stderrLines)
    {
        Client = client;
        Transport = transport;
        ExecutablePath = executablePath;
        _stderrLines = stderrLines;
    }

    public McpClient Client { get; }

    public string ExecutablePath { get; }

    public StdioClientTransport Transport { get; }

    public IReadOnlyList<string> StandardErrorLines => _stderrLines;

    public static async Task<UnityctlMcpHarness> StartAsync(CancellationToken cancellationToken = default)
    {
        var executablePath = ResolveExecutablePath();
        var stderrLines = new List<string>();
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.None));

        var transportOptions = new StdioClientTransportOptions
        {
            Command = executablePath,
            Arguments = [],
            WorkingDirectory = RepoRoot,
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["Logging__LogLevel__Default"] = "None",
                ["Logging__LogLevel__Microsoft"] = "None",
                ["Logging__LogLevel__ModelContextProtocol"] = "None"
            },
            StandardErrorLines = line => stderrLines.Add(line)
        };

        var transport = new StdioClientTransport(transportOptions, loggerFactory);
        var client = await McpClient.CreateAsync(transport, new McpClientOptions(), loggerFactory, cancellationToken);

        return new UnityctlMcpHarness(client, transport, executablePath, stderrLines);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
    }

    private static string RepoRoot
    {
        get
        {
            var baseDir = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        }
    }

    private static string ResolveExecutablePath()
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "unityctl-mcp.exe" : "unityctl-mcp";
        // Debug first: dotnet test defaults to Debug, so prefer matching configuration
        // to avoid stale Release binaries silently breaking tests.
        var candidates = new[]
        {
            Path.Combine(RepoRoot, "src", "Unityctl.Mcp", "bin", "Debug", "net10.0", fileName),
            Path.Combine(RepoRoot, "src", "Unityctl.Mcp", "bin", "Release", "net10.0", fileName)
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path != null)
        {
            return path;
        }

        throw new FileNotFoundException(
            "Could not find the built unityctl-mcp executable. Build src/Unityctl.Mcp first.",
            candidates[0]);
    }
}
