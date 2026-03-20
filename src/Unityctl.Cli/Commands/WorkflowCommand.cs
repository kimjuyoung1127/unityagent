using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Core.Verification;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Executes a sequential workflow of unityctl commands from a JSON definition file.
/// Supports continueOnError for fault-tolerant batch execution.
/// </summary>
public static class WorkflowCommand
{
    public static void Verify(
        string file,
        string project,
        string? artifactsDir = null,
        bool inlineEvidence = false,
        bool json = false)
    {
        var exitCode = VerifyAsync(file, project, artifactsDir, inlineEvidence, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    public static void Run(string file, string? project = null, bool json = false)
    {
        var exitCode = RunAsync(file, project, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> VerifyAsync(
        string file,
        string project,
        string? artifactsDir,
        bool inlineEvidence,
        bool json)
    {
        VerificationDefinition? definition;
        try
        {
            var content = await File.ReadAllTextAsync(file);
            definition = JsonSerializer.Deserialize(content, UnityctlJsonContext.Default.VerificationDefinition);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: Verification file not found: {file}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading verification file: {ex.Message}");
            return 1;
        }

        if (definition == null || definition.Steps.Length == 0)
        {
            Console.Error.WriteLine("Error: Verification file is empty or invalid.");
            return 1;
        }

        var resolvedArtifactsDir = ResolveArtifactsDirectory(artifactsDir, definition.Name);
        Directory.CreateDirectory(resolvedArtifactsDir);

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);
        var diffEngine = new ImageDiffEngine();
        var artifacts = new List<VerificationArtifact>();
        var stepResults = new List<VerificationStepResult>();
        var captureArtifacts = new Dictionary<string, VerificationArtifact>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < definition.Steps.Length; i++)
        {
            var step = definition.Steps[i];
            var stepId = string.IsNullOrWhiteSpace(step.Id) ? $"{step.Kind}-{i + 1}" : step.Id!;

            switch (step.Kind)
            {
                case "projectValidate":
                    {
                        var response = await executor.ExecuteAsync(project, ProjectValidateCommand.CreateRequest());
                        stepResults.Add(new VerificationStepResult
                        {
                            Id = stepId,
                            Kind = step.Kind,
                            Passed = response.Success,
                            Message = response.Message ?? "project validate completed",
                            Data = response.Data
                        });
                        break;
                    }

                case "capture":
                    {
                        var view = step.View ?? "scene";
                        var format = step.Format ?? "png";
                        var extension = string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                        var artifactPath = Path.Combine(resolvedArtifactsDir, $"{stepId}.{extension}");
                        var request = ScreenshotCommand.CreateCaptureRequest(
                            view,
                            step.Width ?? 1920,
                            step.Height ?? 1080,
                            format,
                            quality: 75,
                            output: artifactPath);
                        var response = await executor.ExecuteAsync(project, request);
                        if (!response.Success || !File.Exists(artifactPath))
                        {
                            stepResults.Add(new VerificationStepResult
                            {
                                Id = stepId,
                                Kind = step.Kind,
                                Passed = false,
                                Message = response.Message ?? "capture failed",
                                Data = response.Data
                            });
                            break;
                        }

                        var artifact = BuildArtifact(stepId, "capture", artifactPath, inlineEvidence ? response.Data?["base64"]?.GetValue<string>() : null, new JsonObject
                        {
                            ["view"] = view,
                            ["format"] = format,
                            ["width"] = step.Width ?? 1920,
                            ["height"] = step.Height ?? 1080
                        });
                        artifacts.Add(artifact);
                        captureArtifacts[stepId] = artifact;
                        stepResults.Add(new VerificationStepResult
                        {
                            Id = stepId,
                            Kind = step.Kind,
                            Passed = response.Success,
                            Message = response.Message ?? "capture completed",
                            Data = new JsonObject { ["artifactId"] = artifact.Id, ["path"] = artifact.Path }
                        });
                        break;
                    }

                case "imageDiff":
                    {
                        if (string.IsNullOrWhiteSpace(step.Baseline) || string.IsNullOrWhiteSpace(step.Candidate))
                        {
                            stepResults.Add(new VerificationStepResult
                            {
                                Id = stepId,
                                Kind = step.Kind,
                                Passed = false,
                                Message = "imageDiff requires baseline and candidate capture ids."
                            });
                            break;
                        }

                        if (!captureArtifacts.TryGetValue(step.Baseline, out var baselineArtifact)
                            || !captureArtifacts.TryGetValue(step.Candidate, out var candidateArtifact))
                        {
                            stepResults.Add(new VerificationStepResult
                            {
                                Id = stepId,
                                Kind = step.Kind,
                                Passed = false,
                                Message = "Referenced capture artifact not found."
                            });
                            break;
                        }

                        var diffImagePath = Path.Combine(resolvedArtifactsDir, $"{stepId}-diff.png");
                        var diffResult = diffEngine.Diff(
                            baselineArtifact.Path,
                            candidateArtifact.Path,
                            diffImagePath,
                            step.MaxChangedPixelRatio ?? 0.0);
                        var diffArtifact = BuildArtifact(stepId, "imageDiff", diffImagePath, inlineEvidence ? Convert.ToBase64String(await File.ReadAllBytesAsync(diffImagePath)) : null, diffResult.ToJson());
                        artifacts.Add(diffArtifact);
                        stepResults.Add(new VerificationStepResult
                        {
                            Id = stepId,
                            Kind = step.Kind,
                            Passed = diffResult.Passed,
                            Message = diffResult.Message,
                            Data = diffResult.ToJson()
                        });
                        break;
                    }

                case "consoleWatch":
                    {
                        var durationSeconds = step.DurationSeconds ?? 2;
                        var events = await CollectConsoleEventsAsync(executor, project, durationSeconds);
                        var artifactPath = Path.Combine(resolvedArtifactsDir, $"{stepId}.json");
                        await File.WriteAllTextAsync(
                            artifactPath,
                            JsonSerializer.Serialize(events.ToArray(), UnityctlJsonContext.Default.EventEnvelopeArray));
                        var artifact = BuildArtifact(stepId, "consoleWatch", artifactPath, null, new JsonObject
                        {
                            ["durationSeconds"] = durationSeconds,
                            ["eventCount"] = events.Count
                        });
                        artifacts.Add(artifact);
                        stepResults.Add(new VerificationStepResult
                        {
                            Id = stepId,
                            Kind = step.Kind,
                            Passed = true,
                            Message = $"Collected {events.Count} console events.",
                            Data = new JsonObject { ["eventCount"] = events.Count, ["artifactId"] = artifact.Id }
                        });
                        break;
                    }

                case "uiAssert":
                    {
                        if (string.IsNullOrWhiteSpace(step.TargetId)
                            || string.IsNullOrWhiteSpace(step.Field)
                            || step.Expected == null)
                        {
                            stepResults.Add(new VerificationStepResult
                            {
                                Id = stepId,
                                Kind = step.Kind,
                                Passed = false,
                                Message = "uiAssert requires targetId, field, and expected."
                            });
                            break;
                        }

                        var response = await executor.ExecuteAsync(project, UiCommand.CreateGetRequest(step.TargetId));
                        if (!response.Success || response.Data == null)
                        {
                            stepResults.Add(new VerificationStepResult
                            {
                                Id = stepId,
                                Kind = step.Kind,
                                Passed = false,
                                Message = response.Message ?? "ui get failed",
                                Data = response.Data
                            });
                            break;
                        }

                        var actualNode = TryReadNode(response.Data, step.Field);
                        var passedAssert = actualNode != null && JsonNodesEqual(actualNode, step.Expected);
                        var artifactPath = Path.Combine(resolvedArtifactsDir, $"{stepId}.json");
                        await File.WriteAllTextAsync(
                            artifactPath,
                            JsonSerializer.Serialize(response.Data, UnityctlJsonContext.Default.JsonObject));
                        var artifact = BuildArtifact(stepId, "uiAssert", artifactPath, null, new JsonObject
                        {
                            ["field"] = step.Field,
                            ["expected"] = step.Expected?.DeepClone(),
                            ["actual"] = actualNode?.DeepClone()
                        });
                        artifacts.Add(artifact);
                        stepResults.Add(new VerificationStepResult
                        {
                            Id = stepId,
                            Kind = step.Kind,
                            Passed = passedAssert,
                            Message = passedAssert
                                ? $"UI assertion passed for '{step.Field}'."
                                : $"UI assertion failed for '{step.Field}'.",
                            Data = new JsonObject
                            {
                                ["field"] = step.Field,
                                ["expected"] = step.Expected?.DeepClone(),
                                ["actual"] = actualNode?.DeepClone(),
                                ["artifactId"] = artifact.Id
                            }
                        });
                        break;
                    }

                case "playSmoke":
                    {
                        var settleTimeoutSeconds = step.SettleTimeoutSeconds ?? 10;
                        var watchDurationSeconds = step.DurationSeconds ?? 2;
                        var playStartResponse = await executor.ExecuteAsync(project, PlayModeCommand.CreateRequest("start"));

                        if (!playStartResponse.Success)
                        {
                            stepResults.Add(new VerificationStepResult
                            {
                                Id = stepId,
                                Kind = step.Kind,
                                Passed = false,
                                Message = playStartResponse.Message ?? "play start failed",
                                Data = playStartResponse.Data
                            });
                            break;
                        }

                        var settled = await WaitForPlayModeAsync(
                            project,
                            settleTimeoutSeconds,
                            (proj, request, ct) => executor.ExecuteAsync(proj, request, ct: ct));
                        var consoleEvents = await CollectConsoleEventsAsync(executor, project, watchDurationSeconds);
                        VerificationArtifact? playCaptureArtifact = null;
                        if (settled)
                        {
                            var capturePath = Path.Combine(resolvedArtifactsDir, $"{stepId}-game.png");
                            var captureResponse = await executor.ExecuteAsync(
                                project,
                                ScreenshotCommand.CreateCaptureRequest(
                                    view: "game",
                                    width: 1280,
                                    height: 720,
                                    format: "png",
                                    quality: 75,
                                    output: capturePath));
                            if (captureResponse.Success && File.Exists(capturePath))
                            {
                                playCaptureArtifact = BuildArtifact(stepId + "-game", "playCapture", capturePath, null, new JsonObject
                                {
                                    ["mode"] = "game-camera"
                                });
                                artifacts.Add(playCaptureArtifact);
                            }
                        }
                        var stopResponse = await executor.ExecuteAsync(project, PlayModeCommand.CreateRequest("stop"));

                        var artifactPath = Path.Combine(resolvedArtifactsDir, $"{stepId}.json");
                        var artifactPayload = new JsonObject
                        {
                            ["settled"] = settled,
                            ["settleTimeoutSeconds"] = settleTimeoutSeconds,
                            ["watchDurationSeconds"] = watchDurationSeconds,
                            ["eventCount"] = consoleEvents.Count,
                            ["stopSuccess"] = stopResponse.Success,
                            ["captureArtifactId"] = playCaptureArtifact?.Id
                        };
                        await File.WriteAllTextAsync(
                            artifactPath,
                            JsonSerializer.Serialize(new JsonObject
                            {
                                ["summary"] = artifactPayload,
                                ["events"] = JsonSerializer.SerializeToNode(consoleEvents.ToArray(), UnityctlJsonContext.Default.EventEnvelopeArray)
                            }, UnityctlJsonContext.Default.JsonObject));

                        var artifact = BuildArtifact(stepId, "playSmoke", artifactPath, null, artifactPayload);
                        artifacts.Add(artifact);
                        stepResults.Add(new VerificationStepResult
                        {
                            Id = stepId,
                            Kind = step.Kind,
                            Passed = settled && stopResponse.Success,
                            Message = settled
                                ? $"Play smoke passed with {consoleEvents.Count} console events."
                                : "Play smoke failed to settle into Play Mode before timeout.",
                            Data = new JsonObject
                            {
                                ["settled"] = settled,
                                ["eventCount"] = consoleEvents.Count,
                                ["stopSuccess"] = stopResponse.Success,
                                ["captureArtifactId"] = playCaptureArtifact?.Id,
                                ["artifactId"] = artifact.Id
                            }
                        });
                        break;
                    }

                default:
                    stepResults.Add(new VerificationStepResult
                    {
                        Id = stepId,
                        Kind = step.Kind,
                        Passed = false,
                        Message = $"Unsupported verification step kind: {step.Kind}"
                    });
                    break;
            }
        }

        var passed = stepResults.All(step => step.Passed);
        var result = new VerificationResult
        {
            Passed = passed,
            Summary = passed
                ? $"Verification passed ({stepResults.Count} steps)."
                : $"Verification failed ({stepResults.Count(step => !step.Passed)} failing step(s)).",
            ArtifactsDirectory = resolvedArtifactsDir,
            Steps = stepResults.ToArray(),
            Artifacts = artifacts.ToArray()
        };

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(result, UnityctlJsonContext.Default.VerificationResult));
        }
        else
        {
            PrintVerificationResult(result);
        }

        return result.Passed ? 0 : 1;
    }

