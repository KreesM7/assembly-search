using AssemblySearch.Core.Search;
using AssemblySearch.Core.Query;
using AssemblySearch.Desktop.Presentation;
using Xunit;
namespace AssemblySearch.Desktop.Tests;

public class SearchControllerTests
{
    static SearchRequest Request(string q) => new(new(q, SearchKind.MethodDef), [Path.Combine(AppContext.BaseDirectory, "SearchFixture.dll")]);
    [Fact]
    public async Task RealSearchPopulatesDesktop()
    {
        await using var c = new SearchController(new SearchService(), a => a());
        await c.UpdateAsync(Request("Read"));
        Assert.Equal(3, c.Results.Count);
        Assert.Equal(SearchStatus.Complete, c.Summary!.Status);
        Assert.False(c.IsSearching);
    }
    [Fact]
    public async Task SupersededSearchNeverReturnsStaleRows()
    {
        await using var c = new SearchController(new SearchService(), a => a());
        var old = c.UpdateAsync(Request("Read"));
        var latest = c.UpdateAsync(Request("NativeNeedle"));
        await Task.WhenAll(old, latest);
        Assert.Single(c.Results);
        Assert.Equal("NativeNeedle", c.Results[0].Candidate.DisplayName);
    }
    [Fact]
    public async Task ClearIsImmediate()
    {
        await using var c = new SearchController(new SearchService(), a => a());
        await c.UpdateAsync(Request("Read"));
        var task = c.UpdateAsync(Request(""));
        Assert.Empty(c.Results);
        await task;
        Assert.Empty(c.Results);
    }
    [Fact]
    public async Task InvalidQueryIsVisible()
    {
        await using var c = new SearchController(new SearchService(), a => a());
        await c.UpdateAsync(Request("[") with
        {
            Query = new("[", Mode: QueryMode.Regex)
        });
        Assert.Equal(SearchStatus.Invalid, c.Summary!.Status);
        Assert.NotEmpty(c.Summary.Diagnostics);
    }
    [Fact]
    public async Task UnloadingRemovesMatchingResults()
    {
        await using var c = new SearchController(new SearchService(), a => a());
        var r = Request("Read");
        await c.UpdateAsync(r);
        await c.UnloadAsync(r.LoadedPaths[0]);
        Assert.Empty(c.Results);
    }
}
