using System.Globalization;
using System.Windows.Data;

namespace Danawa.Client.Converters;

/// <summary>true -> 채워진 하트, false -> 빈 하트.</summary>
public sealed class FavoriteHeartConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "♥" : "♡";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
