using System;
using System.Globalization;
using System.Windows.Data;

namespace GabLuchi.Views;

public class SmartDlcPlatformLabelConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is string platform && !string.IsNullOrEmpty(platform) ? $"Platform: {platform}" : "Platform: unknown";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SmartDlcUnlockerLabelConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is string name && !string.IsNullOrEmpty(name) ? $"Recommended: {name}" : "No unlocker available";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SmartDlcCountLabelConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int count)
			return count == 0 ? "No DLC" : $"{count} DLC(s)";
		return "No DLC";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
