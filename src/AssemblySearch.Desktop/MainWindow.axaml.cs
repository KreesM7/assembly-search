using Avalonia.Input.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AssemblySearch.Core.Query;
using AssemblySearch.Core.Search;
using AssemblySearch.Core.Metadata;
using AssemblySearch.Core.Serialization;
using AssemblySearch.Desktop.Presentation;
namespace AssemblySearch.Desktop;

public partial class MainWindow : Window
{
    public MainWindowViewModel Model
    {
        get;
    }
    readonly string settingsPath; bool ready, closing; string? details, lastRequest;
    T C<T>(string name) where T : Control => this.FindControl<T>(name)!;
    public MainWindow() : this(null) { }
    public MainWindow(string? settingsPath)
    {
        AvaloniaXamlLoader.Load(this);
        this.settingsPath = settingsPath ?? SettingsStore.DefaultPath;
        Model = new(new SearchController(new SearchService(), a => { if (Dispatcher.UIThread.CheckAccess()) a(); else Dispatcher.UIThread.Post(a); }));
        var messages = new List<SearchDiagnostic>();
        var settings = SettingsStore.Load(this.settingsPath, messages.Add);
        Width = settings.Width;
        Height = settings.Height;
        C<ComboBox>("CategoryBox").ItemsSource = Enum.GetValues<SearchKind>();
        C<ComboBox>("CategoryBox").SelectedItem = settings.Mode == QueryMode.Literal ? SearchKind.Literal : settings.Kind;
        C<ComboBox>("ModeBox").ItemsSource = Enum.GetValues<QueryMode>();
        C<ComboBox>("ModeBox").SelectedItem = settings.Kind == SearchKind.Literal ? QueryMode.Literal : settings.Mode;
        C<ComboBox>("ScopeBox").ItemsSource = Enum.GetValues<SearchScopeKind>();
        C<ComboBox>("ScopeBox").SelectedItem = settings.Scope;
        C<CheckBox>("CaseBox").IsChecked = settings.CaseSensitive;
        C<CheckBox>("ExactBox").IsChecked = settings.Exact;
        C<CheckBox>("AnyBox").IsChecked = settings.AnyTerm;
        C<CheckBox>("GeneratedBox").IsChecked = settings.IncludeGenerated;
        C<CheckBox>("FrameworkBox").IsChecked = settings.IncludeFramework;
        C<CheckBox>("AttributesBox").IsChecked = settings.IncludeAttributes;
        C<CheckBox>("ResourcesBox").IsChecked = settings.IncludeResourceContents;
        C<NumericUpDown>("LimitBox").Value = settings.Limit;
        C<ListBox>("LoadedList").ItemsSource = Model.LoadedPaths;
        C<ListBox>("ResultsList").ItemsSource = Model.Results;
        Model.Controller.PropertyChanged += (_, _) => RefreshState();
        Model.Results.CollectionChanged += (_, _) => RefreshState();
        C<TextBox>("QueryBox").TextChanged += async (_, _) => { C<Button>("ClearButton").IsVisible = !string.IsNullOrEmpty(C<TextBox>("QueryBox").Text); if (ready) await SearchAsync(); };
        C<TextBox>("TypeBox").TextChanged += async (_, _) => { if (ready) await SearchAsync(); };
        foreach (var name in new[] { "CategoryBox", "ModeBox", "ScopeBox" })
            C<ComboBox>(name).SelectionChanged += async (_, _) =>
            {
                if (!ready)
                    return;
                C<StackPanel>("TypePanel").IsVisible = C<ComboBox>("ScopeBox").SelectedItem is SearchScopeKind.Type;
                var category = C<ComboBox>("CategoryBox");
                var mode = C<ComboBox>("ModeBox");
                if (name == "CategoryBox")
                {
                    if (category.SelectedItem is SearchKind.Literal)
                        mode.SelectedItem = QueryMode.Literal;
                    else if (mode.SelectedItem is QueryMode.Literal)
                        mode.SelectedItem = QueryMode.Text;
                }
                else if (name == "ModeBox")
                {
                    if (mode.SelectedItem is QueryMode.Literal)
                        category.SelectedItem = SearchKind.Literal;
                    else if (category.SelectedItem is SearchKind.Literal)
                        category.SelectedItem = SearchKind.Any;
                }
                await SearchAsync();
            };
        foreach (var name in new[] { "CaseBox", "ExactBox", "AnyBox", "GeneratedBox", "FrameworkBox", "AttributesBox", "ResourcesBox" })
            C<CheckBox>(name).IsCheckedChanged += async (_, _) => { if (ready) await SearchAsync(); };
        C<NumericUpDown>("LimitBox").ValueChanged += async (_, _) => { if (ready) await SearchAsync(); };
        C<ListBox>("LoadedList").SelectionChanged += async (_, _) => { if (ready && C<ComboBox>("ScopeBox").SelectedItem is not SearchScopeKind.All) await SearchAsync(); };
        C<ListBox>("ResultsList").SelectionChanged += (_, _) => ShowSelection();
        C<Button>("OpenButton").Click += async (_, _) => await OpenFiles();
        C<Button>("RemoveButton").Click += async (_, _) => { foreach (var p in C<ListBox>("LoadedList").SelectedItems!.Cast<string>().ToArray()) { await Model.Controller.UnloadAsync(p); Model.LoadedPaths.Remove(p); } await SearchAsync(true); };
        C<Button>("SearchButton").Click += async (_, _) => await SearchAsync(true);
        C<Button>("CancelButton").Click += async (_, _) => await Model.Controller.CancelAsync();
        C<Button>("ClearButton").Click += async (_, _) => { C<TextBox>("QueryBox").Text = ""; await SearchAsync(true); C<TextBox>("QueryBox").Focus(); };
        C<Button>("ExportButton").Click += async (_, _) => await ExportResults();
        C<Button>("CopyButton").Click += async (_, _) => { try { if (Clipboard is { } clipboard && details != null) await clipboard.SetTextAsync(details); } catch (Exception e) { Notice(e.Message); } };
        KeyDown += async (_, e) => { if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control)) { C<TextBox>("QueryBox").Focus(); e.Handled = true; } else if (e.Key == Key.Escape) { await Model.Controller.CancelAsync(); e.Handled = true; } else if (e.Key == Key.Enter && C<TextBox>("QueryBox").IsFocused) { await SearchAsync(true); e.Handled = true; } };
        Closing += async (_, e) => { if (closing) return; e.Cancel = true; closing = true; ready = false; await Model.Controller.DisposeAsync(); try { var q = BuildQuery(); SettingsStore.Save(this.settingsPath, new(q.Kind, q.Mode, q.Scope, q.CaseSensitive, q.Exact, q.AnyTerm, q.IncludeGenerated, q.IncludeFramework, q.IncludeAttributes, q.IncludeResourceContents, q.Limit, Width, Height)); } catch (Exception ex) { Notice(ex.Message); } Close(); };
        Opened += (_, _) => { if (Screens.Primary is { } screen) { Width = Math.Min(Width, screen.WorkingArea.Width / RenderScaling); Height = Math.Min(Height, screen.WorkingArea.Height / RenderScaling); } C<TextBox>("QueryBox").Focus(); };
        ready = true;
        C<StackPanel>("TypePanel").IsVisible = settings.Scope == SearchScopeKind.Type;
        if (messages.Count > 0)
            Notice(string.Join('\n', messages.Select(x => x.Message)));
    }
    SearchQuery BuildQuery() => new(C<TextBox>("QueryBox").Text ?? "", (SearchKind)(C<ComboBox>("CategoryBox").SelectedItem ?? SearchKind.Any), (QueryMode)(C<ComboBox>("ModeBox").SelectedItem ?? QueryMode.Text), (SearchScopeKind)(C<ComboBox>("ScopeBox").SelectedItem ?? SearchScopeKind.All), C<ListBox>("LoadedList").SelectedItems!.Cast<string>().ToArray(), C<TextBox>("TypeBox").Text, C<CheckBox>("CaseBox").IsChecked == true, C<CheckBox>("ExactBox").IsChecked == true, C<CheckBox>("AnyBox").IsChecked == true, C<CheckBox>("GeneratedBox").IsChecked == true, C<CheckBox>("FrameworkBox").IsChecked == true, C<CheckBox>("AttributesBox").IsChecked == true, C<CheckBox>("ResourcesBox").IsChecked == true, (int)(C<NumericUpDown>("LimitBox").Value ?? 10000));
    public void LoadPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var canonical = ScopeResolver.CanonicalPath(path);
            if (!Model.LoadedPaths.Contains(canonical))
                Model.LoadedPaths.Add(canonical);
        }
        if (C<ListBox>("LoadedList").SelectedIndex < 0 && Model.LoadedPaths.Count > 0)
            C<ListBox>("LoadedList").SelectedIndex = 0;
        C<TextBlock>("AssemblyCountText").Text = $"Loaded assemblies ({Model.LoadedPaths.Count})";
    }
    public Task SearchAsync(bool immediate = false)
    {
        if (!ready)
            return Task.CompletedTask;
        var query = BuildQuery();
        var fingerprint = System.Text.Json.JsonSerializer.Serialize(new SearchRequest(query, Model.LoadedPaths.ToArray()), SearchExport.Options);
        if (!immediate && fingerprint == lastRequest)
            return Task.CompletedTask;
        lastRequest = fingerprint;
        details = null;
        C<Button>("CopyButton").IsEnabled = false;
        C<TextBox>("ContextBox").Text = "Select a result to inspect its metadata and instruction context.";
        Model.Query = query;
        return Model.SearchAsync(immediate);
    }
    void RefreshState()
    {
        var c = Model.Controller;
        C<TextBlock>("ResultCountText").Text = $"Results ({Model.Results.Count})";
        C<Button>("CancelButton").IsEnabled = c.IsSearching;
        C<ProgressBar>("SearchProgress").IsVisible = c.IsSearching;
        C<Button>("ExportButton").IsEnabled = !c.IsSearching && c.Summary != null;
        C<TextBlock>("StatusText").Text = c.IsSearching ? "Searching…" : c.Summary is { } s ? $"{s.Status} · {Model.Results.Count} results · {s.ScannedFiles} scanned · {s.SkippedFiles} skipped · {s.ElapsedMilliseconds} ms" : "Ready";
        C<TextBlock>("EmptyText").IsVisible = Model.Results.Count == 0;
        C<TextBlock>("EmptyText").Text = c.IsSearching ? "Searching loaded assemblies…" : string.IsNullOrWhiteSpace(C<TextBox>("QueryBox").Text) ? "Open assemblies, then enter a search query." : c.Summary?.Status == SearchStatus.Complete ? "No matches. Try another category or broaden the query." : "Search incomplete or invalid. See diagnostics for recovery.";
        C<TextBox>("DiagnosticsBox").Text = string.Join('\n', c.Diagnostics.Select(d => $"{d.Code}: {d.Message}"));
    }
    void ShowSelection()
    {
        Model.SelectedResult = C<ListBox>("ResultsList").SelectedItem as SearchResult;
        if (Model.SelectedResult is not { } result)
            return;
        var c = result.Candidate;
        try
        {
            details = $"Assembly: {c.Assembly.Path}\nSHA-256: {c.Assembly.Sha256}\nMVID: {c.Assembly.Mvid}\nToken: 0x{c.Token:X8}\nCoordinate: {c.Coordinate}\n\n" + ContextReader.Read(c);
            C<TextBox>("ContextBox").Text = details;
            C<Button>("CopyButton").IsEnabled = true;
        }
        catch (Exception e) { Notice(e.Message); C<TextBox>("ContextBox").Text = "Context unavailable: " + e.Message; C<Button>("CopyButton").IsEnabled = false; }
    }
    void Notice(string text) => C<TextBox>("DiagnosticsBox").Text = text;
    async Task OpenFiles()
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new()
            {
                Title = "Open .NET assemblies",
                AllowMultiple = true,
                FileTypeFilter = [new(".NET assemblies") { Patterns = ["*.dll", "*.exe"] }, FilePickerFileTypes.All]
            });
            LoadPaths(files.Select(f => f.TryGetLocalPath()).OfType<string>());
            await SearchAsync(true);
        }
        catch (Exception e) { Notice(e.Message); }
    }
    async Task ExportResults()
    {
        try
        {
            if (Model.Controller.Summary is not { } summary)
                return;
            var json = SearchExport.Serialize(Model.Query, Model.Results.ToArray(), summary);
            var file = await StorageProvider.SaveFilePickerAsync(new()
            {
                Title = "Export search results",
                SuggestedFileName = "assembly-search-results.json",
                DefaultExtension = "json"
            });
            if (file == null)
                return;
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
        }
        catch (Exception e) { Notice(e.Message); }
    }
}
