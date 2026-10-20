using AssemblySearch.Core.Metadata;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
using Xunit;
namespace AssemblySearch.Core.Tests;

public class BoundaryAuditTests
{
    [Fact]
    public void OversizedSparseFileIsRejectedBeforeReading()
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var stream = File.OpenWrite(path))
                stream.SetLength(512L * 1024 * 1024 + 1);
            Assert.Throws<IOException>(() => CategoryTests.Read(SearchKind.Any, path: path));
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public void FrameworkPolicyExcludesCoreLibraryButKeepsUnsignedSystemName()
    {
        var reader = new MetadataReader();
        var q = new SearchQuery("", SearchKind.AssemblyDef, IncludeFramework: false);
        Assert.Empty(reader.Read(typeof(object).Assembly.Location, q, _ => { }, default));
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        try
        {
            using (var module = dnlib.DotNet.ModuleDefMD.Load(CategoryTests.Fixture))
            {
                module.Assembly.Name = "System.Custom";
                module.Write(path);
            }
            Assert.Single(reader.Read(path, q, _ => { }, default));
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public async Task CancellationRetainsAlreadyPublishedBatches()
    {
        using var token = new CancellationTokenSource();
        await using var session = new SearchService(new EndlessReader()).Start(new(new("needle", Limit: 100000), ["/fixture"]), token.Token);
        var rows = new List<SearchResult>();
        await foreach (var batch in session.ReadBatchesAsync())
        {
            rows.AddRange(batch.Results);
            token.Cancel();
        }
        var summary = await session.Completion;
        Assert.NotEmpty(rows);
        Assert.Equal(SearchStatus.Cancelled, summary.Status);
        Assert.Equal(rows.Count, summary.ResultCount);
    }
    sealed class EndlessReader : IMetadataReader
    {
        public IEnumerable<SearchCandidate> Read(string path, SearchQuery query, Action<SearchDiagnostic> report, CancellationToken token)
        {
            for (int i = 0; i < 1000000; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return new(new(path, "hash", Guid.Empty), SearchKind.MethodDef, i.ToString(), "needle", null, null, (uint)i, null, "needle", null, false, false, "");
            }
        }
    }
}
