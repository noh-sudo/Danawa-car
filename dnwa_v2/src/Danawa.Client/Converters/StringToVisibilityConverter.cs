using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Danawa.Client.Converters;

/// <summary>빈 문자열/null이면 Collapsed, 값이 있으면 Visible, 에러 메시지 표시용 — ConverterParameter=invert면 반대(검색어가 비었을 때만 돋보기 버튼 보이기)</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool hasText = !string.IsNullOrEmpty(value as string);
        if (parameter as string == "invert")
        {
            hasText = !hasText;
        }

        return hasText ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
