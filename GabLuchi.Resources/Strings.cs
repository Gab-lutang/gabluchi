using System.Globalization;
using System.Resources;

namespace GabLuchi.Resources;

public static class Strings
{
	private static readonly ResourceManager Rm = new ResourceManager("GabLuchi.Resources.Strings", typeof(Strings).Assembly);

	public static string Nav_Home => Get("Nav_Home");

	public static string Nav_Add => Get("Nav_Add");

	public static string Nav_Manage => Get("Nav_Manage");

	public static string Nav_Mode => Get("Nav_Mode");

	public static string Nav_Fixes => Get("Nav_Fixes");

	public static string Nav_RestartSteam => Get("Nav_RestartSteam");

	public static string Nav_Settings => Get("Nav_Settings");

	public static string Nav_SignInDiscord => Get("Nav_SignInDiscord");

	public static string Nav_WaitingForBrowser => Get("Nav_WaitingForBrowser");

	public static string Nav_Footer_Guest => Get("Nav_Footer_Guest");

	public static string Nav_Footer_LoggedIn => Get("Nav_Footer_LoggedIn");

	public static string Main_RestartSteam_Ask => Get("Main_RestartSteam_Ask");

	public static string Settings_Title => Get("Settings_Title");

	public static string Settings_Section_Account => Get("Settings_Section_Account");

	public static string Settings_Section_General => Get("Settings_Section_General");

	public static string Settings_Section_Steam => Get("Settings_Section_Steam");

	public static string Settings_Section_Install => Get("Settings_Section_Install");

	public static string Settings_SignOut => Get("Settings_SignOut");

	public static string Settings_BrowsingAsGuest => Get("Settings_BrowsingAsGuest");

	public static string Settings_GuestHint => Get("Settings_GuestHint");

	public static string Settings_SignInDiscord => Get("Settings_SignInDiscord");

	public static string Settings_BotCode_Hint => Get("Settings_BotCode_Hint");

	public static string Settings_BotCode_Placeholder => Get("Settings_BotCode_Placeholder");

	public static string Settings_BotCode_Redeem => Get("Settings_BotCode_Redeem");

	public static string Settings_BotCode_Expired => Get("Settings_BotCode_Expired");

	public static string Settings_BotCode_Invalid => Get("Settings_BotCode_Invalid");

	public static string Settings_BotCode_ServerError => Get("Settings_BotCode_ServerError");

	public static string Settings_BotLink_Title => Get("Settings_BotLink_Title");

	public static string Settings_BotLink_Body => Get("Settings_BotLink_Body");

	public static string Settings_BotLink_Dismiss => Get("Settings_BotLink_Dismiss");

	public static string Settings_LoginRequired => Get("Settings_LoginRequired");

	public static string Settings_ManageKeyHintPrefix => Get("Settings_ManageKeyHintPrefix");

	public static string Settings_Section_Hubcap => Get("Settings_Section_Hubcap");

	public static string Settings_HubcapKey => Get("Settings_HubcapKey");

	public static string Settings_HubcapKey_Hint => Get("Settings_HubcapKey_Hint");

	public static string Settings_HubcapKeyPlaceholder => Get("Settings_HubcapKeyPlaceholder");

	public static string Settings_HubcapValidate => Get("Settings_HubcapValidate");

	public static string Settings_HubcapClear => Get("Settings_HubcapClear");

	public static string Settings_HubcapGetKey => Get("Settings_HubcapGetKey");

	public static string Settings_HubcapKeyOk => Get("Settings_HubcapKeyOk");

	public static string Settings_HubcapKeyOkExpiry => Get("Settings_HubcapKeyOkExpiry");

	public static string Settings_HubcapKeyBad => Get("Settings_HubcapKeyBad");

	public static string Settings_HubcapKeyError => Get("Settings_HubcapKeyError");

	public static string Settings_HubcapActive => Get("Settings_HubcapActive");

	public static string Settings_HubcapRefresh => Get("Settings_HubcapRefresh");

	public static string Settings_Section_License => Get("Settings_Section_License");

	public static string Settings_LicenseKey => Get("Settings_LicenseKey");

	public static string Settings_LicenseKey_Hint => Get("Settings_LicenseKey_Hint");

	public static string Settings_LicenseKeyPlaceholder => Get("Settings_LicenseKeyPlaceholder");

	public static string Settings_LicenseValidate => Get("Settings_LicenseValidate");

	public static string Settings_LicenseClear => Get("Settings_LicenseClear");

	public static string Settings_LicenseGetKey => Get("Settings_LicenseGetKey");

	public static string Settings_LicenseDiscord => Get("Settings_LicenseDiscord");

	public static string Settings_LicenseActive => Get("Settings_LicenseActive");

	public static string Settings_LicenseKeyBad => Get("Settings_LicenseKeyBad");

	public static string Settings_LicenseKeyInvalid => Get("Settings_LicenseKeyInvalid");

	public static string Settings_LicenseKeyUsed => Get("Settings_LicenseKeyUsed");

	public static string Settings_LicenseKeyRevoked => Get("Settings_LicenseKeyRevoked");

	public static string Settings_LicenseKeyUnreachable => Get("Settings_LicenseKeyUnreachable");

