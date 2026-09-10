using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class DownloadGamesView : UserControl, IComponentConnector
{
	public DownloadGamesView(DownloadGamesViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	private DownloadGamesViewModel Vm => (DownloadGamesViewModel)DataContext;

	private void BrowseInstallDir_Click(object sender, RoutedEventArgs e)
	{
		System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog
		{
			ShowNewFolderButton = false
		};
		if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			Vm.InstallDir = dlg.SelectedPath;
		}
	}

	private async void Search_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.CanSearch)
		{
			await Vm.Search();
		}
	}

	private void OpenMirror_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is string url)
		{
			Vm.OpenMirror(url);
		}
	}

	private void ExtractDownloaded_Click(object sender, RoutedEventArgs e)
	{
		Vm.ExtractDownloaded();
	}
}
