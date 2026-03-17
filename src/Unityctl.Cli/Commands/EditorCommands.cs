using System.Text.Json;
using Unityctl.Cli.Infrastructure;
using Unityctl.Cli.Platform;
using Unityctl.Shared.Serialization;

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
            Console.WriteLine($"Found {editors.Count} Unity Editor(s):");
            Console.WriteLine();
            foreach (var editor in editors)
            {
                Console.Write("  ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(editor.Version);
                Console.ResetColor();
                Console.WriteLine($"  {editor.Location}");
            }
        }
    }
}
