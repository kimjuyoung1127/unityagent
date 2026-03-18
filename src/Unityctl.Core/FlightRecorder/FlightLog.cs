using System.Globalization;
using System.Text.Json;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Core.FlightRecorder;

/// <summary>
/// Append-only NDJSON flight recorder.
/// Phase 3B: full implementation with retention, query, and stats.
/// Thread-safe for concurrent Record() calls within the same process.
/// </summary>
public sealed class FlightLog
{
    private readonly string _logDirectory;
    private readonly object _writeLock = new();

    /// <summary>The directory where NDJSON log files are stored.</summary>
    public string LogDirectory => _logDirectory;

    public FlightLog(string? logDirectory = null)
    {
        _logDirectory = logDirectory ??
            Path.Combine(Constants.GetConfigDirectory(), Constants.FlightLogDirectory);
    }

    /// <summary>
    /// Append a flight entry to today's NDJSON log file.
    /// Never throws — all errors are silently swallowed.
    /// </summary>
    public void Record(FlightEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry, FlightJsonContext.Default.FlightEntry);
            var fileName = $"flight-{DateTimeOffset.UtcNow:yyyy-MM-dd}.ndjson";
            var filePath = Path.Combine(_logDirectory, fileName);
            lock (_writeLock)
            {
                Directory.CreateDirectory(_logDirectory);
                File.AppendAllText(filePath, json + "\n");
            }
        }
        catch
        {
            // Flight recording should never crash the CLI
        }
    }

    /// <summary>
    /// Query log entries with optional filters.
    /// Returns entries in newest-first order, capped by <see cref="FlightQuery.Last"/>.
    /// </summary>
    public List<FlightEntry> Query(FlightQuery query)
    {
        var results = new List<FlightEntry>();
        try
        {
            if (!Directory.Exists(_logDirectory))
                return results;

            var limit = query.Last ?? 20;

            // Newest files first
            var files = Directory.GetFiles(_logDirectory, "flight-*.ndjson")
                .OrderByDescending(f => f)
                .ToArray();

            foreach (var file in files)
            {
                var dateStr = ExtractDateFromFileName(Path.GetFileName(file));
                if (dateStr != null)
                {
                    if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal, out var fileDate))
                    {
                        // Skip files that are entirely after Until
                        if (query.Until.HasValue && fileDate.Date > query.Until.Value.Date)
                            continue;

                        // Files are newest-first; once a file is before Since, all remaining are too
                        if (query.Since.HasValue && fileDate.Date < query.Since.Value.Date)
                            break;
                    }
                }

                // Read lines in reverse (newest line first within the file)
                var lines = File.ReadAllLines(file);
                for (var i = lines.Length - 1; i >= 0; i--)
                {
                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    FlightEntry? entry;
                    try
                    {
                        entry = JsonSerializer.Deserialize(line, FlightJsonContext.Default.FlightEntry);
                    }
                    catch
                    {
                        continue;
                    }

                    if (entry == null) continue;

                    // Entry-level filters
                    if (query.Op != null &&
                        !entry.Operation.Equals(query.Op, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (query.Level != null &&
                        !entry.Level.Equals(query.Level, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (query.ProjectPath != null && entry.Project != query.ProjectPath)
                        continue;

                    if (query.Since.HasValue)
                    {
                        var ts = DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp);
                        if (ts < query.Since.Value) continue;
                    }

                    if (query.Until.HasValue)
                    {
                        var ts = DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp);
                        if (ts > query.Until.Value) continue;
                    }

                    results.Add(entry);
                    if (results.Count >= limit) return results;
                }
            }
        }
        catch
        {
            // Never crash the CLI
        }

        return results;
    }

    /// <summary>
    /// Delete log files older than 30 days, then enforce 50 MB total size limit.
    /// Never throws.
    /// </summary>
    public PruneResult Prune()
    {
        var result = new PruneResult();
        try
        {
            if (!Directory.Exists(_logDirectory))
                return result;

            var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

            // Oldest files first so we can delete from the front
            var files = Directory.GetFiles(_logDirectory, "flight-*.ndjson")
                .OrderBy(f => f)
                .ToList();

            // Phase 1: delete files older than 30 days
            var toDelete = new List<string>();
            foreach (var file in files)
            {
                var dateStr = ExtractDateFromFileName(Path.GetFileName(file));
                if (dateStr != null &&
                    DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var fileDate))
                {
                    if (fileDate.Date < cutoff.Date)
                        toDelete.Add(file);
                }
            }

            foreach (var file in toDelete)
            {
                var size = new FileInfo(file).Length;
                File.Delete(file);
                result.DeletedFiles++;
                result.FreedBytes += size;
                files.Remove(file);
            }

            // Phase 2: enforce 50 MB total size limit
            const long MaxBytes = 50L * 1024 * 1024;
            var totalBytes = files.Sum(f => new FileInfo(f).Length);

            while (totalBytes > MaxBytes && files.Count > 0)
            {
                var oldest = files[0];
                var size = new FileInfo(oldest).Length;
                File.Delete(oldest);
                result.DeletedFiles++;
                result.FreedBytes += size;
                totalBytes -= size;
                files.RemoveAt(0);
            }
        }
        catch
        {
            // Never crash the CLI
        }

        return result;
    }

    /// <summary>
    /// Return statistics about the log directory.
    /// Never throws.
    /// </summary>
    public FlightStats GetStats()
    {
        var stats = new FlightStats();
        try
        {
            if (!Directory.Exists(_logDirectory))
                return stats;

            var files = Directory.GetFiles(_logDirectory, "flight-*.ndjson")
                .OrderBy(f => f)
                .ToArray();

            stats.FileCount = files.Length;

            var dates = files
                .Select(f => ExtractDateFromFileName(Path.GetFileName(f)))
                .Where(d => d != null)
                .ToArray();

            stats.OldestDate = dates.Length > 0 ? dates[0] : null;
            stats.NewestDate = dates.Length > 0 ? dates[dates.Length - 1] : null;

            foreach (var file in files)
            {
                stats.TotalBytes += new FileInfo(file).Length;
                stats.EntryCount += File.ReadAllLines(file).Count(l => !string.IsNullOrWhiteSpace(l));
            }
        }
        catch
        {
            // Never crash the CLI
        }

        return stats;
    }

    /// <summary>
    /// Extract the date string from a flight log file name.
    /// "flight-2026-03-18.ndjson" → "2026-03-18"
    /// </summary>
    private static string? ExtractDateFromFileName(string fileName)
    {
        const string prefix = "flight-";
        const string suffix = ".ndjson";

        if (fileName.StartsWith(prefix, StringComparison.Ordinal) &&
            fileName.EndsWith(suffix, StringComparison.Ordinal))
        {
            var dateStr = fileName.Substring(prefix.Length,
                fileName.Length - prefix.Length - suffix.Length);
            return dateStr.Length == 10 ? dateStr : null; // yyyy-MM-dd = 10 chars
        }

        return null;
    }
}
