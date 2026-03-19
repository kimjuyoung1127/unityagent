using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using Unityctl.Mcp.Tools;
using Xunit;

namespace Unityctl.Mcp.Tests;

/// <summary>
/// Verifies that all MCP tool types are correctly annotated.
/// Uses reflection so tests remain independent of the hosting infrastructure.
/// </summary>
public class ToolAnnotationTests
{
    // All tool types defined in the Mcp assembly
    private static readonly Type[] ToolTypes =
    [
        typeof(PingTool),
        typeof(StatusTool),
        typeof(BuildTool),
        typeof(TestTool),
        typeof(CheckTool),
        typeof(LogTool),
        typeof(WatchTool),
        typeof(SessionTool),
        typeof(SchemaTool),
        typeof(ExecTool),
        typeof(RunTool),
        typeof(QueryTool)
    ];

    [Fact]
    public void AllToolTypes_HaveMcpServerToolTypeAttribute()
    {
        foreach (var type in ToolTypes)
        {
            Assert.True(
                type.GetCustomAttribute<McpServerToolTypeAttribute>() != null,
                $"{type.Name} is missing [McpServerToolType]");
        }
    }

    [Fact]
    public void AllToolMethods_HaveDescriptionAttribute()
    {
        foreach (var type in ToolTypes)
        {
            var toolMethods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
                .ToArray();

            Assert.True(toolMethods.Length > 0, $"{type.Name} has no [McpServerTool] methods");

            foreach (var method in toolMethods)
            {
                Assert.True(
                    method.GetCustomAttribute<DescriptionAttribute>() != null,
                    $"{type.Name}.{method.Name} is missing [Description]");
            }
        }
    }

    [Fact]
    public void AllToolMethods_HaveNonEmptyDescription()
    {
        foreach (var type in ToolTypes)
        {
            var toolMethods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

            foreach (var method in toolMethods)
            {
                var desc = method.GetCustomAttribute<DescriptionAttribute>();
                Assert.False(
                    string.IsNullOrWhiteSpace(desc?.Description),
                    $"{type.Name}.{method.Name} has empty description");
            }
        }
    }

    [Fact]
    public void AllToolMethods_HaveUniqueMcpToolNames()
    {
        var names = ToolTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>())
            .Where(a => a != null)
            .Select(a => a!.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AllToolMethods_ToolNamesStartWithUnityctl()
    {
        foreach (var type in ToolTypes)
        {
            var toolMethods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

            foreach (var method in toolMethods)
            {
                var attr = method.GetCustomAttribute<McpServerToolAttribute>()!;
                if (!string.IsNullOrEmpty(attr.Name))
                {
                    Assert.True(
                        attr.Name!.StartsWith("unityctl_", StringComparison.Ordinal),
                        $"{type.Name}.{method.Name} tool name '{attr.Name}' should start with 'unityctl_'");
                }
            }
        }
    }

    [Fact]
    public void TotalRegisteredTools_Equals12()
    {
        var toolCount = ToolTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            .Count(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

        Assert.Equal(12, toolCount);
    }

    [Fact]
    public void ProjectParameter_OnTools_HaveDescriptions()
    {
        foreach (var type in ToolTypes)
        {
            var toolMethods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);

            foreach (var method in toolMethods)
            {
                var projectParam = method.GetParameters()
                    .FirstOrDefault(p => p.Name == "project");

                if (projectParam != null)
                {
                    Assert.True(
                        projectParam.GetCustomAttribute<DescriptionAttribute>() != null,
                        $"{type.Name}.{method.Name} parameter 'project' is missing [Description]");
                }
            }
        }
    }
}
