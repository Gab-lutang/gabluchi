using System;
using System.Globalization;
using System.Windows.Data;
using GabLuchi.Services;

namespace GabLuchi.Views;

public class UrlToHosterConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string url)
			return SteamRipService.GetHosterName(url);
		return value?.ToString() ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
