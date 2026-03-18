namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// Result of a flight log prune operation.
/// </summary>
public sealed class PruneResult
{
    /// <summary>Number of files deleted.</summary>
    public int DeletedFiles { get; set; }

    /// <summary>Total bytes freed.</summary>
    public long FreedBytes { get; set; }
}
