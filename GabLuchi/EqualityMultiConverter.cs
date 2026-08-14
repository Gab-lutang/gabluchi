using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GabLuchi;

public class EqualityMultiConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
	{
		if (values == null || values.Length < 2 || values.Any((object v) => v == null))
		{
			return false;
		}
		long first = System.Convert.ToInt64(values[0]);
		return values.Skip(1).All((object v) => System.Convert.ToInt64(v) == first);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