	public static string Settings_LicenseKeyOwnerMismatch => Get("Settings_LicenseKeyOwnerMismatch");

	public static string Settings_LicenseKeyNeedLogin => Get("Settings_LicenseKeyNeedLogin");

	public static string Settings_LicenseKeyError => Get("Settings_LicenseKeyError");

	public static string Add_Err_LicenseRequired => Get("Add_Err_LicenseRequired");

	public static string Settings_Language => Get("Settings_Language");

	public static string Settings_Language_Hint => Get("Settings_Language_Hint");

	public static string Settings_Language_SystemDefault => Get("Settings_Language_SystemDefault");

	public static string Settings_SteamLocation => Get("Settings_SteamLocation");

	public static string Settings_Change => Get("Settings_Change");

	public static string Settings_Open => Get("Settings_Open");

	public static string Settings_ResetToAuto => Get("Settings_ResetToAuto");

	public static string Settings_AutoUpdateApps => Get("Settings_AutoUpdateApps");

	public static string Settings_AutoUpdateApps_Hint => Get("Settings_AutoUpdateApps_Hint");

	public static string Settings_SteamNotFound => Get("Settings_SteamNotFound");

	public static string Settings_SteamSource_Custom => Get("Settings_SteamSource_Custom");

	public static string Settings_SteamSource_NotFound => Get("Settings_SteamSource_NotFound");

	public static string Settings_SteamSource_Auto => Get("Settings_SteamSource_Auto");

	public static string Settings_SteamWarning_NoExe => Get("Settings_SteamWarning_NoExe");

	public static string Settings_ChooseSteamFolder => Get("Settings_ChooseSteamFolder");

	public static string Lang_Changed_Title => Get("Lang_Changed_Title");

	public static string Lang_Changed_Body => Get("Lang_Changed_Body");

	public static string Lang_Changed_Restart => Get("Lang_Changed_Restart");

	public static string Home_Welcome => Get("Home_Welcome");

	public static string Home_LuasOnBoard => Get("Home_LuasOnBoard");

	public static string Home_LastAdded => Get("Home_LastAdded");

	public static string Home_PluginStatus => Get("Home_PluginStatus");

	public static string Home_RecentlyAdded => Get("Home_RecentlyAdded");

	public static string Home_ViewDetails => Get("Home_ViewDetails");

	public static string Home_GamesOnBoard => Get("Home_GamesOnBoard");

	public static string Home_ViewAll => Get("Home_ViewAll");

	public static string Home_ManageLibrary => Get("Home_ManageLibrary");

	public static string Home_SteamReady => Get("Home_SteamReady");

	public static string Home_SteamMissing => Get("Home_SteamMissing");

	public static string Home_CheckingSteam => Get("Home_CheckingSteam");

	public static string Home_SteamDetected => Get("Home_SteamDetected");

	public static string Home_SteamNotFound => Get("Home_SteamNotFound");

	public static string Home_BrowsingAsGuest => Get("Home_BrowsingAsGuest");

	public static string Home_SignedIn => Get("Home_SignedIn");

	public static string Home_SignedInAs => Get("Home_SignedInAs");

	public static string Home_NoModeSelected => Get("Home_NoModeSelected");

	public static string Home_ModeIs => Get("Home_ModeIs");

	public static string Mode_Title => Get("Mode_Title");

	public static string Mode_Subtitle => Get("Mode_Subtitle");

	public static string Mode_CheckForUpdates => Get("Mode_CheckForUpdates");

	public static string Mode_Active => Get("Mode_Active");

	public static string Mode_Recommended => Get("Mode_Recommended");

	public static string Mode_Confirm_Body => Get("Mode_Confirm_Body");

	public static string Mode_Cancel => Get("Mode_Cancel");

	public static string Mode_CloseSteamContinue => Get("Mode_CloseSteamContinue");

	public static string Mode_Desc_SteamTools => Get("Mode_Desc_SteamTools");

	public static string Mode_Desc_OpenSteamTools => Get("Mode_Desc_OpenSteamTools");

	public static string Mode_Desc_CloudRedirect => Get("Mode_Desc_CloudRedirect");

	public static string Mode_CloudRedirect_Manage => Get("Mode_CloudRedirect_Manage");

	public static string Mode_CloudRedirect_LaunchFailed => Get("Mode_CloudRedirect_LaunchFailed");

	public static string Mode_Experimental => Get("Mode_Experimental");

	public static string Mode_CloudRedirectSupport => Get("Mode_CloudRedirectSupport");

	public static string Mode_CloudRedirect_AddonDesc => Get("Mode_CloudRedirect_AddonDesc");

	public static string Mode_CloudRedirect_Locked => Get("Mode_CloudRedirect_Locked");

	public static string Mode_CloudRedirect_Enable => Get("Mode_CloudRedirect_Enable");

	public static string Mode_CloudRedirect_Disable => Get("Mode_CloudRedirect_Disable");

	public static string Mode_CloudRedirect_Status_Enabled => Get("Mode_CloudRedirect_Status_Enabled");

	public static string Mode_CloudRedirect_Status_Disabled => Get("Mode_CloudRedirect_Status_Disabled");

	public static string Mode_CloudRedirect_Status_NotInstalled => Get("Mode_CloudRedirect_Status_NotInstalled");

