using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GabLuchi;

public class StatusToBrushConverter : IValueConverter
{
	private static readonly SolidColorBrush Available = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e"));

	private static readonly SolidColorBrush Unavailable = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"));

	private static readonly SolidColorBrush Other = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f97316"));

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return (value as string) switch
		{
			"available" => Available, 
			"unavailable" => Unavailable, 
			_ => Other, 
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
