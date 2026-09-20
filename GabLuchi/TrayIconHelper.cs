using System;
using System.Windows;
using System.Windows.Forms;
using GabLuchi.Services;
using Application = System.Windows.Application;

namespace GabLuchi;

public static class TrayIconHelper
{
	private static NotifyIcon? _icon;

	public static event Action? ShowRequested;

	public static event Action? ExitRequested;

	public static void Initialize()
	{
		if (_icon != null) return;
		_icon = new NotifyIcon();
		_icon.Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico"));
		_icon.Text = "GabLuchi";
		_icon.Visible = true;

		var menu = new ContextMenuStrip();
		menu.Items.Add("Show GabLuchi", null, (_, _) => ShowRequested?.Invoke());
		menu.Items.Add("-");
		menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
		_icon.ContextMenuStrip = menu;

		_icon.DoubleClick += (_, _) => ShowRequested?.Invoke();
	}

	public static void Dispose()
	{
		if (_icon != null)
		{
			_icon.Visible = false;
			_icon.Dispose();
			_icon = null;
		}
	}
}
