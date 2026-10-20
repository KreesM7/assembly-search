using System.Collections.ObjectModel;
using System.ComponentModel;
using AssemblySearch.Core.Search;
using AssemblySearch.Core.Query;
namespace AssemblySearch.Desktop.Presentation;

public sealed class SearchController(ISearchService service, Action<Action> postToUi, TimeProvider? timeProvider = null) : INotifyPropertyChanged, IAsyncDisposable
{
    readonly TimeProvider clock = timeProvider ?? TimeProvider.System; readonly List<Task> runs = [];
    CancellationTokenSource? cancellation; Task active = Task.CompletedTask; long generation; bool disposed;
    public ObservableCollection<SearchResult> Results { get; } = [];
    public SearchSummary? Summary
    {
        get; private set;
    }
    public bool IsSearching
    {
        get; private set;
    }
    public IReadOnlyList<SearchDiagnostic> Diagnostics => Summary?.Diagnostics ?? [];
    public event PropertyChangedEventHandler? PropertyChanged;
    void Changed()
    {
        PropertyChanged?.Invoke(this, new(null));
    }
    static void TryCancel(CancellationTokenSource? source)
    {
        try
        {
            source?.Cancel();
        }
        catch (ObjectDisposedException) { }
    }
    public Task UpdateAsync(SearchRequest request) => Start(request, false);
    public Task SubmitAsync(SearchRequest request) => Start(request, true);
    Task Start(SearchRequest request, bool immediate)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        long current = Interlocked.Increment(ref generation);
        TryCancel(cancellation);
        var source = new CancellationTokenSource();
        cancellation = source;
        postToUi(() => { if (current != generation) return; Results.Clear(); Summary = null; IsSearching = !string.IsNullOrWhiteSpace(request.Query.Text); Changed(); });
        runs.RemoveAll(t => t.IsCompleted);
        active = Run(request, current, source, immediate);
        runs.Add(active);
        return active;
    }
    async Task Run(SearchRequest request, long current, CancellationTokenSource source, bool immediate)
    {
        void Post(Action action) => postToUi(() => { if (current == generation && !disposed) action(); });
        try
        {
            if (!immediate && !string.IsNullOrWhiteSpace(request.Query.Text))
                await Task.Delay(TimeSpan.FromMilliseconds(250), clock, source.Token);
            source.Token.ThrowIfCancellationRequested();
            await using var session = service.Start(request, source.Token);
            await foreach (var batch in session.ReadBatchesAsync())
                Post(() => { foreach (var result in batch.Results) Results.Add(result); });
            var summary = await session.Completion;
            Post(() => { Summary = summary; Changed(); });
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { Post(() => { Summary = new(SearchStatus.Cancelled, 0, 0, 0, Results.Count, []); Changed(); }); }
        catch (Exception e) { Post(() => { Summary = new(e is QueryValidationException ? SearchStatus.Invalid : SearchStatus.Incomplete, 0, 0, 0, Results.Count, [new("query-error", e.Message)]); Changed(); }); }
        finally { Post(() => { IsSearching = false; Changed(); }); source.Dispose(); }
    }
    public async Task CancelAsync()
    {
        TryCancel(cancellation);
        await active;
    }
    public async Task UnloadAsync(string path)
    {
        Interlocked.Increment(ref generation);
        await CancelAsync();
        postToUi(() => { foreach (var result in Results.Where(r => r.Candidate.Assembly.Path == ScopeResolver.CanonicalPath(path)).ToArray()) Results.Remove(result); IsSearching = false; Changed(); });
    }
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;
        disposed = true;
        Interlocked.Increment(ref generation);
        TryCancel(cancellation);
        await Task.WhenAll(runs);
    }
}
