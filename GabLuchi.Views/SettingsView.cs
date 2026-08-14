using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class SettingsView : UserControl, IComponentConnector
{
	private readonly SettingsViewModel _vm;

	public SettingsView(SettingsViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		_vm = viewModel;
		base.Loaded += delegate
		{
			_vm.OnViewLoaded();
		};
	}
}