	public static string Mode_CloudRedirect_Status_UpdateAvailable => Get("Mode_CloudRedirect_Status_UpdateAvailable");

	public static string Mode_CloudRedirect_Toast_Enabled => Get("Mode_CloudRedirect_Toast_Enabled");

	public static string Mode_CloudRedirect_Toast_Disabled => Get("Mode_CloudRedirect_Toast_Disabled");

	public static string Mode_CloudRedirect_Toast_Updated => Get("Mode_CloudRedirect_Toast_Updated");

	public static string Onboarding_Title => Get("Onboarding_Title");

	public static string Onboarding_SignIn_Blurb => Get("Onboarding_SignIn_Blurb");

	public static string Onboarding_SignedIn => Get("Onboarding_SignedIn");

	public static string Onboarding_Recommended_Title => Get("Onboarding_Recommended_Title");

	public static string Onboarding_Recommended_Detail => Get("Onboarding_Recommended_Detail");

	public static string Onboarding_Plugin_Title => Get("Onboarding_Plugin_Title");

	public static string Onboarding_Plugin_Detail => Get("Onboarding_Plugin_Detail");

	public static string Onboarding_Yes => Get("Onboarding_Yes");

	public static string Onboarding_No => Get("Onboarding_No");

	public static string Onboarding_Go => Get("Onboarding_Go");

	public static string Onboarding_Applying => Get("Onboarding_Applying");

	public static string Mode_Checking => Get("Mode_Checking");

	public static string Mode_StatusUnavailable => Get("Mode_StatusUnavailable");

	public static string Mode_NotActive => Get("Mode_NotActive");

	public static string Mode_NotInstalled => Get("Mode_NotInstalled");

	public static string Mode_UpToDate => Get("Mode_UpToDate");

	public static string Mode_UpdateAvailable => Get("Mode_UpdateAvailable");

	public static string Mode_Btn_Reinstall => Get("Mode_Btn_Reinstall");

	public static string Mode_Btn_Update => Get("Mode_Btn_Update");

	public static string Mode_Btn_Install => Get("Mode_Btn_Install");

	public static string Mode_Btn_Switch => Get("Mode_Btn_Switch");

	public static string Mode_Confirm_Reinstall => Get("Mode_Confirm_Reinstall");

	public static string Mode_Confirm_Switch => Get("Mode_Confirm_Switch");

	public static string Mode_Toast_Updated => Get("Mode_Toast_Updated");

	public static string Mode_Toast_Updated_Restarting => Get("Mode_Toast_Updated_Restarting");

	public static string Mode_Toast_Updated_Start => Get("Mode_Toast_Updated_Start");

	public static string Mode_Toast_InstallFailed => Get("Mode_Toast_InstallFailed");

	public static string Mode_Toast_InstallFailed_Body => Get("Mode_Toast_InstallFailed_Body");

	public static string Drop_Title => Get("Drop_Title");

	public static string Drop_Hint => Get("Drop_Hint");

	public static string Drop_BrowseFiles => Get("Drop_BrowseFiles");

	public static string Drop_Picker_Title => Get("Drop_Picker_Title");

	public static string Drop_Picker_Filter => Get("Drop_Picker_Filter");

	public static string Drop_Nothing => Get("Drop_Nothing");

	public static string Drop_Confirm_Replace => Get("Drop_Confirm_Replace");

	public static string Drop_Confirm_NoChanges => Get("Drop_Confirm_NoChanges");

	public static string Drop_NothingInstalled => Get("Drop_NothingInstalled");

	public static string Drop_Count_Luas => Get("Drop_Count_Luas");

	public static string Drop_Count_Manifests => Get("Drop_Count_Manifests");

	public static string Drop_Result_Installed => Get("Drop_Result_Installed");

	public static string Drop_Result_Failed => Get("Drop_Result_Failed");

	public static string Drop_Result_RestartApply => Get("Drop_Result_RestartApply");

	public static string Common_SearchPlaceholder => Get("Common_SearchPlaceholder");

	public static string Common_Loading => Get("Common_Loading");

	public static string Common_AppId => Get("Common_AppId");

	public static string Common_AppFallback => Get("Common_AppFallback");

	public static string Manage_Title => Get("Manage_Title");

	public static string Manage_Refresh => Get("Manage_Refresh");

	public static string Manage_Filters => Get("Manage_Filters");

	public static string Manage_Filter_Type => Get("Manage_Filter_Type");

	public static string Manage_Filter_Genre => Get("Manage_Filter_Genre");

	public static string Manage_Filter_Year => Get("Manage_Filter_Year");

	public static string Manage_Filter_Price => Get("Manage_Filter_Price");

	public static string Manage_Filter_Content => Get("Manage_Filter_Content");

	public static string Manage_Content_HideAdult => Get("Manage_Content_HideAdult");

	public static string Manage_Content_AdultOnly => Get("Manage_Content_AdultOnly");

	public static string Manage_Filter_SortBy => Get("Manage_Filter_SortBy");

	public static string Manage_ClearFilters => Get("Manage_ClearFilters");

	public static string Manage_Select => Get("Manage_Select");

	public static string Manage_DeleteThisLua => Get("Manage_DeleteThisLua");

	public static string Manage_ResultsPerPage => Get("Manage_ResultsPerPage");

