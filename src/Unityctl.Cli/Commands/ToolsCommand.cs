using System.Text.Json;
using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Shared.Commands;

namespace Unityctl.Cli.Commands;

public static class ToolsCommand
{
    public static void Execute(bool json = false)
    {
        var tools = GetToolDefinitions();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(tools, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
            }));
        }
        else
        {
            var console = ConsoleOutput.CreateOut();
            var tree = new Tree(
                new Markup($"[bold]unityctl[/] v{Markup.Escape(Unityctl.Shared.Constants.Version)} [dim]— {tools.Length} tools[/]"));

            foreach (var tool in tools)
            {
                var toolNode = tree.AddNode(
                    new Markup($"[cyan]{Markup.Escape(tool.Name)}[/]  [dim]{Markup.Escape(tool.Description)}[/]"));

                foreach (var p in tool.Parameters)
                {
                    var req = p.Required ? " [red](required)[/]" : "";
                    toolNode.AddNode(
                        new Markup($"[grey]--{Markup.Escape(p.Name)}[/]  [dim]{Markup.Escape(p.Type)}[/]  {Markup.Escape(p.Description)}{req}"));
                }
            }

            console.Write(tree);
        }
    }

    internal static CommandDefinition[] GetToolDefinitions()
        => CommandCatalog.All;
}
