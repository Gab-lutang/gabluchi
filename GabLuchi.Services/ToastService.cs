using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace GabLuchi.Services;

public class ToastService
{
	private readonly SnackbarService _snackbar = new SnackbarService();

	private SnackbarPresenter? _presenter;

	public void Attach(SnackbarPresenter presenter)
	{
		_presenter = presenter;
		_snackbar.SetSnackbarPresenter(presenter);
	}

	public void Show(string title, string message, bool error = false)
	{
		Application current = Application.Current;
		Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
		if (val != null)
		{
			if (val.CheckAccess())
			{
				Post();
			}
			else
			{
				val.Invoke((Action)Post);
			}
		}
		void Post()
		{
			_snackbar.Show(title, message, (!error) ? ControlAppearance.Secondary : ControlAppearance.Caution, null, TimeSpan.FromSeconds(3.0));
		}
	}

	public void ShowAction(string title, string message, string actionLabel, Action onAction, bool error = false)
	{
		Application current = Application.Current;
		Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
		if (val != null)
		{
			if (val.CheckAccess())
			{
				Post();
			}
			else
			{
				val.Invoke((Action)Post);
			}
		}
		void Post()
		{
			if (_presenter != null)
			{
				Snackbar obj = new Snackbar(_presenter)
				{
					Title = title,
					Appearance = ((!error) ? ControlAppearance.Secondary : ControlAppearance.Caution),
					Icon = new SymbolIcon(SymbolRegular.ArrowSync24),
					Timeout = TimeSpan.FromDays(1.0),
					IsCloseButtonEnabled = true
				};
				Wpf.Ui.Controls.Button button = new Wpf.Ui.Controls.Button
				{
					Content = actionLabel,
					Appearance = ControlAppearance.Primary,
					Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
					HorizontalAlignment = HorizontalAlignment.Left
				};
				button.Click += delegate
				{
					onAction();
				};
				obj.Content = new StackPanel
				{
					Children = 
					{
						(UIElement)new System.Windows.Controls.TextBlock
						{
							Text = message,
							TextWrapping = TextWrapping.Wrap
						},
						(UIElement)button
					}
				};
				obj.Show();
			}
		}
	}
}