    internal static async Task<int> RunAsync(string file, string? project, bool json)
    {
        WorkflowDefinition? workflow;
        try
        {
            var content = await File.ReadAllTextAsync(file);
            workflow = JsonSerializer.Deserialize(content, UnityctlJsonContext.Default.WorkflowDefinition);
        }
        catch (FileNotFoundException)
        {
            Console.Error.WriteLine($"Error: Workflow file not found: {file}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading workflow file: {ex.Message}");
            return 1;
        }

        if (workflow == null || workflow.Steps.Length == 0)
        {
            Console.Error.WriteLine("Error: Workflow file is empty or invalid.");
            return 1;
        }

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var results = new List<CommandResponse>();
        var anyFailed = false;

        foreach (var step in workflow.Steps)
        {
            var stepProject = step.Project ?? project;
            if (string.IsNullOrWhiteSpace(stepProject))
            {
                Console.Error.WriteLine(
                    $"Error: Step '{step.Command}' has no project path. " +
                    "Provide --project or set 'project' in the step definition.");
                if (!workflow.ContinueOnError) return 1;
                anyFailed = true;
                continue;
            }

            var request = new CommandRequest
            {
                Command = step.Command,
                Parameters = step.Parameters
            };

            CancellationToken ct = default;
            if (step.TimeoutSeconds.HasValue)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(step.TimeoutSeconds.Value));
                ct = cts.Token;
            }

