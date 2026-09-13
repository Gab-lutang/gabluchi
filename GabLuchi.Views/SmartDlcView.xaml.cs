using System.Windows.Controls;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class SmartDlcView : UserControl
{
	public SmartDlcView(SmartDlcViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
	}
}
