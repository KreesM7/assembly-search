using Avalonia;using Avalonia.Headless;
[assembly:AvaloniaTestApplication(typeof(AssemblySearch.Desktop.Tests.HeadlessSetup))]
namespace AssemblySearch.Desktop.Tests;
public class HeadlessSetup{public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<AssemblySearch.Desktop.App>().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).UseSkia();}
