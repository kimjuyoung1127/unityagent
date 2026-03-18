namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// Summary statistics for the flight recorder log directory.
/// </summary>
public sealed class FlightStats
{
    /// <summary>Number of NDJSON log files.</summary>
    public int FileCount { get; set; }

    /// <summary>Total size of all log files in bytes.</summary>
    public long TotalBytes { get; set; }

    /// <summary>Total number of log entries across all files.</summary>
    public long EntryCount { get; set; }

    /// <summary>Date string (yyyy-MM-dd) of the oldest log file, or null if no files.</summary>
    public string? OldestDate { get; set; }

    /// <summary>Date string (yyyy-MM-dd) of the newest log file, or null if no files.</summary>
    public string? NewestDate { get; set; }
}
