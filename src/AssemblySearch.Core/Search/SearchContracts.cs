namespace AssemblySearch.Core.Search;
public sealed record SearchDiagnostic(string Code,string Message,string? Path=null);
public enum SearchStatus{Complete,Incomplete,Cancelled,Invalid}
public sealed record SearchRequest(AssemblySearch.Core.Query.SearchQuery Query,IReadOnlyList<string> LoadedPaths);
public sealed record SearchResult(string Id,AssemblySearch.Core.Metadata.SearchCandidate Candidate,string ValueKind,string ValueDisplay);
public sealed record SearchBatch(IReadOnlyList<SearchResult> Results);
public sealed record SearchSummary(SearchStatus Status,long ElapsedMilliseconds,int ScannedFiles,int SkippedFiles,int ResultCount,IReadOnlyList<SearchDiagnostic> Diagnostics);
public interface ISearchService{SearchSession Start(SearchRequest request,CancellationToken token=default);}
