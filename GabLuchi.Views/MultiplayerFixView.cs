using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace GabLuchi.Views;

public partial class MultiplayerFixView : UserControl, IComponentConnector
{
	public MultiplayerFixView(ViewModels.MultiplayerFixViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
	}

	private void SearchBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			if (DataContext is System.ComponentModel.INotifyPropertyChanged inpc)
			{
				var prop = DataContext.GetType().GetProperty("SearchCommand");
				if (prop?.GetValue(DataContext) is ICommand cmd)
				{
					cmd.Execute(null);
				}
			}
		}
	}
}
