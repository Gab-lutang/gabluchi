using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GabLuchi;

public class SourceToBrushConverter : IValueConverter
{
	private static readonly SolidColorBrush Perondepot = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06b6d4"));

	private static readonly SolidColorBrush GabLuchiFixes = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6"));

	private static readonly SolidColorBrush Library = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e"));

	private static readonly SolidColorBrush Other = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7280"));

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return (value as string) switch
		{
			"perondepot" => Perondepot,
			"gabluchi-fixes" => GabLuchiFixes,
			"library" => Library,
			_ => Other,
		};
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
