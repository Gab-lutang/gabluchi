using System.Windows;
using System.Windows.Controls;
using GabLuchi.Models;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class GameHealthView : UserControl
{
	public GameHealthView()
	{
		InitializeComponent();
	}

	private void FixIssue_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && btn.Tag is HealthIssue issue && DataContext is GameHealthViewModel vm)
		{
			_ = FixIssueCore(vm, issue);
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
