using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using GabLuchi.Models;

namespace GabLuchi.Views;

public partial class MultiplayerFixView : UserControl, IComponentConnector
{
	public MultiplayerFixView(ViewModels.MultiplayerFixViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	private void SearchBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			if (DataContext is System.ComponentModel.INotifyPropertyChanged inpc)
			{
				var prop = DataContext.GetType().GetProperty("SearchCommand");
				if (prop?.GetValue(DataContext) is ICommand cmd)
				{
					cmd.Execute(null);
				}
			}
		}
	}

	private void GameSearchBox_GotFocus(object sender, RoutedEventArgs e)
	{
		if (DataContext is ViewModels.MultiplayerFixViewModel vm && vm.GameSearchResults.Count > 0)
		{
			vm.IsGamePickerOpen = true;
		}
	}

	private void GameSearchBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (DataContext is ViewModels.MultiplayerFixViewModel vm)
		{
			vm.IsGamePickerOpen = false;
		}
	}

	private void GameResult_Click(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement fe && fe.Tag is GameInfo game && DataContext is ViewModels.MultiplayerFixViewModel vm)
		{
			vm.SelectGameCmd.Execute(game);
		}
	}
}
