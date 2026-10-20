using System.Globalization;using AssemblySearch.Core.Search;
namespace AssemblySearch.Cli;
public static class OutputFormatter {
 public static string Safe(string text)=>string.Concat(text.Select(c=>char.IsControl(c)?"\\u"+((int)c).ToString("X4",CultureInfo.InvariantCulture):c.ToString()));
 public static string Row(SearchResult r)=>$"{r.Candidate.Kind}\t{Safe(r.Candidate.DisplayName)}\t{Safe(r.Candidate.Assembly.Path)}\t{r.Candidate.Token:X8}\t{r.Candidate.IlOffset:X4}";
}
