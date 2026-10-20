using System.Text.Json;
using System.Text.Json.Serialization;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
namespace AssemblySearch.Core.Serialization;

public static class SearchExport
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static string Serialize(SearchQuery query, IReadOnlyList<SearchResult> results, SearchSummary summary) => JsonSerializer.Serialize(new { schemaVersion = 1, query, results, diagnostics = summary.Diagnostics, summary }, Options);
}
