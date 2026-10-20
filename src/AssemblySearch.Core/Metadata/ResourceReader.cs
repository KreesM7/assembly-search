using System.Collections;
using System.Text;
using System.Globalization;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
using dnlib.DotNet;
namespace AssemblySearch.Core.Metadata;

public static class ResourceReader
{
    const int Limit = 16 * 1024 * 1024;
    public static IEnumerable<SearchCandidate> Enumerate(EmbeddedResource resource, AssemblyIdentity identity, SearchQuery query, Action<SearchDiagnostic> report, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string name = resource.Name;
        var root = new SearchCandidate(identity, SearchKind.Resource, "resource:" + name, name, null, null, resource.MDToken.Raw, null, name, null, false, false, "Resource: " + name);
        yield return root;
        if (!query.IncludeResourceContents)
            yield break;
        var reader = resource.CreateReader();
        if (reader.Length > Limit)
        {
            report(new("resource-limit", $"Resource {name} exceeds 16 MiB.", identity.Path));
            yield break;
        }
        var data = reader.ToArray();
        var values = new List<(string Key, object? Value)>();
        try
        {
            if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            {
                using var source = new MemoryStream(data, false);
                using var resources = new System.Resources.ResourceReader(source);
                IDictionaryEnumerator enumerator = resources.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    token.ThrowIfCancellationRequested();
                    string key = (string)enumerator.Key;
                    resources.GetResourceData(key, out var type, out var raw);
                    if (raw.Length > Limit)
                    {
                        report(new("resource-limit", $"Resource value {key} exceeds 16 MiB.", identity.Path));
                        continue;
                    }
                    values.Add((key, null));
                    using var b = new BinaryReader(new MemoryStream(raw), new UTF8Encoding(false, true));
                    object? value = type switch
                    {
                        "ResourceTypeCode.Null" => null,
                        "ResourceTypeCode.String" => b.ReadString(),
                        "ResourceTypeCode.Boolean" => b.ReadBoolean(),
                        "ResourceTypeCode.Char" => (char)b.ReadUInt16(),
                        "ResourceTypeCode.Byte" => b.ReadByte(),
                        "ResourceTypeCode.SByte" => b.ReadSByte(),
                        "ResourceTypeCode.Int16" => b.ReadInt16(),
                        "ResourceTypeCode.UInt16" => b.ReadUInt16(),
                        "ResourceTypeCode.Int32" => b.ReadInt32(),
                        "ResourceTypeCode.UInt32" => b.ReadUInt32(),
                        "ResourceTypeCode.Int64" => b.ReadInt64(),
                        "ResourceTypeCode.UInt64" => b.ReadUInt64(),
                        "ResourceTypeCode.Single" => b.ReadSingle(),
                        "ResourceTypeCode.Double" => b.ReadDouble(),
                        _ => null
                    };
                    if (value != null)
                        values.Add((key, value));
                    else if (type != "ResourceTypeCode.Null")
                        report(new("resource-unsupported", $"Resource {name}/{key}: unsupported {type}; no deserialization was attempted.", identity.Path));
                }
            }
            else if (new[] { ".txt", ".xml", ".json", ".config", ".csv", ".html", ".xaml", ".md", ".cs", ".vb", ".js", ".css", ".sql", ".yaml", ".yml", ".svg" }.Any(ext => name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                values.Add((name, new UTF8Encoding(false, true).GetString(data)));
            else
                report(new("resource-unsupported", $"Resource {name} has no supported text decoder.", identity.Path));
        }
        catch (Exception e) when (e is ArgumentException or IOException or BadImageFormatException or FormatException or NotSupportedException) { report(new("resource-format", $"Resource {name}: {e.Message}", identity.Path)); }
        int ordinal = 0;
        foreach (var (key, value) in values)
        {
            token.ThrowIfCancellationRequested();
            string text = value == null ? key : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            var c = root with
            {
                DisplayName = name + " / " + key,
                MatchText = text,
                LiteralValue = value,
                Coordinate = root.Coordinate + ":" + ordinal++,
                Context = root.Context + "\nKey: " + key + "\nValue: " + text
            };
            yield return c;
            if (value != null)
                yield return c with
                {
                    Kind = SearchKind.Literal,
                    IsLiteral = true,
                    Coordinate = c.Coordinate + ":literal"
                };
        }
    }
}
