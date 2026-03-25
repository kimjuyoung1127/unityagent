using System.Text.Json.Nodes;
using Unityctl.Cli.Output;
using Unityctl.Shared.Commands;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

internal static class CliCommandSuggestions
{
    private static readonly string[] KnownCommands = CommandCatalog.All
        .Select(command => command.CliName ?? command.Name)
        .Concat([
            "play start",
            "play stop",
            "play pause"
        ])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(command => command, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static readonly int[] KnownTokenCounts = KnownCommands
        .Select(command => command.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
        .Distinct()
        .OrderByDescending(length => length)
        .ToArray();

    public static bool TryHandleUnknownCommand(string[] args)
    {
        if (!TryBuildUnknownCommandResponse(args, out var response, out var json))
            return false;

        if (json)
        {
            JsonOutput.PrintResponse(response);
        }
        else
        {
            Console.Error.WriteLine(response.Message);
            Console.Error.WriteLine("Use `unityctl --help` to see available commands.");
        }

        Environment.ExitCode = 1;
        return true;
    }

    internal static bool TryBuildUnknownCommandResponse(string[] args, out CommandResponse response, out bool json)
    {
        json = args.Any(arg => string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase));
        response = CommandResponse.Fail(StatusCode.CommandNotFound, "Unknown command.");

        if (args.Length == 0)
            return false;

        var leadingTokens = args
            .TakeWhile(arg => !arg.StartsWith("-", StringComparison.Ordinal))
            .ToArray();

        if (leadingTokens.Length == 0)
            return false;

        foreach (var tokenCount in KnownTokenCounts.Where(tokenCount => tokenCount <= leadingTokens.Length))
        {
            var candidate = string.Join(" ", leadingTokens.Take(tokenCount));
            if (KnownCommands.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                return false;
        }

        var candidateLength = Math.Min(
            leadingTokens.Length,
            KnownTokenCounts.Length == 0 ? 1 : KnownTokenCounts[0]);
        var requestedCommand = string.Join(" ", leadingTokens.Take(candidateLength));
        var suggestedCommand = FindNearestCommand(requestedCommand);

        var message = suggestedCommand == null
            ? $"Unknown command: {requestedCommand}"
            : $"Unknown command: {requestedCommand}. Did you mean `{suggestedCommand}`?";

        response = CommandResponse.Fail(StatusCode.CommandNotFound, message);
        response.Data = new JsonObject
        {
            ["requestedCommand"] = requestedCommand,
            ["suggestedCommand"] = suggestedCommand,
            ["recommendedNextCommand"] = "unityctl --help"
        };

        return true;
    }

    private static string? FindNearestCommand(string requestedCommand)
    {
        if (string.IsNullOrWhiteSpace(requestedCommand))
            return null;

        var requestedTokens = requestedCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<string> candidates = KnownCommands;
        if (requestedTokens.Length > 0)
        {
            var sameGroup = KnownCommands
                .Where(command => command.StartsWith(requestedTokens[0] + " ", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (sameGroup.Length > 0)
                candidates = sameGroup;
        }

        var bestCommand = default(string);
        var bestDistance = int.MaxValue;
        var bestPrefixScore = -1;

        foreach (var candidate in candidates)
        {
            var candidateRemainder = string.Join(" ", candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1));
            var requestedRemainder = requestedTokens.Length > 1
                ? string.Join(" ", requestedTokens.Skip(1))
                : requestedCommand;
            var distance = requestedTokens.Length > 1
                ? LevenshteinDistance(requestedRemainder, candidateRemainder)
                : LevenshteinDistance(requestedCommand, candidate);
            var prefixScore = CommonPrefixLength(requestedRemainder, candidateRemainder);

            if (distance < bestDistance
                || (distance == bestDistance && prefixScore > bestPrefixScore))
            {
                bestDistance = distance;
                bestPrefixScore = prefixScore;
                bestCommand = candidate;
            }
        }

        return bestDistance <= Math.Max(3, requestedCommand.Length / 2)
            ? bestCommand
            : bestCommand;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var source = left.AsSpan();
        var target = right.AsSpan();
        var costs = new int[target.Length + 1];

        for (var j = 0; j <= target.Length; j++)
            costs[j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            var previousDiagonal = costs[0];
            costs[0] = i;

            for (var j = 1; j <= target.Length; j++)
            {
                var previousAbove = costs[j];
                var substitutionCost = source[i - 1] == target[j - 1] ? 0 : 1;
                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    previousDiagonal + substitutionCost);
                previousDiagonal = previousAbove;
            }
        }

        return costs[target.Length];
    }

    private static int CommonPrefixLength(string left, string right)
    {
        var max = Math.Min(left.Length, right.Length);
        for (var i = 0; i < max; i++)
        {
            if (char.ToLowerInvariant(left[i]) != char.ToLowerInvariant(right[i]))
                return i;
        }

        return max;
    }
}