	public static string Manage_Action_ViewDetails => Get("Manage_Action_ViewDetails");

	public static string Manage_Action_Update => Get("Manage_Action_Update");

	public static string Manage_Action_OpenStore => Get("Manage_Action_OpenStore");

	public static string Manage_Action_OpenInSteam => Get("Manage_Action_OpenInSteam");

	public static string Manage_Action_Reveal => Get("Manage_Action_Reveal");

	public static string Manage_Action_CopyAppId => Get("Manage_Action_CopyAppId");

	public static string Manage_Action_RemoveDrm => Get("Manage_Action_RemoveDrm");

	public static string Manage_ActionsHeader => Get("Manage_ActionsHeader");

	public static string Manage_Steamless_Confirm_Title => Get("Manage_Steamless_Confirm_Title");

	public static string Manage_Steamless_Confirm_Body => Get("Manage_Steamless_Confirm_Body");

	public static string Manage_Steamless_Working => Get("Manage_Steamless_Working");

	public static string Manage_Toast_Steamless_Done => Get("Manage_Toast_Steamless_Done");

	public static string Manage_Toast_Goldberg_Applied => Get("Manage_Toast_Goldberg_Applied");

	public static string Manage_Toast_Bypass_Applied => Get("Manage_Toast_Bypass_Applied");

	public static string Manage_Toast_Goldberg_Many => Get("Manage_Toast_Goldberg_Many");

	public static string Manage_Toast_Bypass_Many => Get("Manage_Toast_Bypass_Many");

	public static string Manage_Steamless_NoInstall => Get("Manage_Steamless_NoInstall");

	public static string Manage_Steamless_Failed => Get("Manage_Steamless_Failed");

	public static string Manage_CopyAppIdTip => Get("Manage_CopyAppIdTip");

	public static string Manage_LoadingDepotInfo => Get("Manage_LoadingDepotInfo");

	public static string Manage_MissingHint => Get("Manage_MissingHint");

	public static string Manage_UnknownHint => Get("Manage_UnknownHint");

	public static string Manage_Clear => Get("Manage_Clear");

	public static string Manage_DeleteSelected => Get("Manage_DeleteSelected");

	public static string Manage_SelectAll => Get("Manage_SelectAll");

	public static string Manage_CopySelected => Get("Manage_CopySelected");

	public static string Manage_RemoveDrmSelected => Get("Manage_RemoveDrmSelected");

	public static string Manage_Steamless_Many_Title => Get("Manage_Steamless_Many_Title");

	public static string Manage_Steamless_Many_Body => Get("Manage_Steamless_Many_Body");

	public static string Manage_Toast_Steamless_Many => Get("Manage_Toast_Steamless_Many");

	public static string Manage_Action_RestoreCrack => Get("Manage_Action_RestoreCrack");

	public static string Manage_RestoreCrackSelected => Get("Manage_RestoreCrackSelected");

	public static string Manage_Restore_Confirm_Title => Get("Manage_Restore_Confirm_Title");

	public static string Manage_Restore_Confirm_Body => Get("Manage_Restore_Confirm_Body");

	public static string Manage_Toast_Restore_Done => Get("Manage_Toast_Restore_Done");

	public static string Manage_Restore_NoInstall => Get("Manage_Restore_NoInstall");

	public static string Manage_Restore_Failed => Get("Manage_Restore_Failed");

	public static string Manage_Restore_Many_Title => Get("Manage_Restore_Many_Title");

	public static string Manage_Action_LaunchGame => Get("Manage_Action_LaunchGame");

	public static string Manage_Toast_LaunchGame_Done => Get("Manage_Toast_LaunchGame_Done");

	public static string Manage_Toast_Shortcut_Created => Get("Manage_Toast_Shortcut_Created");

	public static string Manage_Restore_Many_Body => Get("Manage_Restore_Many_Body");

	public static string Manage_Toast_Restore_Many => Get("Manage_Toast_Restore_Many");

	public static string Manage_Toast_Copied_Title => Get("Manage_Toast_Copied_Title");

	public static string Manage_Toast_Copied_Body => Get("Manage_Toast_Copied_Body");

	public static string Manage_LoadingGames => Get("Manage_LoadingGames");

	public static string Manage_Opt_Any => Get("Manage_Opt_Any");

	public static string Manage_Opt_Free => Get("Manage_Opt_Free");

	public static string Manage_Opt_Paid => Get("Manage_Opt_Paid");

	public static string Manage_Sort_RecentlyAdded => Get("Manage_Sort_RecentlyAdded");

	public static string Manage_Sort_NameAZ => Get("Manage_Sort_NameAZ");

	public static string Manage_Sort_ReleaseNewest => Get("Manage_Sort_ReleaseNewest");

	public static string Manage_Sort_Metacritic => Get("Manage_Sort_Metacritic");

	public static string Manage_Sort_MostReviewed => Get("Manage_Sort_MostReviewed");

	public static string Manage_PageSize_All => Get("Manage_PageSize_All");

	public static string Manage_PageLabel => Get("Manage_PageLabel");

	public static string Manage_SelectionLabel => Get("Manage_SelectionLabel");

	public static string Manage_FetchingDetails => Get("Manage_FetchingDetails");

