using ConsoleAppFramework;
using Unityctl.Cli.Commands;

var app = ConsoleApp.Create();

app.Add("", () => Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — A deterministic control plane for Unity\nUse --help for available commands."));

app.Add("init", (string project, string? source = null) =>
    InitCommand.Execute(project, source));

app.Add("editor list", (bool json = false) =>
    EditorCommands.List(json));

app.Add("ping", (string project, bool json = false) =>
    PingCommand.Execute(project, json));

app.Add("status", (string project, bool wait = false, bool json = false) =>
    StatusCommand.Execute(project, wait, json));

app.Add("build", (string project, string target = "StandaloneWindows64", string? output = null, bool json = false) =>
    BuildCommand.Execute(project, target, output, json));

app.Add("test", (string project, string mode = "edit", string? filter = null, bool noWait = false, int timeout = 300, bool json = false) =>
    TestCommand.Execute(project, mode, filter, !noWait, timeout, json));

app.Add("check", (string project, string type = "compile", bool json = false) =>
    CheckCommand.Execute(project, type, json));

app.Add("tools", (bool json = false) =>
    ToolsCommand.Execute(json));

app.Add("log", (
        int? last = null,
        bool tail = false,
        string? op = null,
        string? level = null,
        string? since = null,
        bool json = false,
        bool prune = false,
        bool stats = false) =>
    LogCommand.Execute(last, tail, op, level, since, json, prune, stats));

app.Run(args);
