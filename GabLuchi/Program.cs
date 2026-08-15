using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using GabLuchi.Services;
using Velopack;

namespace GabLuchi;

public static class Program
{
	private const string ShowWindowEventName = "GabLuchi.ShowWindow";

	internal static EventWaitHandle? ShowWindowSignal;

	private const string EnableTrayLockEventName = "GabLuchi.EnableTrayLock";

	internal static EventWaitHandle? EnableTrayLockSignal;

	private const string RecheckUpdatesEventName = "GabLuchi.RecheckUpdates";

	internal static EventWaitHandle? RecheckUpdatesSignal;

	internal static string? StartupUrl;

	internal static bool StartMinimized;

	internal static bool SessionTrayLock;

	internal static bool FirstRun;

	private static readonly string[] SupportedLanguages = new string[30]
	{
		"en", "zh-Hans", "zh-Hant", "ja", "ko", "es", "es-419", "pt-BR", "pt-PT", "fr",
		"de", "it", "nl", "pl", "ru", "uk", "tr", "ar", "cs", "hu",
		"ro", "el", "bg", "th", "vi", "id", "da", "fi", "nb", "sv"
	};

	[STAThread]
	public static void Main(string[] args)
	{
		VelopackApp.Build().OnFirstRun(delegate
		{
			FirstRun = true;
		}).Run();
		ApplyUiCulture();
		ProtocolService.Register();
		string text = null;
		bool flag = false;
		bool flag2 = false;
		if (args != null && args.Length > 0)
		{
			foreach (string text2 in args)
			{
				if (text2.StartsWith("gabluchi://", StringComparison.OrdinalIgnoreCase))
				{
					text = text2;
				}
				else if (text2.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
				}
				else if (text2.Equals("--tray-locked", StringComparison.OrdinalIgnoreCase))
				{
					flag2 = true;
				}
			}
		}
		bool createdNew;
		using (new Mutex(initiallyOwned: true, "GabLuchi.SingleInstance", out createdNew))
		{
			if (!createdNew)
			{
				if (text != null)
				{
					ProtocolService.WritePending(text);
				}
				if (!flag)
				{
					try
					{
						if (EventWaitHandle.TryOpenExisting("GabLuchi.ShowWindow", out EventWaitHandle result))
						{
							result.Set();
							result.Dispose();
						}
					}
					catch
					{
					}
				}
				if (!flag2)
				{
					return;
				}
				try
				{
					if (EventWaitHandle.TryOpenExisting("GabLuchi.EnableTrayLock", out EventWaitHandle result2))
					{
						result2.Set();
						result2.Dispose();
					}
				}
				catch
				{
				}
				try
				{
					if (EventWaitHandle.TryOpenExisting("GabLuchi.RecheckUpdates", out EventWaitHandle result3))
					{
						result3.Set();
						result3.Dispose();
					}
					return;
				}
				catch
				{
					return;
				}
			}
			StartupUrl = text;
			StartMinimized = flag;
			SessionTrayLock = flag2;
			ShowWindowSignal = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, "GabLuchi.ShowWindow");
			EnableTrayLockSignal = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, "GabLuchi.EnableTrayLock");
			RecheckUpdatesSignal = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, "GabLuchi.RecheckUpdates");
			App app = new App();
			app.InitializeComponent();
			app.Run();
			ShowWindowSignal.Dispose();
			EnableTrayLockSignal.Dispose();
			RecheckUpdatesSignal.Dispose();
		}
	}

	private static void ApplyUiCulture()
	{
		try
		{
			CultureInfo cultureInfo = (CultureInfo.DefaultThreadCurrentCulture = (CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(ResolveLanguageTag())));
			Thread.CurrentThread.CurrentUICulture = cultureInfo;
			Thread.CurrentThread.CurrentCulture = cultureInfo;
			FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), (PropertyMetadata)(object)new FrameworkPropertyMetadata((object)XmlLanguage.GetLanguage(cultureInfo.IetfLanguageTag)));
		}
		catch
		{
		}
	}

	private static string ResolveLanguageTag()
	{
		string text = ReadSavedLanguage();
		if (text != null && IsSupported(text))
		{
			return text;
		}
		return MatchOsLanguage(CultureInfo.InstalledUICulture);
	}

	internal static string MatchOsLanguage(CultureInfo os)
	{
		if (IsSupported(os.Name))
		{
			return os.Name;
		}
		string twoLetterISOLanguageName = os.TwoLetterISOLanguageName;
		bool flag;
		switch (twoLetterISOLanguageName)
		{
		case "zh":
			if (os.Name.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return "zh-Hant";
			}
			if (os.Name.IndexOf("Hans", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return "zh-Hans";
			}
			switch (os.Name)
			{
			case "zh-TW":
			case "zh-HK":
			case "zh-MO":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				return "zh-Hans";
			}
			return "zh-Hant";
		case "pt":
			return "pt-PT";
		case "es":
			if (!IsLatinAmericanSpanish(os.Name))
			{
				return "es";
			}
			return "es-419";
		case "nb":
		case "nn":
		case "no":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return "nb";
		}
		if (IsSupported(twoLetterISOLanguageName))
		{
			return twoLetterISOLanguageName;
		}
		return "en";
	}

	private static bool IsLatinAmericanSpanish(string name)
	{
		if (name.StartsWith("es-", StringComparison.OrdinalIgnoreCase))
		{
			return !name.Equals("es-ES", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool IsSupported(string tag)
	{
		return SupportedLanguages.Contains<string>(tag, StringComparer.OrdinalIgnoreCase);
	}

	private static string? ReadSavedLanguage()
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "settings.json");
			if (!File.Exists(path))
			{
				return null;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
			if (jsonDocument.RootElement.TryGetProperty("Language", out var value) && value.ValueKind == JsonValueKind.String)
			{
				string text = value.GetString();
				return string.IsNullOrWhiteSpace(text) ? null : text;
			}
		}
		catch
		{
		}
		return null;
	}
}