	public static string Manage_Empty_NoMatch => Get("Manage_Empty_NoMatch");

	public static string Manage_Empty_NoLuas => Get("Manage_Empty_NoLuas");

	public static string Manage_Empty_NoSteam => Get("Manage_Empty_NoSteam");

	public static string Manage_Toast_NotFound_Title => Get("Manage_Toast_NotFound_Title");

	public static string Manage_Toast_NotFound_Body => Get("Manage_Toast_NotFound_Body");

	public static string Manage_Toast_Refreshed_Title => Get("Manage_Toast_Refreshed_Title");

	public static string Manage_Toast_Refreshed_Body => Get("Manage_Toast_Refreshed_Body");

	public static string Manage_DepotError => Get("Manage_DepotError");

	public static string Manage_Toggle_InLua => Get("Manage_Toggle_InLua");

	public static string Manage_Toggle_Missing => Get("Manage_Toggle_Missing");

	public static string Manage_Toggle_Unknown => Get("Manage_Toggle_Unknown");

	public static string Manage_Depot => Get("Manage_Depot");

	public static string Manage_SharedDepot => Get("Manage_SharedDepot");

	public static string Manage_DlcName => Get("Manage_DlcName");

	public static string Manage_Delete_Title => Get("Manage_Delete_Title");

	public static string Manage_Delete_Body => Get("Manage_Delete_Body");

	public static string Manage_DeleteMany_Title => Get("Manage_DeleteMany_Title");

	public static string Manage_DeleteMany_Body => Get("Manage_DeleteMany_Body");

	public static string Manage_RemoveFailed_Title => Get("Manage_RemoveFailed_Title");

	public static string Manage_RemoveFailed_File => Get("Manage_RemoveFailed_File");

	public static string Manage_RemoveFailed_Named => Get("Manage_RemoveFailed_Named");

	public static string Manage_RemoveFailed_Count => Get("Manage_RemoveFailed_Count");

	public static string Manage_RestartSteam_Title => Get("Manage_RestartSteam_Title");

	public static string Manage_RestartSteam_Ask => Get("Manage_RestartSteam_Ask");

	public static string Manage_RestartSteam_Failed => Get("Manage_RestartSteam_Failed");

	public static string Fixes_Title => Get("Fixes_Title");

	public static string Fixes_Loading => Get("Fixes_Loading");

	public static string Fixes_Build => Get("Fixes_Build");

	public static string Fixes_Manifest => Get("Fixes_Manifest");

	public static string Fixes_Fix => Get("Fixes_Fix");

	public static string Fixes_Count => Get("Fixes_Count");

	public static string Fixes_Err_Load => Get("Fixes_Err_Load");

	public static string Fixes_Empty_None => Get("Fixes_Empty_None");

	public static string Fixes_SignIn => Get("Fixes_SignIn");

	public static string Fixes_SignInRequired => Get("Fixes_SignInRequired");

	public static string Fixes_Toast_DownloadFailed => Get("Fixes_Toast_DownloadFailed");

	public static string Fixes_Toast_DownloadFailed_Body => Get("Fixes_Toast_DownloadFailed_Body");

	public static string Fixes_Toast_InstallFailed => Get("Fixes_Toast_InstallFailed");

	public static string Fixes_Toast_InstallFailed_Body => Get("Fixes_Toast_InstallFailed_Body");

	public static string Fixes_Toast_FixInstalled => Get("Fixes_Toast_FixInstalled");

	public static string Fixes_Toast_FixInstalled_Restarting => Get("Fixes_Toast_FixInstalled_Restarting");

	public static string Fixes_Toast_FixInstalled_Restart => Get("Fixes_Toast_FixInstalled_Restart");

	public static string Fixes_Toast_GameNotFound => Get("Fixes_Toast_GameNotFound");

	public static string Fixes_Toast_GameNotFound_Body => Get("Fixes_Toast_GameNotFound_Body");

	public static string Fixes_Toast_PartiallyApplied => Get("Fixes_Toast_PartiallyApplied");

	public static string Fixes_Toast_PartiallyApplied_Body => Get("Fixes_Toast_PartiallyApplied_Body");

	public static string Fixes_Toast_FixApplied => Get("Fixes_Toast_FixApplied");

	public static string Fixes_Toast_FixApplied_Body => Get("Fixes_Toast_FixApplied_Body");

	public static string Fixes_Toast_CouldntApply => Get("Fixes_Toast_CouldntApply");

	public static string Fixes_Toast_Refreshed_Title => Get("Fixes_Toast_Refreshed_Title");

	public static string Fixes_Toast_Refreshed_Body => Get("Fixes_Toast_Refreshed_Body");

	public static string Add_Title => Get("Add_Title");

	public static string Add_Subtitle => Get("Add_Subtitle");

	public static string Add_SearchPlaceholder => Get("Add_SearchPlaceholder");

	public static string Add_Featured_TopSellers => Get("Add_Featured_TopSellers");

	public static string Add_Featured_NewReleases => Get("Add_Featured_NewReleases");

	public static string Add_Released => Get("Add_Released");

	public static string Add_Fetch => Get("Add_Fetch");

	public static string Add_Checking => Get("Add_Checking");

	public static string Add_Reveal => Get("Add_Reveal");

