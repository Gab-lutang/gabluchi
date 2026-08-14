using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class ManageView : UserControl, IComponentConnector, IStyleConnector
{
	private readonly ManageViewModel _viewModel;

	public ManageView(ManageViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = (_viewModel = viewModel);
		_viewModel.ScrollToTop = ScrollGridToTop;
		base.Loaded += async delegate
		{
			await _viewModel.LoadAsync();
		};
	}

	private void ScrollGridToTop()
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)delegate
		{
			FindDescendant<ScrollViewer>((DependencyObject)(object)TileScroller)?.ScrollToTop();
		}, (DispatcherPriority)4, Array.Empty<object>());
	}

	private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
	{
		int childrenCount = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < childrenCount; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			T val = (T)(object)((child is T) ? child : null);
			if (val != null)
			{
				return val;
			}
			T val2 = FindDescendant<T>(child);
			if (val2 != null)
			{
				return val2;
			}
		}
		return default(T);
	}

	private void Tile_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is LuaTileViewModel tile)
		{
			_viewModel.ResolveTile(tile);
		}
	}

	private void Tile_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: LuaTileViewModel dataContext })
		{
			_viewModel.ResolveTile(dataContext);
		}
	}

	private void Tile_Click(object sender, MouseButtonEventArgs e)
	{
		object originalSource = e.OriginalSource;
		DependencyObject val = (DependencyObject)((originalSource is DependencyObject) ? originalSource : null);
		if ((val == null || FindAncestor<ButtonBase>(val) == null) && sender is FrameworkElement { DataContext: LuaTileViewModel dataContext })
		{
			if (_viewModel.IsSelecting)
			{
				dataContext.IsSelected = !dataContext.IsSelected;
			}
			else
			{
				_viewModel.OpenDetailCommand.Execute(dataContext);
			}
		}
	}

	private void ToggleSelect_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: LuaTileViewModel dataContext })
		{
			dataContext.IsSelected = !dataContext.IsSelected;
		}
	}

	private void Scrim_Click(object sender, MouseButtonEventArgs e)
	{
		_viewModel.CloseDetailCommand.Execute(null);
	}

	private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
	{
		while (current != null)
		{
			T val = (T)(object)((current is T) ? current : null);
			if (val != null)
			{
				return val;
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return default(T);
	}
}
