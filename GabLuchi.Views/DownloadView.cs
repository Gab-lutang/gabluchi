using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class DownloadView : UserControl, IComponentConnector
{
	private readonly DispatcherTimer _spotlightTimer;

	public DownloadView(DownloadViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		viewModel.LoadFeaturedAsync();
		_spotlightTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(8.0)
		};
		_spotlightTimer.Tick += delegate
		{
			if (viewModel.ShowFeatured && !SpotlightFrame.IsMouseOver)
			{
				viewModel.SpotlightNextCommand.Execute(null);
			}
		};
		base.Loaded += delegate
		{
			viewModel.SyncFastFetch();
			_spotlightTimer.Start();
		};
		base.Unloaded += delegate
		{
			_spotlightTimer.Stop();
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