using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using GabLuchi.Services;
using GabLuchi.ViewModels;
using Markdig.Wpf;

namespace GabLuchi.Views;

public partial class FixesView : UserControl, IComponentConnector, IStyleConnector
{
	private readonly FixesViewModel _viewModel;

	public FixesView(FixesViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = (_viewModel = viewModel);
		_viewModel.ScrollToTop = ScrollGridToTop;
		base.Loaded += async delegate
		{
			await _viewModel.LoadAsync();
		};
		base.CommandBindings.Add(new CommandBinding(Commands.Hyperlink, OpenHyperlink));
		AddHandler(Hyperlink.RequestNavigateEvent, (RequestNavigateEventHandler)delegate(object _, RequestNavigateEventArgs e)
		{
			e.Handled = true;
		});
	}

	private static void OpenHyperlink(object sender, ExecutedRoutedEventArgs e)
	{
		Open(e.Parameter as string);
		e.Handled = true;
	}

	private static void Open(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return;
		}
		try
		{
			SteamService.OpenUrl(url);
		}
		catch
		{
		}
	}

	private void Markdown_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (!e.Handled && sender is UIElement uIElement)
		{
			e.Handled = true;
			uIElement.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = UIElement.MouseWheelEvent,
				Source = sender
			});
		}
	}

	private void ScrollGridToTop()
	{
		((DispatcherObject)this).Dispatcher.BeginInvoke((Delegate)(Action)delegate
		{
			FindDescendant<ScrollViewer>((DependencyObject)(object)GameScroller)?.ScrollToTop();
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

	private void Scrim_Click(object sender, MouseButtonEventArgs e)
	{
		_viewModel.CloseDetailCommand.Execute(null);
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		_viewModel.CloseDetailCommand.Execute(null);
	}
}
