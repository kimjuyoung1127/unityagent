using Unityctl.Cli.Infrastructure;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class RetryPolicyTests
{
    [Theory]
    [InlineData(StatusCode.Compiling, true)]
    [InlineData(StatusCode.Reloading, true)]
    [InlineData(StatusCode.Busy, true)]
    [InlineData(StatusCode.Ready, false)]
    [InlineData(StatusCode.NotFound, false)]
    [InlineData(StatusCode.BuildFailed, false)]
    public void IsTransient_ClassifiesCorrectly(StatusCode code, bool expected)
    {
        Assert.Equal(expected, RetryPolicy.IsTransient(code));
    }

    [Theory]
    [InlineData(StatusCode.NotFound, true)]
    [InlineData(StatusCode.ProjectLocked, true)]
    [InlineData(StatusCode.PluginNotInstalled, true)]
    [InlineData(StatusCode.Ready, false)]
    [InlineData(StatusCode.BuildFailed, false)]
    public void IsFatal_ClassifiesCorrectly(StatusCode code, bool expected)
    {
        Assert.Equal(expected, RetryPolicy.IsFatal(code));
    }

    [Fact]
    public void GetDelayMs_ExponentialBackoff()
    {
        var policy = new RetryPolicy { BaseDelayMs = 1000, MaxDelayMs = 16000 };

        Assert.Equal(1000, policy.GetDelayMs(0));
        Assert.Equal(2000, policy.GetDelayMs(1));
        Assert.Equal(4000, policy.GetDelayMs(2));
        Assert.Equal(8000, policy.GetDelayMs(3));
        Assert.Equal(16000, policy.GetDelayMs(4)); // capped
        Assert.Equal(16000, policy.GetDelayMs(5)); // capped
    }

    [Fact]
    public async Task ExecuteWithRetry_SuccessOnFirstTry()
    {
        var policy = new RetryPolicy();
        var callCount = 0;

        var result = await policy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(CommandResponse.Ok("done"));
        });

        Assert.True(result.Success);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetry_RetriesOnTransient()
    {
        var policy = new RetryPolicy { MaxRetries = 2, BaseDelayMs = 10 };
        var callCount = 0;

        var result = await policy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            if (callCount < 3)
                return Task.FromResult(CommandResponse.Fail(StatusCode.Compiling, "still compiling"));
            return Task.FromResult(CommandResponse.Ok("ready"));
        });

        Assert.True(result.Success);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetry_StopsOnFatal()
    {
        var policy = new RetryPolicy { MaxRetries = 3, BaseDelayMs = 10 };
        var callCount = 0;

        var result = await policy.ExecuteWithRetryAsync(() =>
        {
            callCount++;
            return Task.FromResult(CommandResponse.Fail(StatusCode.NotFound, "not found"));
        });

        Assert.False(result.Success);
        Assert.Equal(1, callCount); // No retries for fatal
    }
}
