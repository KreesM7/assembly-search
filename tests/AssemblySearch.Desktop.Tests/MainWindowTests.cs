using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;
namespace AssemblySearch.Desktop.Tests;

public class MainWindowTests
{
    public static AssemblySearch.Desktop.MainWindow Window() => new(Path.Combine(Path.GetTempPath(), "assembly-search-test-" + Guid.NewGuid(), "settings.json"));
    [AvaloniaFact]
    public void LoadedAssembliesShowBasenames()
    {
        var w = Window();
        w.Show();
        w.LoadPaths([Path.Combine(AppContext.BaseDirectory, "SearchFixture.dll"), Path.Combine(AppContext.BaseDirectory, "ReferenceFixture.dll")]);
        Dispatcher.UIThread.RunJobs();
        w.UpdateLayout();
        Assert.Contains(w.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "SearchFixture.dll");
        Assert.Contains(w.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "ReferenceFixture.dll");
        w.Close();
    }
    [AvaloniaFact]
    public void AllCategoriesAndScopesAreAvailable()
    {
        var w = Window();
        w.Show();
        Assert.NotNull(w.FindControl<TextBox>("QueryBox"));
        Assert.Equal(24, w.FindControl<ComboBox>("CategoryBox")!.ItemCount);
        Assert.Equal(4, w.FindControl<ComboBox>("ScopeBox")!.ItemCount);
        w.Close();
    }
    [AvaloniaFact]
    public async Task LoadSearchAndSelectShowsContext()
    {
        var w = Window();
        w.Show();
        w.LoadPaths([Path.Combine(AppContext.BaseDirectory, "SearchFixture.dll")]);
        w.FindControl<TextBox>("QueryBox")!.Text = "Read";
        w.FindControl<ComboBox>("CategoryBox")!.SelectedItem = AssemblySearch.Core.Query.SearchKind.MethodDef;
        await w.SearchAsync(true);
        Dispatcher.UIThread.RunJobs();
        Assert.True(w.Model.Results.Count == 3, $"Rows={w.Model.Results.Count}, status={w.Model.Controller.Summary?.Status}, query={w.Model.Query}, searching={w.Model.Controller.IsSearching}");
        var list = w.FindControl<ListBox>("ResultsList")!;
        list.SelectedIndex = 0;
        Assert.Contains("Token", w.FindControl<TextBox>("ContextBox")!.Text);
        Assert.Contains("IL_", w.FindControl<TextBox>("ContextBox")!.Text);
        w.Close();
    }
    [AvaloniaFact]
    public async Task InvalidQueryExplainsRecovery()
    {
        var w = Window();
        w.Show();
        w.LoadPaths([Path.Combine(AppContext.BaseDirectory, "SearchFixture.dll")]);
        w.FindControl<TextBox>("QueryBox")!.Text = "[";
        w.FindControl<ComboBox>("ModeBox")!.SelectedItem = AssemblySearch.Core.Query.QueryMode.Regex;
        await w.SearchAsync(true);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("Invalid", w.FindControl<TextBlock>("StatusText")!.Text);
        Assert.Contains("regex", w.FindControl<TextBox>("DiagnosticsBox")!.Text, StringComparison.OrdinalIgnoreCase);
        w.Close();
    }
    [AvaloniaFact]
    public async Task SearchPreviewRenders()
    {
        var w = Window();
        w.Show();
        w.LoadPaths([Path.Combine(AppContext.BaseDirectory, "SearchFixture.dll")]);
        w.FindControl<TextBox>("QueryBox")!.Text = "Read";
        w.FindControl<ComboBox>("CategoryBox")!.SelectedItem = AssemblySearch.Core.Query.SearchKind.MethodDef;
        await w.SearchAsync(true);
        Dispatcher.UIThread.RunJobs();
        w.FindControl<ListBox>("ResultsList")!.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        using var bitmap = w.CaptureRenderedFrame();
        Assert.NotNull(bitmap);
        var path = Environment.GetEnvironmentVariable("ASSEMBLY_SEARCH_SCREENSHOT");
        if (path != null)
            bitmap.Save(path, Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        w.Close();
    }
    [AvaloniaFact]
    public void KeyboardFocusShortcutWorks()
    {
        var w = Window();
        w.Show();
        w.KeyPress(Key.F, RawInputModifiers.Control, PhysicalKey.F, "f");
        w.KeyRelease(Key.F, RawInputModifiers.Control, PhysicalKey.F, "f");
        Assert.True(w.FindControl<TextBox>("QueryBox")!.IsFocused);
        w.Close();
    }
}
