using InstaType.Models;
using InstaType.Services;

namespace InstaType.Infrastructure.Storage;

/// <summary>
/// Implements <see cref="IHistoryService"/>.
/// Free tier: in-memory ring buffer (last 10 entries, cleared on exit).
/// Core+: SQLite database at %LOCALAPPDATA%\InstaType\history.db via Microsoft.Data.Sqlite.
/// Schema migration handled at startup via simple version table.
/// </summary>
internal sealed class HistoryService : IHistoryService
{
    // TODO (F-06): Implement SQLite schema init, Add, GetRecent, Search (FTS5),
    // Clear, ExportToCsv. Free tier path uses a fixed-size Queue<TranscriptionEntry>.

    public Task AddAsync(TranscriptionEntry entry, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<TranscriptionEntry>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<TranscriptionEntry>> SearchAsync(string query, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task ClearAsync(DateTimeOffset? before = null, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task ExportToCsvAsync(string filePath, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
