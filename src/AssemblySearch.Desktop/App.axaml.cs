using Avalonia;using Avalonia.Controls.ApplicationLifetimes;using Avalonia.Markup.Xaml;
namespace AssemblySearch.Desktop;
public partial class App:Application {
 public override void Initialize()=>AvaloniaXamlLoader.Load(this);
 public override void OnFrameworkInitializationCompleted(){if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop){var window=new MainWindow();desktop.MainWindow=window;window.LoadPaths(desktop.Args?.Where(File.Exists)??[]);}base.OnFrameworkInitializationCompleted();}
}
