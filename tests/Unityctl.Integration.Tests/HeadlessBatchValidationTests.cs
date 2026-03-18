using System.Text.Json;
using Unityctl.Integration.Tests.Support;
using Unityctl.Shared.Protocol;
using Xunit;
using Xunit.Sdk;

namespace Unityctl.Integration.Tests;

[CollectionDefinition(nameof(UnityctlHeadlessCollection), DisableParallelization = true)]
public sealed class UnityctlHeadlessCollection;

[Collection(nameof(UnityctlHeadlessCollection))]
public sealed class HeadlessBatchValidationTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

    [Fact]
    public async Task Status_ClosedEditor_ReturnsStructuredResponseOrKnownFailure()
    {
        var response = await ExecuteJsonCommandAsync("status", "--project", TestEnvironment.SampleUnityProjectRoot, "--json");

        if (response.Success)
        {
            Assert.Equal(StatusCode.Ready, response.StatusCode);
            var data = JsonResponseExtensions.RequireDataElement(response, "projectPath");
            Assert.Equal(JsonValueKind.String, data.ValueKind);
            var normalizedPath = (data.GetString() ?? string.Empty).Replace('/', Path.DirectorySeparatorChar);
            Assert.EndsWith(Path.Combine("SampleUnityProject", "Assets"), normalizedPath);

            var payload = JsonResponseExtensions.RequireFullData(response);
            Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("unityVersion").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("platform").GetString()));
            return;
        }

        Assert.Equal(StatusCode.UnknownError, response.StatusCode);
        Assert.Contains("no response file", response.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Check_ClosedEditor_ReturnsStructuredResponse()
    {
        var response = await ExecuteJsonCommandAsync("check", "--project", TestEnvironment.SampleUnityProjectRoot, "--json");

        Assert.True(response.Success, response.Message);
        Assert.Equal(StatusCode.Ready, response.StatusCode);

        var data = JsonResponseExtensions.RequireFullData(response);
        Assert.True(data.GetProperty("assemblies").GetInt32() > 0);
        Assert.False(data.GetProperty("scriptCompilationFailed").GetBoolean());
    }

    [Fact]
    public async Task TestEditMode_ClosedEditor_ReturnsStructuredResponseOrKnownFailure()
    {
        var response = await ExecuteJsonCommandAsync("test", "--project", TestEnvironment.SampleUnityProjectRoot, "--mode", "edit", "--json");

        if (response.Success)
        {
            Assert.True(response.StatusCode is StatusCode.Ready or StatusCode.Accepted, $"Unexpected status: {response.StatusCode}");
            var payload = JsonResponseExtensions.RequireFullData(response);
            Assert.True(payload.GetProperty("total").GetInt32() >= 0);
            Assert.Equal(0, payload.GetProperty("failed").GetInt32());
            return;
        }

        Assert.Equal(StatusCode.UnknownError, response.StatusCode);
        Assert.Contains("no response file", response.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildDryRun_ClosedEditor_ReturnsStructuredResponse()
    {
        var response = await ExecuteJsonCommandAsync(
            "build",
            "--project", TestEnvironment.SampleUnityProjectRoot,
            "--dry-run",
            "--json");

        Assert.True(
            response.StatusCode is StatusCode.Ready or StatusCode.BuildFailed,
            $"Unexpected status: {response.StatusCode} / {response.Message}");

        var checksElement = JsonResponseExtensions.RequireDataElement(response, "checks");
        Assert.Equal(JsonValueKind.Array, checksElement.ValueKind);

        var checks = checksElement.EnumerateArray().ToArray();
        Assert.True(checks.Length > 0, "Expected at least one preflight check.");
        Assert.Contains(checks, c => c.GetProperty("check").GetString() == "ScenesExist");

        if (response.Success)
        {
            Assert.DoesNotContain(checks, c =>
                c.GetProperty("category").GetString() == "error" &&
                !c.GetProperty("passed").GetBoolean());
            return;
        }

        Assert.Contains(checks, c =>
            c.GetProperty("category").GetString() == "error" &&
            !c.GetProperty("passed").GetBoolean());
    }

    private static async Task<CommandResponse> ExecuteJsonCommandAsync(params string[] args)
    {
        TestEnvironment.EnsureCliCanRun();
        TestEnvironment.EnsureSampleProjectReady();

        var fullArgs = args.ToList();
        if (!fullArgs.Contains("--json", StringComparer.Ordinal))
            fullArgs.Add("--json");

        var (exitCode, stdout, stderr) = await TestEnvironment.RunCliAsync(DefaultTimeout, fullArgs.ToArray());
        if (string.IsNullOrWhiteSpace(stdout))
            throw SkipException.ForSkip($"SKIPPED: CLI produced no stdout for {string.Join(' ', args)}. stderr: {stderr}");

        var response = JsonResponseExtensions.ParseCommandResponse(stdout, string.Join(' ', args));
        JsonResponseExtensions.SkipIfEnvironmentUnavailable(response, string.Join(' ', args));

        return response;
    }
}
