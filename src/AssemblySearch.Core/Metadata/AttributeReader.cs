using dnlib.DotNet;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
namespace AssemblySearch.Core.Metadata;

public static class AttributeReader
{
    public static IEnumerable<SearchCandidate> Enumerate(IHasCustomAttribute owner, SearchCandidate parent, Action<SearchDiagnostic> report)
    {
        int n = 0;
        foreach (var attr in owner.CustomAttributes)
        {
            var root = parent with
            {
                DisplayName = attr.TypeFullName,
                MatchText = attr.TypeFullName,
                Coordinate = parent.Coordinate + ":attribute:" + n++,
                Context = parent.Context + "\nAttribute: " + attr.TypeFullName
            };
            yield return root;
            int argIndex = 0;
            foreach (var arg in attr.ConstructorArguments.Concat(attr.NamedArguments.Select(x => x.Argument)))
            {
                foreach (var value in Values(arg))
                {
                    var coord = root.Coordinate + ":argument:" + argIndex++;
                    if (value is null)
                        continue;
                    if (value is not (string or bool or char or byte or sbyte or short or ushort or int or uint or long or ulong or float or double))
                    {
                        report(new("attribute-unsupported", "Attribute value kind is not searchable.", parent.Assembly.Path));
                        continue;
                    }
                    string text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
                    var row = root with
                    {
                        DisplayName = text,
                        MatchText = text,
                        Coordinate = coord,
                        LiteralValue = value,
                        Context = root.Context + "\nValue: " + text
                    };
                    yield return row;
                    yield return row with
                    {
                        Kind = SearchKind.Literal,
                        IsLiteral = true,
                        Coordinate = coord + ":literal"
                    };
                }
            }
        }
    }
    static IEnumerable<object?> Values(CAArgument arg)
    {
        if (arg.Value is IList<CAArgument> array)
        {
            foreach (var child in array)
                foreach (var value in Values(child))
                    yield return value;
        }
        else
            yield return arg.Value is UTF8String s ? s.String : arg.Value;
    }
}
