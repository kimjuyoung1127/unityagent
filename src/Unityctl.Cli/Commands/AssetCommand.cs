using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class AssetCommand
{
    public static void Refresh(string project, bool noWait = false, bool json = false)
    {
        var exitCode = ExecuteRefreshAsync(project, noWait, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteRefreshAsync(string project, bool noWait, bool json)
    {
        var request = CreateRefreshRequest();
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;
        if (noWait)
        {
            response = await executor.ExecuteAsync(project, request);
        }
        else
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.AssetRefreshResult,
                timeoutSeconds: 60,
                timeoutStatusCode: StatusCode.UnknownError,
                timeoutMessage: "Asset refresh timed out after 60s");
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    internal static CommandRequest CreateRefreshRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.AssetRefresh,
            Parameters = new JsonObject()
        };
    }

    public static void Create(string project, string path, string type, bool json = false)
    {
        var request = CreateCreateRequest(path, type);
        CommandRunner.Execute(project, request, json);
    }

    public static void CreateFolder(string project, string parent, string name, bool json = false)
    {
        var request = CreateCreateFolderRequest(parent, name);
        CommandRunner.Execute(project, request, json);
    }

    public static void Copy(string project, string source, string destination, bool json = false)
    {
        var request = CreateCopyRequest(source, destination);
        CommandRunner.Execute(project, request, json);
    }

    public static void Move(string project, string source, string destination, bool json = false)
    {
        var request = CreateMoveRequest(source, destination);
        CommandRunner.Execute(project, request, json);
    }

    public static void Delete(string project, string path, bool json = false)
    {
        var request = CreateDeleteRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void Import(string project, string path, string? options = null, bool json = false)
    {
        var request = CreateImportRequest(path, options);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCreateRequest(string path, string type)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("type must not be empty", nameof(type));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetCreate,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["type"] = type
            }
        };
    }

    internal static CommandRequest CreateCreateFolderRequest(string parent, string name)
    {
        if (string.IsNullOrWhiteSpace(parent))
            throw new ArgumentException("parent must not be empty", nameof(parent));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetCreateFolder,
            Parameters = new JsonObject
            {
                ["parent"] = parent,
                ["name"] = name
            }
        };
    }

    internal static CommandRequest CreateCopyRequest(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("source must not be empty", nameof(source));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("destination must not be empty", nameof(destination));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetCopy,
            Parameters = new JsonObject
            {
                ["source"] = source,
                ["destination"] = destination
            }
        };
    }

    internal static CommandRequest CreateMoveRequest(string source, string destination)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("source must not be empty", nameof(source));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("destination must not be empty", nameof(destination));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetMove,
            Parameters = new JsonObject
            {
                ["source"] = source,
                ["destination"] = destination
            }
        };
    }

    internal static CommandRequest CreateDeleteRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetDelete,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateImportRequest(string path, string? options)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject { ["path"] = path };
        if (!string.IsNullOrEmpty(options)) parameters["options"] = options;

        return new CommandRequest
        {
            Command = WellKnownCommands.AssetImport,
            Parameters = parameters
        };
    }
}
