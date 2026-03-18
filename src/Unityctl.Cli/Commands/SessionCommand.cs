using System.Text.Json;
using Spectre.Console;
using Unityctl.Cli.Output;
using Unityctl.Core.Sessions;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

public static class SessionCommand
{
    public static void List(bool json = false)
        => ListCore(new SessionManager(new NdjsonSessionStore()), json);

    public static void Stop(string id, bool json = false)
        => StopCore(new SessionManager(new NdjsonSessionStore()), id, json);

    public static void Clean()
        => CleanCore(new SessionManager(new NdjsonSessionStore()));

    internal static void ListCore(SessionManager manager, bool json)
    {
        var sessions = manager.ListAsync().GetAwaiter().GetResult();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(
                sessions.ToArray(),
                UnityctlJsonContext.Default.SessionArray));
            return;
        }

        if (sessions.Count == 0)
        {
            Console.WriteLine("No active sessions.");
            return;
        }

        var console = ConsoleOutput.CreateOut();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("State")
            .AddColumn("Command")
            .AddColumn("Project")
            .AddColumn("Created");

        foreach (var s in sessions)
        {
            var shortId = s.Id.Length > 8 ? s.Id[..8] : s.Id;
            var project = s.ProjectPath.Length > 28
                ? ".." + s.ProjectPath[^26..]
                : s.ProjectPath;
            var stateColor = s.State switch
            {
                SessionState.Running => "green",
                SessionState.Failed => "red",
                SessionState.Completed => "cyan",
                SessionState.Cancelled => "yellow",
                SessionState.TimedOut => "red",
                _ => "white"
            };

            table.AddRow(
                new Text(shortId),
                new Markup($"[{stateColor}]{s.State}[/]"),
                new Text(s.Command),
                new Text(project),
                new Text(s.CreatedAt));
        }

        console.Write(table);
    }

    internal static void StopCore(SessionManager manager, string id, bool json)
    {
        try
        {
            manager.CancelAsync(id).GetAwaiter().GetResult();

            if (json)
                Console.WriteLine($"{{\"success\":true,\"id\":\"{id}\"}}");
            else
                Console.WriteLine(
                    $"Session {id} cancelled. (Unity process may still be running)");
        }
        catch (InvalidOperationException ex)
        {
            if (json)
                Console.WriteLine($"{{\"success\":false,\"error\":\"{ex.Message}\"}}");
            else
                Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    internal static void CleanCore(SessionManager manager)
    {
        var count = manager.CleanStaleAsync().GetAwaiter().GetResult();
        Console.WriteLine($"Cleaned {count} stale session(s).");
    }
}
