using AssemblySearch.Core.Query;
namespace AssemblySearch.Core.Metadata;
public sealed record AssemblyIdentity(string Path,string Sha256,Guid Mvid);
public sealed record SearchCandidate(AssemblyIdentity Assembly,SearchKind Kind,string Coordinate,string DisplayName,string? DeclaringType,string? DeclaringMember,uint? Token,uint? IlOffset,string MatchText,[property: System.Text.Json.Serialization.JsonIgnore] object? LiteralValue,bool IsLiteral,bool IsGenerated,string Context);
public interface IMetadataReader { IEnumerable<SearchCandidate> Read(string path,SearchQuery query,Action<AssemblySearch.Core.Search.SearchDiagnostic> report,CancellationToken token); }
