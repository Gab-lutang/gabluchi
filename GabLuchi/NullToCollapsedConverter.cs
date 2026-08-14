using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GabLuchi;

public class NullToCollapsedConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		bool flag = ((value == null || (value is string text && text == "")) ? true : false);
		return flag ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
