using System.Globalization;
using System.Windows.Data;

namespace Danawa.Client.Converters;

/// <summary>원 단위 정수 → "1,900" (만원 단위). 관리자 차량 표의 기본가(만원) 열.</summary>
public sealed class WonToManwonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int won ? (won / 10_000).ToString("N0", culture) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