	public static string Add_Supporter => Get("Add_Supporter");

	public static string Add_Unlimited => Get("Add_Unlimited");

	public static string Add_RequiresOwnKey => Get("Add_RequiresOwnKey");

	public static string Add_Discord => Get("Add_Discord");

	public static string Add_JoinDiscord => Get("Add_JoinDiscord");

	public static string Add_Download => Get("Add_Download");

	public static string Add_NeedsHubcapKey => Get("Add_NeedsHubcapKey");

	public static string Add_NeedsHubcapKey_Tip => Get("Add_NeedsHubcapKey_Tip");

	public static string Add_Depots => Get("Add_Depots");

	public static string Add_Available => Get("Add_Available");

	public static string Add_Missing => Get("Add_Missing");

	public static string Add_NoKey => Get("Add_NoKey");

	public static string Add_DownloadLua => Get("Add_DownloadLua");

	public static string Add_Generating => Get("Add_Generating");

	public static string Add_DlcHint => Get("Add_DlcHint");

	public static string Add_AlreadyHaveFiles => Get("Add_AlreadyHaveFiles");

	public static string Add_SignIn_Dlc => Get("Add_SignIn_Dlc");

	public static string Add_SignIn_Download => Get("Add_SignIn_Download");

	public static string Add_Err_BaseGame => Get("Add_Err_BaseGame");

	public static string Add_Err_Generic => Get("Add_Err_Generic");

	public static string Add_Err_Download => Get("Add_Err_Download");

	public static string Add_Err_Generate => Get("Add_Err_Generate");

	public static string Add_Confirm_Replace => Get("Add_Confirm_Replace");

	public static string Add_Confirm_NoChanges => Get("Add_Confirm_NoChanges");

	public static string Add_Status_Cancelled => Get("Add_Status_Cancelled");

	public static string Add_Status_InstallFailed => Get("Add_Status_InstallFailed");

	public static string Add_Status_AddedManifests => Get("Add_Status_AddedManifests");

	public static string Add_Status_AddedFetch => Get("Add_Status_AddedFetch");

	public static string Add_FastFetch => Get("Add_FastFetch");

	public static string Add_FastFetch_Hint => Get("Add_FastFetch_Hint");

	public static string Add_FastFetch_NoSource => Get("Add_FastFetch_NoSource");

	public static string Add_FastFetch_Via => Get("Add_FastFetch_Via");

	public static string Confirm_OpenSteamDb => Get("Confirm_OpenSteamDb");

	public static string Confirm_Shared => Get("Confirm_Shared");

	public static string Confirm_ReviewChanges => Get("Confirm_ReviewChanges");

	public static string Confirm_Adding => Get("Confirm_Adding");

	public static string Confirm_Removing => Get("Confirm_Removing");

	public static string Confirm_Skip => Get("Confirm_Skip");

	public static string Confirm_Replace => Get("Confirm_Replace");

	public static string Plugin_Title => Get("Plugin_Title");

	public static string Plugin_Subtitle => Get("Plugin_Subtitle");

	public static string Plugin_CardTitle => Get("Plugin_CardTitle");

	public static string Plugin_Badge_UpdateAvailable => Get("Plugin_Badge_UpdateAvailable");

	public static string Plugin_Row_InstalledVersion => Get("Plugin_Row_InstalledVersion");

	public static string Plugin_Row_LatestVersion => Get("Plugin_Row_LatestVersion");

	public static string Plugin_Row_Frontend => Get("Plugin_Row_Frontend");

	public static string Plugin_Row_Loader => Get("Plugin_Row_Loader");

	public static string Plugin_Millennium_Coexisting => Get("Plugin_Millennium_Coexisting");

	public static string Plugin_Footer => Get("Plugin_Footer");

	public static string Plugin_About_Header => Get("Plugin_About_Header");

	public static string Plugin_Feature_StoreButton => Get("Plugin_Feature_StoreButton");

	public static string Plugin_Feature_AutoUpdate => Get("Plugin_Feature_AutoUpdate");

	public static string Plugin_Feature_Millennium => Get("Plugin_Feature_Millennium");

	public static string Plugin_Btn_Install => Get("Plugin_Btn_Install");

	public static string Plugin_Btn_Update => Get("Plugin_Btn_Update");

	public static string Plugin_Btn_Reinstall => Get("Plugin_Btn_Reinstall");

	public static string Plugin_Btn_Uninstall => Get("Plugin_Btn_Uninstall");

	public static string Plugin_Btn_CheckForUpdates => Get("Plugin_Btn_CheckForUpdates");

	public static string Plugin_Checking => Get("Plugin_Checking");

	public static string Plugin_Status_Installed => Get("Plugin_Status_Installed");

	public static string Plugin_Status_NotInstalled => Get("Plugin_Status_NotInstalled");

	public static string Plugin_Status_UpToDate => Get("Plugin_Status_UpToDate");

	public static string Plugin_Status_OutOfDate => Get("Plugin_Status_OutOfDate");

	public static string Plugin_Version_Unknown => Get("Plugin_Version_Unknown");

	public static string Plugin_Version_Offline => Get("Plugin_Version_Offline");

	public static string Plugin_Status_OfflineCheck => Get("Plugin_Status_OfflineCheck");

