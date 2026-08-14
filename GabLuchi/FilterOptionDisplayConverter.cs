using System;
using System.Globalization;
using System.Windows.Data;
using GabLuchi.Resources;

namespace GabLuchi;

public class FilterOptionDisplayConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		string text = value as string;
		string result;
		switch (text)
		{
		case "Any":
			result = Strings.Manage_Opt_Any;
			goto IL_01e9;
		case "Free":
			result = Strings.Manage_Opt_Free;
			goto IL_01e9;
		case "Paid":
			result = Strings.Manage_Opt_Paid;
			goto IL_01e9;
		case "Hide adult":
			result = Strings.Manage_Content_HideAdult;
			goto IL_01e9;
		case "Adult only":
			result = Strings.Manage_Content_AdultOnly;
			goto IL_01e9;
		case "Recently added":
			result = Strings.Manage_Sort_RecentlyAdded;
			goto IL_01e9;
		case "Name (A–Z)":
			result = Strings.Manage_Sort_NameAZ;
			goto IL_01e9;
		case "Release date (newest)":
			result = Strings.Manage_Sort_ReleaseNewest;
			goto IL_01e9;
		case "Metacritic":
			result = Strings.Manage_Sort_Metacritic;
			goto IL_01e9;
		case "Most reviewed":
			result = Strings.Manage_Sort_MostReviewed;
			goto IL_01e9;
		case "All":
			result = Strings.Manage_PageSize_All;
			goto IL_01e9;
		default:
			result = text;
			goto IL_01e9;
		case null:
			{
				return value;
			}
			IL_01e9:
			return result;
		}
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
