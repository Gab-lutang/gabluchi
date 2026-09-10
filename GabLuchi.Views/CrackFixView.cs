using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.Models;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class CrackFixView : UserControl, IComponentConnector
{
	public CrackFixView(CrackFixViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	private CrackFixViewModel Vm => (CrackFixViewModel)DataContext;

	private void BrowseGameDir_Click(object sender, RoutedEventArgs e)
	{
		System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog
		{
			ShowNewFolderButton = false
		};
		if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			Vm.GameDir = dlg.SelectedPath;
		}
	}

	private async void Search_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.CanSearch)
		{
			await Vm.Search();
		}
	}

	private async void DownloadAndApply_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is CrackFixEntry entry)
		{
			await Vm.DownloadAndApply(entry);
		}
	}

	private void CancelDownload_Click(object sender, RoutedEventArgs e)
	{
		Vm.CancelDownload();
	}
}
