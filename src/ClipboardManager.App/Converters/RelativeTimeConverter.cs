using System.Globalization;
using System.Windows.Data;

namespace ClipboardManager.App.Converters;

/// <summary>
/// Converts a UTC DateTime into human-friendly relative time text (e.g. "Just now", "5m ago").
/// </summary>
public class RelativeTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
        {
            return string.Empty;
        }

        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (timeSpan.TotalSeconds < 30)
        {
            return "Just now";
        }

        if (timeSpan.TotalMinutes < 60)
        {
            int minutes = Math.Max(1, (int)timeSpan.TotalMinutes);
            return $"{minutes}m ago";
        }

        if (timeSpan.TotalHours < 24)
        {
            int hours = (int)timeSpan.TotalHours;
            return $"{hours}h ago";
        }

        if (timeSpan.TotalDays < 7)
        {
            int days = (int)timeSpan.TotalDays;
            return $"{days}d ago";
        }

        return dateTime.ToLocalTime().ToString("MMM dd, yyyy");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
