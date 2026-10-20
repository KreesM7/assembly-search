using AssemblySearch.Core.Metadata;
using AssemblySearch.Core.Query;
using Xunit;
namespace AssemblySearch.Core.Tests;

public class ResourceTests
{
    [Fact]
    public void ResourceNamesKeysAndValues()
    {
        var rows = CategoryTests.Read(SearchKind.Resource);
        Assert.Contains(rows, r => r.DisplayName.Contains("Embedded.txt"));
        Assert.Contains(rows, r => r.MatchText == "resource content needle");
        Assert.Contains(rows, r => r.MatchText == "WelcomeKey");
        Assert.Contains(rows, r => r.MatchText == "resource welcome needle");
    }
    [Fact]
    public void ResourceLiteralsKeepType()
    {
        using var stream = new MemoryStream();
        using var writer = new System.Resources.ResourceWriter(stream);
        writer.AddResource("Answer", 12345);
        writer.Generate();
        Assert.Contains(ResourceBoundaryTests.Decode(stream.ToArray()).Item1, r => Equals(r.LiteralValue, 12345));
    }
    [Fact]
    public void ResourceContentsAreOptional()
    {
        var rows = new MetadataReader().Read(CategoryTests.Fixture, new("", SearchKind.Resource, IncludeResourceContents: false), _ => { }, default).ToArray();
        Assert.Contains(rows, r => r.DisplayName.Contains("Embedded.txt"));
        Assert.DoesNotContain(rows, r => r.MatchText == "resource content needle");
    }
}
public class ResourceBoundaryTests
{
    public static (AssemblySearch.Core.Metadata.SearchCandidate[], List<AssemblySearch.Core.Search.SearchDiagnostic>) Decode(byte[] data, string name = "test.resources", CancellationToken token = default)
    {
        var messages = new List<AssemblySearch.Core.Search.SearchDiagnostic>();
        var rows = AssemblySearch.Core.Metadata.ResourceReader.Enumerate(new dnlib.DotNet.EmbeddedResource(name, data, dnlib.DotNet.ManifestResourceAttributes.Private), new("fixture", new string('0', 64), Guid.NewGuid()), new("", SearchKind.Resource), messages.Add, token).ToArray();
        return (rows, messages);
    }
    [Fact]
    public void SerializedTypesNeverDeserialize()
    {
        using var stream = new MemoryStream();
        using (var writer = new System.Resources.ResourceWriter(stream))
        {
            writer.AddResourceData("danger", "Dangerous.Type, Untrusted.Assembly", new byte[] { 1, 2, 3 });
            writer.Generate();
            var result = Decode(stream.ToArray());
            Assert.Contains(result.Item2, d => d.Code == "resource-unsupported");
        }
    }
    [Fact] public void InvalidTextIsReported() => Assert.Contains(Decode(new byte[] { 0xff, 0xfe, 0xff }, "bad.txt").Item2, d => d.Code == "resource-format");
    [Fact] public void SizeLimitIsReported() => Assert.Contains(Decode(new byte[16 * 1024 * 1024 + 1], "large.txt").Item2, d => d.Code == "resource-limit");
    [Fact] public void BrokenResourceIsReported() => Assert.Contains(Decode(new byte[] { 1, 2, 3 }).Item2, d => d.Code == "resource-format");
    [Fact]
    public void CancellationInterruptsDecoding()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => Decode(new byte[] { 1 }, "a.txt", cts.Token));
    }
}
