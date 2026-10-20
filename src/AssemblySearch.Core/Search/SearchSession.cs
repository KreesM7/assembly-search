using System.Threading.Channels;
namespace AssemblySearch.Core.Search;

public sealed class SearchSession : IAsyncDisposable
{
    internal readonly Channel<SearchBatch> Channel = System.Threading.Channels.Channel.CreateBounded<SearchBatch>(4);
    internal readonly CancellationTokenSource Cancellation;
    public Task<SearchSummary> Completion { get; internal set; } = Task.FromResult(new SearchSummary(SearchStatus.Complete, 0, 0, 0, 0, []));
    internal SearchSession(CancellationToken token)
    {
        Cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
    }
    public IAsyncEnumerable<SearchBatch> ReadBatchesAsync(CancellationToken token = default) => Channel.Reader.ReadAllAsync(token);
    public async ValueTask DisposeAsync()
    {
        Cancellation.Cancel();
        await Completion;
        Cancellation.Dispose();
    }
}