	public static string Plugin_Status_Port8080Busy => Get("Plugin_Status_Port8080Busy");

	public static string Plugin_Confirm_RestartBody => Get("Plugin_Confirm_RestartBody");

	public static string Plugin_Confirm_RestartCaption => Get("Plugin_Confirm_RestartCaption");

	public static string Plugin_Toast_Title => Get("Plugin_Toast_Title");

	public static string Plugin_Toast_Installed => Get("Plugin_Toast_Installed");

	public static string Plugin_Toast_InstallFailed => Get("Plugin_Toast_InstallFailed");

	public static string Plugin_Toast_Removed => Get("Plugin_Toast_Removed");

	public static string Plugin_Toast_UninstallFailed => Get("Plugin_Toast_UninstallFailed");

	public static string Plugin_Toast_UpdateAvailable => Get("Plugin_Toast_UpdateAvailable");

	public static string Plugin_Toast_UpToDate => Get("Plugin_Toast_UpToDate");

	public static string Plugin_Err_SteamNotFound => Get("Plugin_Err_SteamNotFound");

	public static string Plugin_Err_GithubUnreachable => Get("Plugin_Err_GithubUnreachable");

	public static string Plugin_Err_MissingAssets => Get("Plugin_Err_MissingAssets");

	public static string Plugin_Err_VerifyFailed => Get("Plugin_Err_VerifyFailed");

	public static string Plugin_Err_NoGabLuchiJs => Get("Plugin_Err_NoGabLuchiJs");

	public static string Nav_OlderVersions => Get("Nav_OlderVersions");

	public static string OlderVersion_Title => Get("OlderVersion_Title");

	public static string OlderVersion_AppId => Get("OlderVersion_AppId");

	public static string OlderVersion_DepotId => Get("OlderVersion_DepotId");

	public static string OlderVersion_ManifestId => Get("OlderVersion_ManifestId");

	public static string OlderVersion_DestDir => Get("OlderVersion_DestDir");

	public static string OlderVersion_Download => Get("OlderVersion_Download");

	public static string OlderVersion_OpenSteamDb => Get("OlderVersion_OpenSteamDb");

	public static string OlderVersion_CheckDotNet => Get("OlderVersion_CheckDotNet");

	public static string OlderVersion_DotNetAvailable => Get("OlderVersion_DotNetAvailable");

	public static string OlderVersion_DotNetMissing => Get("OlderVersion_DotNetMissing");

	public static string OlderVersion_Downloading => Get("OlderVersion_Downloading");

	public static string OlderVersion_Done => Get("OlderVersion_Done");

	public static string OlderVersion_Error => Get("OlderVersion_Error");

	public static string OlderVersion_BrowseDest => Get("OlderVersion_BrowseDest");

	public static string Nav_MultiplayerFix => Get("Nav_MultiplayerFix");

	public static string MultiplayerFix_Title => Get("MultiplayerFix_Title");

	public static string MultiplayerFix_Subtitle => Get("MultiplayerFix_Subtitle");

	public static string MultiplayerFix_SearchLabel => Get("MultiplayerFix_SearchLabel");

	public static string MultiplayerFix_SearchPlaceholder => Get("MultiplayerFix_SearchPlaceholder");

	public static string MultiplayerFix_Search => Get("MultiplayerFix_Search");

	public static string MultiplayerFix_Searching => Get("MultiplayerFix_Searching");

	public static string MultiplayerFix_NoResults => Get("MultiplayerFix_NoResults");

	public static string MultiplayerFix_Found => Get("MultiplayerFix_Found");

	public static string MultiplayerFix_Open => Get("MultiplayerFix_Open");

	public static string MultiplayerFix_OpenOnSite => Get("MultiplayerFix_OpenOnSite");

	public static string Nav_CrackFix => Get("Nav_CrackFix");

	public static string CrackFix_Title => Get("CrackFix_Title");

	public static string CrackFix_Subtitle => Get("CrackFix_Subtitle");

	public static string CrackFix_SearchLabel => Get("CrackFix_SearchLabel");

	public static string CrackFix_GameName => Get("CrackFix_GameName");

	public static string CrackFix_GameDir => Get("CrackFix_GameDir");

	public static string CrackFix_SearchPlaceholder => Get("CrackFix_SearchPlaceholder");

	public static string CrackFix_Search => Get("CrackFix_Search");

	public static string CrackFix_Loading => Get("CrackFix_Loading");

	public static string CrackFix_Found => Get("CrackFix_Found");

	public static string CrackFix_Download => Get("CrackFix_Download");

	public static string CrackFix_Downloading => Get("CrackFix_Downloading");

	public static string CrackFix_Extracting => Get("CrackFix_Extracting");

	public static string CrackFix_Applying => Get("CrackFix_Applying");

	public static string CrackFix_Done => Get("CrackFix_Done");

	public static string CrackFix_NoFixes => Get("CrackFix_NoFixes");

	public static string CrackFix_NoGameDir => Get("CrackFix_NoGameDir");

	public static string CrackFix_ExtractFailed => Get("CrackFix_ExtractFailed");

	public static string CrackFix_BrowseGameDir => Get("CrackFix_BrowseGameDir");

	public static string Nav_DlcUnlocker => Get("Nav_DlcUnlocker");

