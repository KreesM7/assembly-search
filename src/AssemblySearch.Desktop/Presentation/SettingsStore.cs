using System.Text.Json;
using AssemblySearch.Core.Search;
using AssemblySearch.Core.Serialization;
namespace AssemblySearch.Desktop.Presentation;

public static class SettingsStore
{
    public static string DefaultPath => Path.Combine(Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "assembly-search", "settings.json");
    static AppSettings Validate(AppSettings s) => s with { Kind = Enum.IsDefined(s.Kind) ? s.Kind : Core.Query.SearchKind.Any, Mode = Enum.IsDefined(s.Mode) ? s.Mode : Core.Query.QueryMode.Text, Scope = Enum.IsDefined(s.Scope) ? s.Scope : Core.Query.SearchScopeKind.All, Limit = s.Limit is >= 1 and <= 100000 ? s.Limit : 10000, Width = double.IsFinite(s.Width) ? Math.Clamp(s.Width, 640, 3840) : 1280, Height = double.IsFinite(s.Height) ? Math.Clamp(s.Height, 480, 2160) : 800 };
    public static AppSettings Load(string path, Action<SearchDiagnostic> report)
    {
        if (!File.Exists(path))
            return new();
        try
        {
            return Validate(JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), SearchExport.Options) ?? new());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { report(new("settings-error", e.Message, path)); return new(); }
    }
    public static void Save(string path, AppSettings settings)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + "." + Guid.NewGuid() + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(Validate(settings), SearchExport.Options));
            File.Move(temp, full, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