            CommandResponse response;
            try
            {
                response = await executor.ExecuteAsync(stepProject, request, ct: ct);
            }
            catch (OperationCanceledException)
            {
                response = CommandResponse.Fail(
                    StatusCode.UnknownError,
                    $"Step '{step.Command}' timed out after {step.TimeoutSeconds}s.");
            }

            results.Add(response);

            if (!json)
                CommandRunner.PrintResponse(response, json: false);

            if (!response.Success)
            {
                anyFailed = true;
                if (!workflow.ContinueOnError)
                    break;
            }
        }

        if (json)
        {
            Console.WriteLine("[");
            for (var i = 0; i < results.Count; i++)
            {
                var suffix = i < results.Count - 1 ? "," : string.Empty;
                Console.WriteLine(
                    JsonSerializer.Serialize(results[i], UnityctlJsonContext.Default.CommandResponse) + suffix);
            }
            Console.WriteLine("]");
        }

        return anyFailed ? 1 : 0;
    }

    internal static string ResolveArtifactsDirectory(string? artifactsDir, string workflowName)
    {
        if (!string.IsNullOrWhiteSpace(artifactsDir))
            return Path.GetFullPath(artifactsDir);

        var safeName = string.IsNullOrWhiteSpace(workflowName) ? "verify" : workflowName.Replace(' ', '-');
        return Path.Combine(
            Unityctl.Shared.Constants.GetConfigDirectory(),
            "verification",
            $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{safeName}");
    }

    internal static VerificationArtifact BuildArtifact(
        string id,
        string kind,
        string path,
        string? inlineBase64,
        JsonObject? metadata)
    {
        return new VerificationArtifact
        {
            Id = id,
            Kind = kind,
            Path = path,
            Sha256 = ComputeSha256(path),
            InlineBase64 = inlineBase64,
            Metadata = metadata
        };
    }

    internal static async Task<List<EventEnvelope>> CollectConsoleEventsAsync(
        CommandExecutor executor,
        string project,
        int durationSeconds)
    {
        var events = new List<EventEnvelope>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
        var stream = executor.WatchAsync(project, "console", cts.Token);
        if (stream == null)
            return events;

        try
        {
            await foreach (var evt in stream.WithCancellation(cts.Token))
                events.Add(evt);
        }
        catch (OperationCanceledException)
        {
            // normal timeout path
        }

        return events;
    }

    internal static async Task<bool> WaitForPlayModeAsync(
        string project,
        int settleTimeoutSeconds,
        Func<string, CommandRequest, CancellationToken, Task<CommandResponse>> executor,
        CancellationToken ct = default)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(settleTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        while (!linkedCts.IsCancellationRequested)
        {
            var response = await executor(project, new CommandRequest { Command = WellKnownCommands.Status }, linkedCts.Token);
            if (response.Success
                && response.Data?["isPlaying"]?.GetValue<bool>() == true)
            {
                return true;
            }

            await Task.Delay(250, linkedCts.Token);
        }

        return false;
    }

    internal static JsonNode? TryReadNode(JsonNode root, string fieldPath)
    {
        var current = root;
        foreach (var segment in fieldPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current == null)
                return null;

            if (current is JsonObject obj)
            {
                current = obj[segment];
                continue;
            }

            if (current is JsonArray array && int.TryParse(segment, out var index))
            {
                current = index >= 0 && index < array.Count ? array[index] : null;
                continue;
            }

            return null;
        }

        return current;
    }

    internal static bool JsonNodesEqual(JsonNode? left, JsonNode? right)
    {
        if (left == null && right == null)
            return true;
        if (left == null || right == null)
            return false;

        return left.ToJsonString() == right.ToJsonString();
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }

    private static void PrintVerificationResult(VerificationResult result)
    {
        Console.WriteLine(result.Summary);
        Console.WriteLine($"Artifacts: {result.ArtifactsDirectory}");
        foreach (var step in result.Steps)
            Console.WriteLine($"- [{(step.Passed ? "PASS" : "FAIL")}] {step.Id} ({step.Kind}) — {step.Message}");
    }
}
