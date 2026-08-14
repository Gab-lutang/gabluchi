using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class PluginView : UserControl, IComponentConnector
{
	public PluginView(PluginViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		base.Loaded += async delegate
		{
			await viewModel.LoadAsync();
		};
	}
}