	public static string DlcUnlocker_Title => Get("DlcUnlocker_Title");

	public static string DlcUnlocker_Subtitle => Get("DlcUnlocker_Subtitle");

	public static string DlcUnlocker_GameDir => Get("DlcUnlocker_GameDir");

	public static string DlcUnlocker_AppIdLabel => Get("DlcUnlocker_AppIdLabel");

	public static string DlcUnlocker_Detect => Get("DlcUnlocker_Detect");

	public static string DlcUnlocker_SelectUnlocker => Get("DlcUnlocker_SelectUnlocker");

	public static string DlcUnlocker_DlcIds => Get("DlcUnlocker_DlcIds");

	public static string DlcUnlocker_DlcIdsNote => Get("DlcUnlocker_DlcIdsNote");

	public static string DlcUnlocker_FetchDlcs => Get("DlcUnlocker_FetchDlcs");

	public static string DlcUnlocker_Install => Get("DlcUnlocker_Install");

	public static string DlcUnlocker_Uninstall => Get("DlcUnlocker_Uninstall");

	public static string DlcUnlocker_BrowseGameDir => Get("DlcUnlocker_BrowseGameDir");

	public static string DlcUnlocker_InvalidDir => Get("DlcUnlocker_InvalidDir");

	public static string DlcUnlocker_Detecting => Get("DlcUnlocker_Detecting");

	public static string DlcUnlocker_NoPlatform => Get("DlcUnlocker_NoPlatform");

	public static string DlcUnlocker_PlatformDetected => Get("DlcUnlocker_PlatformDetected");

	public static string DlcUnlocker_AlreadyInstalled => Get("DlcUnlocker_AlreadyInstalled");

	public static string DlcUnlocker_FetchingDlcs => Get("DlcUnlocker_FetchingDlcs");

	public static string DlcUnlocker_NoDlcsFound => Get("DlcUnlocker_NoDlcsFound");

	public static string DlcUnlocker_DlcsFetched => Get("DlcUnlocker_DlcsFetched");

	public static string DlcUnlocker_Installing => Get("DlcUnlocker_Installing");

	public static string DlcUnlocker_Installed => Get("DlcUnlocker_Installed");

	public static string DlcUnlocker_InstallFailed => Get("DlcUnlocker_InstallFailed");

	public static string DlcUnlocker_Uninstalling => Get("DlcUnlocker_Uninstalling");

	public static string DlcUnlocker_Uninstalled => Get("DlcUnlocker_Uninstalled");

	public static string DlcUnlocker_UninstallFailed => Get("DlcUnlocker_UninstallFailed");

	public static string Nav_DownloadGames => Get("Nav_DownloadGames");

	public static string DownloadGames_Title => Get("DownloadGames_Title");

	public static string DownloadGames_Subtitle => Get("DownloadGames_Subtitle");

	public static string DownloadGames_Description => Get("DownloadGames_Description");

	public static string DownloadGames_SearchLabel => Get("DownloadGames_SearchLabel");

	public static string DownloadGames_GameName => Get("DownloadGames_GameName");

	public static string DownloadGames_SearchPlaceholder => Get("DownloadGames_SearchPlaceholder");

	public static string DownloadGames_Search => Get("DownloadGames_Search");

	public static string DownloadGames_InstallDir => Get("DownloadGames_InstallDir");

	public static string DownloadGames_Download => Get("DownloadGames_Download");

	public static string DownloadGames_Loading => Get("DownloadGames_Loading");

	public static string DownloadGames_Found => Get("DownloadGames_Found");

	public static string DownloadGames_Downloading => Get("DownloadGames_Downloading");

	public static string DownloadGames_Extracting => Get("DownloadGames_Extracting");

	public static string DownloadGames_Done => Get("DownloadGames_Done");

	public static string DownloadGames_Cancelled => Get("DownloadGames_Cancelled");

	public static string DownloadGames_ExtractFailed => Get("DownloadGames_ExtractFailed");

	public static string DownloadGames_NoInstallDir => Get("DownloadGames_NoInstallDir");

	public static string DownloadGames_NoUrls => Get("DownloadGames_NoUrls");

	public static string DownloadGames_OpenedInBrowser => Get("DownloadGames_OpenedInBrowser");

	public static string DownloadGames_OpenedMirror => Get("DownloadGames_OpenedMirror");

	public static string DownloadGames_Extract => Get("DownloadGames_Extract");

	public static string Nav_Health => Get("Nav_Health");

	public static string Health_Title => Get("Health_Title");

	public static string Health_Subtitle => Get("Health_Subtitle");

	public static string Health_ScanAll => Get("Health_ScanAll");

	public static string Health_FixAll => Get("Health_FixAll");

	public static string Nav_SmartDlc => Get("Nav_SmartDlc");

	public static string SmartDlc_Title => Get("SmartDlc_Title");

	public static string SmartDlc_Subtitle => Get("SmartDlc_Subtitle");

	public static string SmartDlc_ScanAll => Get("SmartDlc_ScanAll");

	public static string SmartDlc_UnlockAll => Get("SmartDlc_UnlockAll");

	public static string SmartDlc_Install => Get("SmartDlc_Install");

	public static string Get(string key)
	{
		return Rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
	}
}
