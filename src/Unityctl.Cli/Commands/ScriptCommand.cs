using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ScriptCommand
{
    public static void Create(string project, string path, string className, string? ns = null, string baseType = "MonoBehaviour", bool json = false)
    {
        // CLI-side validation: filename must match className
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        if (!string.Equals(fileNameWithoutExt, className, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Error: filename '{fileNameWithoutExt}' does not match className '{className}'");
            Environment.Exit(1);
            return;
        }

        var request = CreateCreateRequest(path, className, ns, baseType);
        CommandRunner.Execute(project, request, json);
    }

    public static void Edit(string project, string path, string? content = null, string? contentFile = null, bool json = false)
    {
        // Exactly one of content or contentFile
        if (content == null && contentFile == null)
        {
            Console.Error.WriteLine("Error: exactly one of --content or --content-file is required");
            Environment.Exit(1);
            return;
        }
        if (content != null && contentFile != null)
        {
            Console.Error.WriteLine("Error: --content and --content-file are mutually exclusive");
            Environment.Exit(1);
            return;
        }

        // CLI reads contentFile and sends as content
        if (contentFile != null)
        {
            if (!File.Exists(contentFile))
            {
                Console.Error.WriteLine($"Error: content file not found: {contentFile}");
                Environment.Exit(1);
                return;
            }
            content = File.ReadAllText(contentFile);
        }

        // Check IPC 10MB limit
        if (content!.Length > 9 * 1024 * 1024) // leave margin for JSON envelope
        {
            Console.Error.WriteLine("Error: content exceeds maximum size (9MB)");
            Environment.Exit(1);
            return;
        }

        var request = CreateEditRequest(path, content);
        CommandRunner.Execute(project, request, json);
    }

    public static void Delete(string project, string path, bool json = false)
    {
        var request = CreateDeleteRequest(path);
        CommandRunner.Execute(project, request, json);
    }

    public static void Validate(string project, string? path = null, bool wait = true, int timeout = 300, bool json = false)
    {
        var exitCode = ValidateAsync(project, path, wait, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ValidateAsync(
        string project,
        string? path,
        bool wait,
        int timeout,
        bool json)
    {
        var request = CreateValidateRequest(path);

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;

        if (wait)
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.ScriptValidateResult,
                timeoutSeconds: timeout,
                timeoutStatusCode: StatusCode.BuildFailed,
                timeoutMessage: $"Script validation timed out after {timeout}s");
        }
        else
        {
            response = await executor.ExecuteAsync(project, request);
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    public static void List(string project, string? folder = null, string? filter = null, int? limit = null, bool json = false)
    {
        var request = CreateListRequest(folder, filter, limit);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest(string? folder = null, string? filter = null, int? limit = null)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(folder)) parameters["folder"] = folder;
        if (!string.IsNullOrWhiteSpace(filter)) parameters["filter"] = filter;
        if (limit.HasValue) parameters["limit"] = limit.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptList,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateCreateRequest(string path, string className, string? ns, string baseType)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (string.IsNullOrWhiteSpace(className))
            throw new ArgumentException("className must not be empty", nameof(className));

        var parameters = new JsonObject
        {
            ["path"] = path,
            ["className"] = className,
            ["baseType"] = baseType
        };
        if (!string.IsNullOrEmpty(ns)) parameters["namespace"] = ns;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptCreate,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateEditRequest(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptEdit,
            Parameters = new JsonObject
            {
                ["path"] = path,
                ["content"] = content
            }
        };
    }

    public static void Patch(string project, string path, int startLine, int deleteCount = 0, string? insertContent = null, string? insertContentFile = null, bool json = false)
    {
        if (insertContent != null && insertContentFile != null)
        {
            Console.Error.WriteLine("Error: --insert-content and --insert-content-file are mutually exclusive");
            Environment.Exit(1);
            return;
        }

        if (insertContentFile != null)
        {
            if (!File.Exists(insertContentFile))
            {
                Console.Error.WriteLine($"Error: insert content file not found: {insertContentFile}");
                Environment.Exit(1);
                return;
            }
            insertContent = File.ReadAllText(insertContentFile);
        }

        // CLI: unescape literal \n to real newlines for ergonomic multi-line input
        if (insertContent != null)
            insertContent = insertContent.Replace("\\n", "\n");

        var request = CreatePatchRequest(path, startLine, deleteCount, insertContent);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreatePatchRequest(string path, int startLine, int deleteCount, string? insertContent)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        var parameters = new JsonObject
        {
            ["path"] = path,
            ["startLine"] = startLine,
            ["deleteCount"] = deleteCount
        };
        if (insertContent != null) parameters["insertContent"] = insertContent;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptPatch,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateDeleteRequest(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path must not be empty", nameof(path));

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptDelete,
            Parameters = new JsonObject { ["path"] = path }
        };
    }

    internal static CommandRequest CreateValidateRequest(string? path)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrEmpty(path)) parameters["path"] = path;

        return new CommandRequest
        {
            Command = WellKnownCommands.ScriptValidate,
            Parameters = parameters
        };
    }
}
