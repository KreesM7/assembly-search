using System.Globalization;
using Avalonia.Data.Converters;
namespace AssemblySearch.Desktop.Presentation;

public sealed class FileNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string path ? Path.GetFileName(path) : value;
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
