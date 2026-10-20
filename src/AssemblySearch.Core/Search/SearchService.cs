using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AssemblySearch.Core.Metadata;
using AssemblySearch.Core.Query;
namespace AssemblySearch.Core.Search;

public sealed class SearchService(IMetadataReader? reader = null) : ISearchService
{
    readonly IMetadataReader reader = reader ?? new MetadataReader();
    public SearchSession Start(SearchRequest request, CancellationToken token = default)
    {
        var matcher = QueryCompiler.Compile(request.Query);
        var files = string.IsNullOrWhiteSpace(request.Query.Text) ? Array.Empty<string>() : ScopeResolver.Resolve(request);
        var session = new SearchSession(token);
        session.Completion = Task.Run(async () =>
        {
            var watch = Stopwatch.StartNew();
            var messages = new ConcurrentQueue<SearchDiagnostic>();
            int scanned = 0, skipped = 0, accepted = 0, emitted = 0, capped = 0;
            bool timedOut = false;
            var gate = new object();
            try
            {
                await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4), CancellationToken = session.Cancellation.Token }, async (path, ct) =>
                {
                    var buffer = new List<SearchResult>(100);
                    async ValueTask Flush()
                    {
                        if (buffer.Count == 0)
                            return;
                        var batch = buffer.ToArray();
                        await session.Channel.Writer.WriteAsync(new(batch), ct);
                        Interlocked.Add(ref emitted, batch.Length);
                        buffer.Clear();
                    }
                    try
                    {
                        foreach (var candidate in reader.Read(path, request.Query, messages.Enqueue, ct))
                        {
                            ct.ThrowIfCancellationRequested();
                            if (Volatile.Read(ref accepted) >= request.Query.Limit)
                                break;
                            if (!matcher.IsMatch(candidate.MatchText, candidate.LiteralValue, candidate.IsLiteral))
                                continue;
                            lock (gate)
                            {
                                if (accepted >= request.Query.Limit)
                                {
                                    capped = 1;
                                    break;
                                }
                                accepted++;
                                if (accepted == request.Query.Limit)
                                    capped = 1;
                            }
                            var key = $"{candidate.Assembly.Path}|{candidate.Assembly.Sha256}|{candidate.Assembly.Mvid}|{candidate.Kind}|{candidate.Coordinate}";
                            var id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
                            var value = candidate.LiteralValue;
                            buffer.Add(new(id, candidate, value?.GetType().Name ?? "none", Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""));
                            if (buffer.Count == 100)
                                await Flush();
                        }
                        Interlocked.Increment(ref scanned);
                    }
                    catch (RegexMatchTimeoutException) { messages.Enqueue(new("regex-timeout", "Regex exceeded its 100 ms match timeout.", path)); timedOut = true; session.Cancellation.Cancel(); }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or BadImageFormatException or InvalidOperationException or NotSupportedException or IndexOutOfRangeException) { messages.Enqueue(new("input-error", e.Message, path)); Interlocked.Increment(ref skipped); }
                    finally { if (buffer.Count > 0 && !ct.IsCancellationRequested) await Flush(); }
                });
            }
            catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested) { }
            catch (Exception e) { messages.Enqueue(new("search-error", e.Message)); }
            finally { session.Channel.Writer.TryComplete(); }
            if (capped != 0)
                messages.Enqueue(new("result-limit", $"Stopped at the {request.Query.Limit} result limit; refine the query."));
            var status = timedOut ? SearchStatus.Incomplete : session.Cancellation.IsCancellationRequested ? SearchStatus.Cancelled : messages.IsEmpty ? SearchStatus.Complete : SearchStatus.Incomplete;
            return new SearchSummary(status, watch.ElapsedMilliseconds, scanned, skipped, emitted, messages.ToArray());
        });
        return session;
    }
}
