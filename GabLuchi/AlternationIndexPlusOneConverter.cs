using System;
using System.Globalization;
using System.Windows.Data;

namespace GabLuchi;

public class AlternationIndexPlusOneConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return (value is int index) ? (index + 1).ToString(CultureInfo.InvariantCulture) : "1";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}