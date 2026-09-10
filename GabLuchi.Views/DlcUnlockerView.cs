using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class DlcUnlockerView : UserControl, IComponentConnector
{
	public DlcUnlockerView(DlcUnlockerViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	private DlcUnlockerViewModel Vm => (DlcUnlockerViewModel)DataContext;

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

	private async void DetectPlatform_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.CanDetect)
		{
			await Vm.DetectPlatform();
		}
	}

	private async void FetchDlcIds_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.CanFetchDlcs)
		{
			await Vm.FetchDlcIds();
		}
	}

	private async void Install_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.CanInstall)
		{
			await Vm.Install();
		}
	}

	private async void Uninstall_Click(object sender, RoutedEventArgs e)
	{
		if (Vm.CanUninstall)
		{
			await Vm.Uninstall();
		}
	}
}
