using System.Globalization;
using System.Windows.Data;

namespace InstaType.Views;

/// <summary>Counts whitespace-delimited words in a string for the history list.</summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class WordCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return "0";
        return s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
