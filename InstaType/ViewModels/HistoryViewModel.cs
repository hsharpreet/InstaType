using InstaType.Models;
using InstaType.Services;

namespace InstaType.ViewModels;

/// <summary>
/// ViewModel for <c>HistoryWindow</c>.
/// Loads transcription history from <see cref="IHistoryService"/>.
/// Supports keyword search (Core+) and CSV export (Core+).
/// Free tier shows a stub with the last 10 in-memory entries and an upgrade prompt.
/// </summary>
public sealed class HistoryViewModel : ViewModelBase
{
    private IReadOnlyList<TranscriptionEntry> _entries = [];
    private string _searchQuery = string.Empty;
    private bool _isLoading;

    /// <summary>Entries currently shown in the list, filtered by <see cref="SearchQuery"/>.</summary>
    public IReadOnlyList<TranscriptionEntry> Entries
    {
        get => _entries;
        private set => SetProperty(ref _entries, value);
    }

    /// <summary>Live search filter; triggers a filtered reload on change.</summary>
    public string SearchQuery
    {
        get => _searchQuery;
        set { if (SetProperty(ref _searchQuery, value)) _ = ReloadAsync(); }
    }

    /// <summary>True while an async load or search is in progress.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    // TODO (F-06): Inject IHistoryService, ISubscriptionService. Implement ReloadAsync,
    // ExportCommand (calls ExportToCsvAsync with SaveFileDialog path), ClearCommand.

    private Task ReloadAsync() => Task.CompletedTask;
}
