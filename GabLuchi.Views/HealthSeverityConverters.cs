using System;
using System.Globalization;
using System.Windows.Data;
using GabLuchi.Models;
using Wpf.Ui.Controls;

namespace GabLuchi.Views;

public class HealthSeverityIconConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is HealthSeverity severity)
		{
			return severity switch
			{
				HealthSeverity.Critical => SymbolRegular.ErrorCircle24,
				HealthSeverity.Warning => SymbolRegular.Warning24,
				HealthSeverity.Info => SymbolRegular.Info24,
				_ => SymbolRegular.Info24
			};
		}
		return SymbolRegular.Info24;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}

public class HealthSeverityColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is HealthSeverity severity)
		{
			return severity switch
			{
				HealthSeverity.Critical => "#ef4444",
				HealthSeverity.Warning => "#eab308",
				HealthSeverity.Info => "#3b82f6",
				_ => "#9ca3af"
			};
		}
		return "#9ca3af";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
