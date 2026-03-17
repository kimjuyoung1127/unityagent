using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Infrastructure;

/// <summary>
/// Retry policy based on StatusCode classification.
/// Transient (1xx): retry with exponential backoff.
/// Fatal (2xx): fail immediately with recovery guidance.
/// Error (5xx): fail with debug info.
/// </summary>
public sealed class RetryPolicy
{
    public int MaxRetries { get; init; } = 3;
    public int BaseDelayMs { get; init; } = 2000;
    public int MaxDelayMs { get; init; } = 30000;

    public static bool IsTransient(StatusCode code) => (int)code >= 100 && (int)code < 200;
    public static bool IsFatal(StatusCode code) => (int)code >= 200 && (int)code < 500;
    public static bool IsError(StatusCode code) => (int)code >= 500;

    public int GetDelayMs(int attempt)
    {
        var delay = BaseDelayMs * (int)Math.Pow(2, attempt);
        return Math.Min(delay, MaxDelayMs);
    }

    public async Task<CommandResponse> ExecuteWithRetryAsync(
        Func<Task<CommandResponse>> action,
        CancellationToken ct = default)
    {
        CommandResponse? lastResponse = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            lastResponse = await action();

            if (lastResponse.Success)
                return lastResponse;

            if (!IsTransient(lastResponse.StatusCode))
                return lastResponse;

            if (attempt < MaxRetries)
            {
                var delay = GetDelayMs(attempt);
                Console.Error.WriteLine(
                    $"[unityctl] Status: {lastResponse.StatusCode} — retrying in {delay / 1000}s ({attempt + 1}/{MaxRetries})");
                await Task.Delay(delay, ct);
            }
        }

        return lastResponse!;
    }
}
