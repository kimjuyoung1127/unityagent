using System.Text.Json;
using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;

namespace Unityctl.Cli.Commands;

public static class EditorCommands
{
    public static void List(bool json = false)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var editors = discovery.FindEditors();

        if (editors.Count == 0)
        {
            Console.Error.WriteLine("No Unity Editors found.");
            Console.Error.WriteLine("Tip: Install Unity via Unity Hub, or check search paths.");
            Environment.Exit(1);
            return;
        }

        if (json)
        {
            var jsonStr = JsonSerializer.Serialize(editors, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(jsonStr);
        }
        else
        {
            var console = ConsoleOutput.CreateOut();
            console.MarkupLine($"Found [cyan]{editors.Count}[/] Unity Editor(s):");
            console.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Version")
                .AddColumn("Location");

            foreach (var editor in editors)
            {
                table.AddRow(
                    new Markup($"[cyan]{Markup.Escape(editor.Version)}[/]"),
                    new Text(editor.Location));
            }

            console.Write(table);
        }
    }
}
