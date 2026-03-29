using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Devon.Editor.ViewModels;

namespace Devon.Editor.Converters;

/// <summary>
/// Shows target room only for Exit actions
/// </summary>
public class ActionTypeToTargetVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RoomActionEntryType type)
        {
            return type == RoomActionEntryType.Exit ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
