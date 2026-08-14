using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class HomeView : UserControl, IComponentConnector
{
	private readonly HomeViewModel _viewModel;

	public HomeView(HomeViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = (_viewModel = viewModel);
		base.Loaded += async delegate
		{
			await _viewModel.LoadAsync();
		};
	}
}
