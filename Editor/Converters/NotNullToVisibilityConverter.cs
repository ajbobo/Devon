using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Devon.Editor.Converters;

/// <summary>
/// Converts non-null to Visible and null to Collapsed
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
