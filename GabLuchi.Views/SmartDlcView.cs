using System.CodeDom.Compiler;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class SmartDlcView : UserControl, IComponentConnector
{
	public SmartDlcView(SmartDlcViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}
}
