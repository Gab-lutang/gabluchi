using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class ModeView : UserControl, IComponentConnector
{
	public ModeView(ModeViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		base.Loaded += async delegate
		{
			await viewModel.LoadAsync();
		};
	}
}
