using ConsoleAppFramework;
using Unityctl.Cli.Commands;

var app = ConsoleApp.Create();

app.Add("", () => Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — Unity CLI for agentic workflows\nUse --help for available commands."));

app.Add("init", (string project, string? source = null) =>
    InitCommand.Execute(project, source));

app.Add("editor list", (bool json = false) =>
    EditorCommands.List(json));

app.Add("status", (string project, bool wait = false, bool json = false) =>
    StatusCommand.Execute(project, wait, json));

app.Add("build", (string project, string target = "StandaloneWindows64", string? output = null, bool json = false) =>
    BuildCommand.Execute(project, target, output, json));

app.Add("test", (string project, string mode = "edit", string? filter = null, bool json = false) =>
    TestCommand.Execute(project, mode, filter, json));

app.Add("check", (string project, string type = "compile", bool json = false) =>
    CheckCommand.Execute(project, type, json));

app.Run(args);
