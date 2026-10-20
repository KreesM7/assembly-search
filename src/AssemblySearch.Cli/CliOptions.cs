using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
namespace AssemblySearch.Cli;

public sealed record CliOptions(SearchRequest Request, bool Json, bool Help)
{
    public static CliOptions Parse(string[] args)
    {
        var files = new List<string>();
        var selected = new List<string>();
        string? text = null, type = null;
        SearchKind kind = SearchKind.Any;
        QueryMode mode = QueryMode.Text;
        SearchScopeKind scope = SearchScopeKind.All;
        int limit = 10000;
        bool json = false, help = false, sensitive = false, exact = false, any = false, generated = false, framework = true, attributes = false, resources = true, pathsOnly = false;
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Value()
            {
                if (++i >= args.Length)
                    throw new QueryValidationException($"Missing value for {a}.");
                return args[i];
            }
            T EnumValue<T>() where T : struct, Enum
            {
                var v = Value();
                return Enum.TryParse<T>(v, true, out var parsed) && Enum.IsDefined(parsed) ? parsed : throw new QueryValidationException($"Unknown {a}: {v}.");
            }
            if (pathsOnly)
            {
                files.Add(a);
                continue;
            }
            switch (a)
            {
                case "--":
                    pathsOnly = true;
                    break;
                case "--help":
                case "-h":
                    help = true;
                    break;
                case "--query":
                case "-q":
                    text = Value();
                    break;
                case "--kind":
                    kind = EnumValue<SearchKind>();
                    break;
                case "--mode":
                    mode = EnumValue<QueryMode>();
                    break;
                case "--scope":
                    scope = EnumValue<SearchScopeKind>();
                    break;
                case "--selected":
                    selected.Add(Value());
                    break;
                case "--type":
                    type = Value();
                    break;
                case "--limit":
                    if (!int.TryParse(Value(), out limit))
                        throw new QueryValidationException("Limit must be an integer.");
                    break;
                case "--json":
                    json = true;
                    break;
                case "--case-sensitive":
                    sensitive = true;
                    break;
                case "--exact":
                    exact = true;
                    break;
                case "--any-term":
                    any = true;
                    break;
                case "--include-generated":
                    generated = true;
                    break;
                case "--no-framework":
                    framework = false;
                    break;
                case "--include-attributes":
                    attributes = true;
                    break;
                case "--no-resource-contents":
                    resources = false;
                    break;
                default:
                    if (a.StartsWith('-'))
                        throw new QueryValidationException($"Unknown argument: {a}. Use -- before filenames starting with '-'.");
                    files.Add(a);
                    break;
            }
        }
        if (!help && (text == null || files.Count == 0))
            throw new QueryValidationException("Provide assembly paths and --query. Use --help for examples.");
        if (kind == SearchKind.Literal)
            mode = QueryMode.Literal;
        var query = new SearchQuery(text ?? "", kind, mode, scope, selected, type, sensitive, exact, any, generated, framework, attributes, resources, limit);
        return new(new(query, files), json, help);
    }
}
