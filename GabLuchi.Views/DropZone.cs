using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.Resources;
using GabLuchi.ViewModels;
using Microsoft.Win32;

namespace GabLuchi.Views;

public partial class DropZone : UserControl, IComponentConnector
{
	private DropInstallViewModel? Vm => base.DataContext as DropInstallViewModel;

	public DropZone()
	{
		InitializeComponent();
	}

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		UpdateDrag(e, entering: true);
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		UpdateDrag(e, entering: true);
	}

	private void OnDragLeave(object sender, DragEventArgs e)
	{
		if (Vm != null)
		{
			Vm.IsDragOver = false;
		}
	}

	private void UpdateDrag(DragEventArgs e, bool entering)
	{
		bool dataPresent = e.Data.GetDataPresent(DataFormats.FileDrop);
		e.Effects = (dataPresent ? DragDropEffects.Copy : DragDropEffects.None);
		if (Vm != null)
		{
			Vm.IsDragOver = dataPresent && entering;
		}
		e.Handled = true;
	}

	private async void OnDrop(object sender, DragEventArgs e)
	{
		if (Vm != null)
		{
			Vm.IsDragOver = false;
		}
		if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] paths && Vm != null)
		{
			await Vm.HandleDropAsync(paths);
		}
	}

	private async void OnBrowseClick(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = Strings.Drop_Picker_Title,
			Multiselect = true,
			Filter = Strings.Drop_Picker_Filter
		};
		if (openFileDialog.ShowDialog() == true && Vm != null)
		{
			await Vm.HandleDropAsync(openFileDialog.FileNames);
		}
	}
}
