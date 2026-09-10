using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class MultiplayerFixViewModel : ObservableObject
{
	private readonly MultiplayerFixService _service;
	private readonly ToastService _toast;

	private string _searchText = "";
	private bool _isBusy;
	private string _statusMessage = "";

	public string SearchText
	{
		get => _searchText;
		set { if (SetProperty(ref _searchText, value)) OnPropertyChanged(nameof(CanSearch)); }
	}

	public bool IsBusy
	{
		get => _isBusy;
		set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(CanSearch)); }
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	public bool CanSearch => !IsBusy && !string.IsNullOrWhiteSpace(SearchText);

	public ObservableCollection<MultiplayerFixResult> Results { get; } = new ObservableCollection<MultiplayerFixResult>();

	public ICommand SearchCmd => searchCommand ?? (searchCommand = new AsyncRelayCommand(Search));
	private AsyncRelayCommand? searchCommand;

	public ICommand OpenInBrowserCmd => openInBrowserCommand ?? (openInBrowserCommand = new RelayCommand<MultiplayerFixResult>(OpenInBrowser));
	private RelayCommand<MultiplayerFixResult>? openInBrowserCommand;

	public ICommand SearchOnSiteCmd => searchOnSiteCommand ?? (searchOnSiteCommand = new RelayCommand(SearchOnSite));
	private RelayCommand? searchOnSiteCommand;

	public MultiplayerFixViewModel(MultiplayerFixService service, ToastService toast)
	{
		_service = service;
		_toast = toast;
	}

	private async Task Search()
	{
		if (IsBusy || string.IsNullOrWhiteSpace(SearchText))
		{
			return;
		}
		IsBusy = true;
		StatusMessage = Strings.MultiplayerFix_Searching;
		Results.Clear();
		try
		{
			List<MultiplayerFixResult> results = await _service.SearchAsync(SearchText.Trim());
			if (results.Count == 0)
			{
				StatusMessage = Strings.MultiplayerFix_NoResults;
			}
			else
			{
				StatusMessage = string.Format(Strings.MultiplayerFix_Found, results.Count);
				foreach (MultiplayerFixResult result in results)
				{
					Results.Add(result);
				}
			}
		}
		catch (Exception ex)
		{
			StatusMessage = "Error: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void OpenInBrowser(MultiplayerFixResult result)
	{
		MultiplayerFixService.OpenInBrowser(result.Url);
	}

	private void SearchOnSite()
	{
		string query = string.IsNullOrWhiteSpace(SearchText) ? "" : Uri.EscapeDataString(SearchText.Trim());
		MultiplayerFixService.OpenInBrowser("https://online-fix.me/index.php?do=search&subaction=search&story=" + query);
	}
}
