using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class DownloadView : UserControl, IComponentConnector
{
	public DownloadView(DownloadViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		viewModel.LoadFeaturedAsync();
		base.Loaded += delegate
		{
			viewModel.SyncFastFetch();
		};
	}

	private void FeaturedStrip_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - (double)e.Delta);
			e.Handled = true;
		}
	}
}
