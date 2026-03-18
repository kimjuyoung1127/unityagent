using System.Text.Json;
using System.Text.Json.Nodes;
using Spectre.Console;
using Spectre.Console.Rendering;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Output;

public static class ConsoleOutput
{
    /// <summary>
    /// Creates an IAnsiConsole bound to Console.Out (follows Console.SetOut redirection for tests).
    /// </summary>
    internal static IAnsiConsole CreateOut() => AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Out)
    });

    /// <summary>
    /// Creates an IAnsiConsole bound to Console.Error for hints/warnings.
    /// </summary>
    internal static IAnsiConsole CreateErr() => AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error)
    });

    public static void PrintResponse(CommandResponse response)
    {
        var console = CreateOut();

        if (response.StatusCode == StatusCode.Accepted)
        {
            console.MarkupLine("[cyan]ACCEPTED [104][/]" + FormatMessage(response.Message));
        }
        else if (response.Success)
        {
            console.MarkupLine("[green]OK[/]" + FormatMessage(response.Message));
        }
        else
        {
            console.MarkupLine($"[red]FAIL [{response.StatusCode}][/]" + FormatMessage(response.Message));
        }

        if (response.Data != null)
        {
            if (response.Data["checks"] is JsonArray checksArray)
            {
                PrintPreflightChecks(checksArray, console);
            }
            else
            {
                foreach (var prop in response.Data)
                {
                    console.MarkupLine($"  [grey]{Markup.Escape(prop.Key)}:[/] {Markup.Escape($"{prop.Value}")}");
                }
            }
        }

        if (response.Errors is { Count: > 0 })
        {
            var stderr = CreateErr();
            foreach (var error in response.Errors)
            {
                stderr.MarkupLine($"  [red]! {Markup.Escape(error)}[/]");
            }
        }
    }

    public static void PrintPreflightChecks(JsonArray checksArray) =>
        PrintPreflightChecks(checksArray, CreateOut());

    private static void PrintPreflightChecks(JsonArray checksArray, IAnsiConsole console)
    {
        var checks = JsonSerializer.Deserialize(checksArray, UnityctlJsonContext.Default.PreflightCheckArray);
        if (checks == null || checks.Length == 0) return;

        console.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("").Centered())
            .AddColumn("Category")
            .AddColumn("Check")
            .AddColumn("Message")
            .AddColumn("Details");

        foreach (var check in checks)
        {
            var (prefix, color) = GetCheckStyle(check);
            table.AddRow(
                new Markup($"[{color}]{prefix}[/]"),
                new Markup($"[{color}]{Markup.Escape(check.Category.ToUpperInvariant())}[/]"),
                new Text(check.Check),
                new Text(check.Message),
                new Text(check.Details ?? ""));
        }

        console.Write(table);

        var errors = checks.Count(c => c.Category == "error" && !c.Passed);
        var warnings = checks.Count(c => c.Category == "warning" && !c.Passed);
        var passed = checks.Count(c => c.Passed);

        var summary = $"  {checks.Length} checks: [green]{passed} passed[/]";
        if (errors > 0) summary += $", [red]{errors} errors[/]";
        if (warnings > 0) summary += $", [yellow]{warnings} warnings[/]";
        console.MarkupLine(summary);
    }

    public static void PrintRecovery(StatusCode code)
    {
        var hint = code switch
        {
            StatusCode.NotFound => "Tip: Is Unity installed? Run 'unityctl editor list' to check.",
            StatusCode.ProjectLocked => "Tip: Close the running Unity Editor or wait for it to finish.",
            StatusCode.LicenseError => "Tip: Activate your Unity license via Unity Hub.",
            StatusCode.PluginNotInstalled => "Tip: Run 'unityctl init --project <path>' to install the plugin.",
            _ => null
        };
        if (hint != null)
        {
            var stderr = CreateErr();
            stderr.MarkupLine($"[yellow]{Markup.Escape(hint)}[/]");
        }
    }

    private static string FormatMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "";
        return $" [dim]—[/] {Markup.Escape(message)}";
    }

    private static (string Prefix, string Color) GetCheckStyle(PreflightCheck check)
    {
        return (check.Category, check.Passed) switch
        {
            ("error", false) => ("\u2717", "red"),
            ("error", true) => ("\u2713", "green"),
            ("warning", false) => ("\u26a0", "yellow"),
            ("warning", true) => ("\u2713", "green"),
            ("info", _) => ("\u2139", "cyan"),
            _ => ("\u00b7", "grey")
        };
    }
}
