using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class OlderVersionView : UserControl, IComponentConnector
{
	public OlderVersionView(OlderVersionViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}
}
