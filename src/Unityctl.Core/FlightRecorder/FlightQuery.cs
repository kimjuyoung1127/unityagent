namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// Filter parameters for querying the flight recorder log.
/// </summary>
public sealed class FlightQuery
{
    /// <summary>Filter by operation name (e.g., "build", "test").</summary>
    public string? Op { get; set; }

    /// <summary>Filter by level ("info", "warn", "error").</summary>
    public string? Level { get; set; }

    /// <summary>Only include entries at or after this time.</summary>
    public DateTimeOffset? Since { get; set; }

    /// <summary>Only include entries at or before this time.</summary>
    public DateTimeOffset? Until { get; set; }

    /// <summary>Maximum number of entries to return (default: 20).</summary>
    public int? Last { get; set; }

    /// <summary>Filter by project path (exact match).</summary>
    public string? ProjectPath { get; set; }
}
