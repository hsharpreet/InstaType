using InstaType.Models;
using InstaType.Services;

namespace InstaType.ViewModels;

/// <summary>
/// ViewModel for <c>HistoryWindow</c>.
/// Loads transcription history from <see cref="IHistoryService"/>, newest first.
/// Supports live keyword search and clear-all.
/// </summary>
public sealed class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryService _history;

    private IReadOnlyList<TranscriptionEntry> _entries = [];
    private string _searchQuery = string.Empty;
    private bool   _isLoading;

    public IReadOnlyList<TranscriptionEntry> Entries
    {
        get => _entries;
        private set => SetProperty(ref _entries, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set { if (SetProperty(ref _searchQuery, value)) _ = ReloadAsync(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsEmpty => _entries.Count == 0;

    public HistoryViewModel(IHistoryService historyService)
    {
        _history = historyService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Entries = string.IsNullOrWhiteSpace(_searchQuery)
                ? await _history.GetRecentAsync(500)
                : await _history.SearchAsync(_searchQuery.Trim());
            OnPropertyChanged(nameof(IsEmpty));
        }
        finally { IsLoading = false; }
    }

    public async Task ClearAllAsync()
    {
        await _history.ClearAsync();
        Entries = [];
        OnPropertyChanged(nameof(IsEmpty));
    }

    private Task ReloadAsync() => LoadAsync();
}
