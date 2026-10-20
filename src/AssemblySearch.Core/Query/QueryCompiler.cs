using System.Text.RegularExpressions;
namespace AssemblySearch.Core.Query;
public sealed class CompiledQuery(Func<string,object?,bool,bool> match) {
 public bool IsMatch(string text,object? literal=null,bool isLiteral=false)=>match(text,literal,isLiteral);
}
public static class QueryCompiler {
 public static CompiledQuery Compile(SearchQuery query) {
  if(query.Text.Length>4096)throw new QueryValidationException("Query exceeds 4096 characters.");
  if(query.Limit is <1 or >100000)throw new QueryValidationException("Result limit must be 1–100000.");
  if(!Enum.IsDefined(query.Kind)||!Enum.IsDefined(query.Mode)||!Enum.IsDefined(query.Scope))throw new QueryValidationException("Unknown search option.");
  var text=query.Text.Trim();
  if(text.Length==0)return new((_,_,_)=>false);
  var comparison=query.CaseSensitive?StringComparison.InvariantCulture:StringComparison.InvariantCultureIgnoreCase;
  bool quoted=query.Mode==QueryMode.Literal&&text.Length>=2&&text[0]=='"'&&text[^1]=='"';
  bool slash=!quoted&&text.Length>2&&text[0]=='/'&&text[^1]=='/';
  if(query.Mode==QueryMode.Regex||slash){
   Regex regex;
   try{regex=new(slash?text[1..^1]:text,RegexOptions.CultureInvariant|(query.CaseSensitive?RegexOptions.None:RegexOptions.IgnoreCase),TimeSpan.FromMilliseconds(100));}
   catch(ArgumentException e){throw new QueryValidationException("Invalid regex: "+e.Message);}
   return new((s,v,lit)=>query.Mode==QueryMode.Literal?lit&&v is string value&&regex.IsMatch(value):regex.IsMatch(s));
  }
  if(query.Mode==QueryMode.Literal)return LiteralMatcher.Compile(text,comparison,query.Exact,quoted);
  var terms=text.Split(' ',StringSplitOptions.RemoveEmptyEntries);
  return new((s,_,_)=>query.AnyTerm?terms.Any(t=>TextMatcher.Match(s,t,comparison,query.Exact)):terms.All(t=>TextMatcher.Match(s,t,comparison,query.Exact)));
 }
}
