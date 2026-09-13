using System;
using System.CodeDom.Compiler;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.Models;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class GameHealthView : UserControl, IComponentConnector
{
	private readonly GameHealthViewModel _viewModel;

	public GameHealthView(GameHealthViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = (_viewModel = viewModel);
	}

	private void FixIssue_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is HealthIssue issue)
		{
			_ = FixIssueCore(_viewModel, issue);
		}
	}

	private static async System.Threading.Tasks.Task FixIssueCore(GameHealthViewModel vm, HealthIssue issue)
	{
		if (issue.FixAction == null)
			return;

		try
		{
			await issue.FixAction();
		}
		catch
		{
		}
	}
}
