using InstaType.Models;

namespace InstaType.Services;

/// <summary>
/// Reads and writes transcription history.
/// Free tier: in-memory ring buffer (last 10 entries, not persisted).
/// Core+: SQLite database at %LOCALAPPDATA%\InstaType\history.db.
/// </summary>
public interface IHistoryService : IAsyncDisposable
{
    /// <summary>Adds a completed transcription to history.</summary>
    Task AddAsync(TranscriptionEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent <paramref name="count"/> entries, newest first.</summary>
    Task<IReadOnlyList<TranscriptionEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Full-text search over history (Core+ only; returns empty list on Free).</summary>
    Task<IReadOnlyList<TranscriptionEntry>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Deletes all history entries before <paramref name="before"/> (Core+ only).</summary>
    Task ClearAsync(DateTimeOffset? before = null, CancellationToken cancellationToken = default);

    /// <summary>Exports all history to CSV and writes to <paramref name="filePath"/> (Core+ only).</summary>
    Task ExportToCsvAsync(string filePath, CancellationToken cancellationToken = default);
}
