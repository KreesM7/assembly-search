using AssemblySearch.Core.Query;using AssemblySearch.Core.Search;using AssemblySearch.Core.Serialization;
namespace AssemblySearch.Cli;
public static class CliRunner {
 public static async Task<int> RunAsync(string[] args,TextWriter stdout,TextWriter stderr,CancellationToken token=default){
  try{
   var options=CliOptions.Parse(args);
   if(options.Help){await stdout.WriteLineAsync(Help);return 0;}
   await using var session=new SearchService().Start(options.Request,token);
   var rows=new List<SearchResult>();await foreach(var batch in session.ReadBatchesAsync())rows.AddRange(batch.Results);
   var summary=await session.Completion;
   rows=rows.OrderBy(r=>r.Candidate.Assembly.Path,StringComparer.Ordinal).ThenBy(r=>r.Candidate.DeclaringType,StringComparer.Ordinal).ThenBy(r=>r.Candidate.DisplayName,StringComparer.Ordinal).ThenBy(r=>r.Candidate.Token).ThenBy(r=>r.Candidate.IlOffset).ThenBy(r=>r.Candidate.Coordinate,StringComparer.Ordinal).ToList();
   if(options.Json)await stdout.WriteLineAsync(SearchExport.Serialize(options.Request.Query,rows,summary));
   else{foreach(var row in rows)await stdout.WriteLineAsync(OutputFormatter.Row(row));await stderr.WriteLineAsync($"{summary.Status}: {rows.Count} results, {summary.ScannedFiles} scanned, {summary.SkippedFiles} skipped, {summary.ElapsedMilliseconds} ms");}
   foreach(var d in summary.Diagnostics)await stderr.WriteLineAsync(OutputFormatter.Safe($"{d.Code}: {d.Path}: {d.Message}"));
   return summary.Status switch{SearchStatus.Complete=>0,SearchStatus.Cancelled=>130,_=>3};
  }catch(Exception e)when(e is QueryValidationException or ArgumentException){await stderr.WriteLineAsync(OutputFormatter.Safe(e.Message));return 2;}
 }
 public static string Help=>"""
 Assembly Search — offline .NET metadata and IL search
 Usage: assembly-search FILE.dll [FILE.exe ...] --query TEXT [options]
   --kind KIND       All 24 categories: AssemblyDef, ModuleDef, Namespace, TypeDef,
                     FieldDef, MethodDef, PropertyDef, EventDef, ParamDef, Local,
                     ParamLocal, AssemblyRef, ModuleRef, Resource, GenericTypeDef,
                     NonGenericTypeDef, EnumTypeDef, InterfaceTypeDef, ClassTypeDef,
                     StructTypeDef, DelegateTypeDef, Member, Any, Literal
   --mode text|regex|literal  (/pattern/ also selects regex; Literal kind sets literal mode)
   --scope all|selected|directory|type
   --selected FILE   Repeatable selection from loaded input paths
   --type FULLNAME   Type scope; nested type separator is /
   --case-sensitive  Invariant-culture comparison (default ignores case)
   --exact           Exact candidate/value equality, not a word-boundary search
   --any-term        OR space-separated terms (default AND)
   --include-generated --no-framework --include-attributes --no-resource-contents
   --limit N         1–100000 (default 10000); capped results are incomplete
   --json            Versioned JSON on stdout; diagnostics on stderr
   --                Remaining arguments are filenames
 Exit: 0 complete, 2 invalid query/arguments, 3 incomplete, 130 cancelled
 Input assemblies are parsed as data and never executed. Missing PDBs and
 unsupported resource contents are explicit diagnostics. Regex timeout: 100 ms.
 """;
}
