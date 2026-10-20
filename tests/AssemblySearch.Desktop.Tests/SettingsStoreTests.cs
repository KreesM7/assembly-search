using AssemblySearch.Desktop.Presentation;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
using Xunit;
namespace AssemblySearch.Desktop.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void OptionsRoundTripWithoutSearchHistory()
    {
        var p = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            SettingsStore.Save(p, new(Kind: SearchKind.Literal, Exact: true, Width: 1100));
            var s = SettingsStore.Load(p, _ => { });
            Assert.Equal(SearchKind.Literal, s.Kind);
            Assert.True(s.Exact);
            Assert.Equal(1100, s.Width);
            Assert.DoesNotContain("query", File.ReadAllText(p), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("path", File.ReadAllText(p), StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(p); }
    }
    [Fact]
    public void BrokenSettingsRecover()
    {
        var p = Path.GetTempFileName();
        try
        {
            File.WriteAllText(p, "broken");
            var diagnostics = new List<SearchDiagnostic>();
            var s = SettingsStore.Load(p, diagnostics.Add);
            Assert.Equal(10000, s.Limit);
            Assert.Contains(diagnostics, x => x.Code == "settings-error");
        }
        finally { File.Delete(p); }
    }
    [Fact]
    public void GeometryAndOptionsAreValidated()
    {
        var p = Path.GetTempFileName();
        try
        {
            SettingsStore.Save(p, new(Width: 1, Height: 100000, Limit: -1));
            var s = SettingsStore.Load(p, _ => { });
            Assert.InRange(s.Width, 640, 3840);
            Assert.InRange(s.Height, 480, 2160);
            Assert.Equal(10000, s.Limit);
        }
        finally { File.Delete(p); }
    }
}
