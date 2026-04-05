using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using EventScanner.Models;

namespace EventScanner.Helpers;

/// <summary>
/// Converts a Severity value to a colored brush for severity badges.
/// Each severity level gets a distinct, recognizable color.
/// </summary>
public sealed class SeverityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Severity severity)
            return new SolidColorBrush(Colors.White);

        return severity switch
        {
            Severity.Critical => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            Severity.High => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
            Severity.Medium => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            Severity.Low => new SolidColorBrush(Color.FromRgb(0, 180, 216)),
            Severity.Informational => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            _ => new SolidColorBrush(Colors.White)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a Severity value to a semi-transparent brush for badge backgrounds.
/// </summary>
public sealed class SeverityToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Severity severity)
            return new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));

        return severity switch
        {
            Severity.Critical => new SolidColorBrush(Color.FromArgb(50, 220, 53, 69)),
            Severity.High => new SolidColorBrush(Color.FromArgb(50, 255, 140, 0)),
            Severity.Medium => new SolidColorBrush(Color.FromArgb(40, 255, 193, 7)),
            Severity.Low => new SolidColorBrush(Color.FromArgb(35, 0, 180, 216)),
            Severity.Informational => new SolidColorBrush(Color.FromArgb(30, 108, 117, 125)),
            _ => new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converts a boolean to a Thickness for subtle scale-like hover effects.
/// </summary>
/// <summary>
/// Hides a control when the bound string is null or whitespace (e.g. session hint).
/// </summary>
public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrWhiteSpace(s)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true)
            return new Thickness(1);
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
