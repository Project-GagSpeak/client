using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.ChatMessages;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Interop.Ipc;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.SpatialAudio.Managers;
using GagSpeak.UpdateMonitoring.SpatialAudio.Spawner;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Permissions;
using ImGuiNET;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using System.Numerics;

namespace GagSpeak.UI;

public class SettingsUi : WindowMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly AccountsTab _accountsTab;
    private readonly DebugTab _debugTab;
    private readonly ClientData _playerCharacterManager;
    private readonly IpcManager _ipcManager;
    private readonly OnFrameworkService _frameworkUtil;
    private readonly GagspeakConfigService _configService;
    private readonly PairManager _pairManager;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly SettingsHardcore _hardcoreSettingsUI;
    private readonly UiSharedService _uiShared;
    private readonly PiShockProvider _shockProvider;
    private readonly AvfxManager _avfxManager;
    private readonly VfxSpawns _vfxSpawns;
    private bool ThemePushed = false;

    public SettingsUi(ILogger<SettingsUi> logger, GagspeakMediator mediator,
        MainHub apiHubMain, AccountsTab accounts, DebugTab debug,
        GagspeakConfigService configService, PairManager pairManager, 
        ClientData playerCharacterManager, ClientConfigurationManager clientConfigs, 
        PiShockProvider shockProvider, AvfxManager avfxManager, VfxSpawns vfxSpawns, 
        ServerConfigurationManager serverConfigs, IpcManager ipcManager, 
        SettingsHardcore hardcoreSettingsUI, UiSharedService uiShared,
        OnFrameworkService frameworkUtil) : base(logger, mediator, "GagSpeak Settings")
    {
        _apiHubMain = apiHubMain;
        _accountsTab = accounts;
        _debugTab = debug;
        _playerCharacterManager = playerCharacterManager;
        _configService = configService;
        _pairManager = pairManager;
        _clientConfigs = clientConfigs;
        _shockProvider = shockProvider;
        _avfxManager = avfxManager;
        _vfxSpawns = vfxSpawns;
        _serverConfigs = serverConfigs;
        _ipcManager = ipcManager;
        _frameworkUtil = frameworkUtil;
        _hardcoreSettingsUI = hardcoreSettingsUI;
        _uiShared = uiShared;

        Flags = ImGuiWindowFlags.NoScrollbar;
        AllowClickthrough = false;
        AllowPinning = false;

        // load the dropdown info
        LanguagesDialects = new Dictionary<string, string[]> {
            { "English", new string[] { "US", "UK" } },
            { "Spanish", new string[] { "Spain", "Mexico" } },
            { "French", new string[] { "France", "Quebec" } },
            { "Japanese", new string[] { "Japan" } }
        };
        _currentDialects = LanguagesDialects[_configService.Current.Language];
        _activeDialect = GetDialectFromConfigDialect();


        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(625, 400),
            MaximumSize = new Vector2(800, 2000),
        };

        Mediator.Subscribe<OpenSettingsUiMessage>(this, (_) => Toggle());
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) => IsOpen = false);
    }

    // Everything below here is temporary until I figure out something better.
    public Dictionary<string, string[]> LanguagesDialects { get; init; } // Languages and Dialects for Chat Garbler.
    private string[] _currentDialects; // Array of Dialects for each Language
    private string _activeDialect; // Selected Dialect for Language


    private string GetDialectFromConfigDialect()
    {
        switch (_configService.Current.LanguageDialect)
        {
            case "IPA_UK": return "en-GB";
            case "IPA_US": return "en-US";
            case "IPA_SPAIN": return "es-ES";
            case "IPA_MEXICO": return "es-MX";
            case "IPA_FRENCH": return "fr-FR";
            case "IPA_QUEBEC": return "fr-CA";
            case "IPA_JAPAN": return "ja-JP";
            default: return "en-US";
        }
    }

    private void SetConfigDialectFromDialect(string dialect)
    {
        switch (dialect)
        {
            case "US": _configService.Current.LanguageDialect = "IPA_US"; _configService.Save(); break;
            case "UK": _configService.Current.LanguageDialect = "IPA_UK"; _configService.Save(); break;
            case "France": _configService.Current.LanguageDialect = "IPA_FRENCH"; _configService.Save(); break;
            case "Quebec": _configService.Current.LanguageDialect = "IPA_QUEBEC"; _configService.Save(); break;
            case "Japan": _configService.Current.LanguageDialect = "IPA_JAPAN"; _configService.Save(); break;
            case "Spain": _configService.Current.LanguageDialect = "IPA_SPAIN"; _configService.Save(); break;
            case "Mexico": _configService.Current.LanguageDialect = "IPA_MEXICO"; _configService.Save(); break;
            default: _configService.Current.LanguageDialect = "IPA_US"; _configService.Save(); break;
        }
    }

    protected override void PreDrawInternal()
    {
        if (!ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.331f, 0.081f, 0.169f, .803f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.579f, 0.170f, 0.359f, 0.828f));

            ThemePushed = true;
        }
    }

    protected override void PostDrawInternal()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(2);
            ThemePushed = false;
        }
    }


    protected override void DrawInternal()
    {
        _uiShared.DrawOtherPluginState();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(GSLoc.Settings.AccountClaimText);
        ImGui.SameLine();
        if (ImGui.Button("CK Discord"))
        {
            Util.OpenLink("https://discord.gg/kinkporium");
        }

        if (MainHub.IsConnected)
        {
            // draw out the tab bar for us.
            if (ImGui.BeginTabBar("mainTabBar"))
            {
                if (ImGui.BeginTabItem(GSLoc.Settings.TabsGlobal))
                {
                    DrawGlobalSettings();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem(GSLoc.Settings.TabsHardcore))
                {
                    _hardcoreSettingsUI.DrawHardcoreSettings();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem(GSLoc.Settings.TabsPreferences))
                {
                    DrawPreferences();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(GSLoc.Settings.TabsAccounts))
                {
                    _accountsTab.DrawManager();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Debug"))
                {
                    _debugTab.DrawDebugMain();
                    ImGui.EndTabItem();
                }

#if DEBUG
                if (ImGui.BeginTabItem("Dev"))
                {
                    _debugTab.DrawDevDebug();
                    ImGui.EndTabItem();
                }
#endif
                ImGui.EndTabBar();
            }
        }
        else
        {
            if (ImGui.BeginTabBar("offlineTabBar"))
            {
                if (ImGui.BeginTabItem(GSLoc.Settings.TabsAccounts))
                {
                    _accountsTab.DrawManager();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
    }

    private DateTime _lastRefresh = DateTime.MinValue;
    private void DrawGlobalSettings()
    {
        bool liveChatGarblerActive = _playerCharacterManager.GlobalPerms!.LiveChatGarblerActive;
        bool liveChatGarblerLocked = _playerCharacterManager.GlobalPerms.LiveChatGarblerLocked;
        bool removeGagOnLockExpiration = _clientConfigs.GagspeakConfig.RemoveGagUponLockExpiration;

        bool wardrobeEnabled = _playerCharacterManager.GlobalPerms.WardrobeEnabled;
        bool itemAutoEquip = _playerCharacterManager.GlobalPerms.ItemAutoEquip;
        bool restraintSetAutoEquip = _playerCharacterManager.GlobalPerms.RestraintSetAutoEquip;
        bool restraintSetDisableWhenUnlocked = _clientConfigs.GagspeakConfig.DisableSetUponUnlock;
        bool cursedDungeonLoot = _clientConfigs.GagspeakConfig.CursedDungeonLoot;
        RevertStyle RevertState = _clientConfigs.GagspeakConfig.RevertStyle;

        bool puppeteerEnabled = _playerCharacterManager.GlobalPerms.PuppeteerEnabled;
        string globalTriggerPhrase = _playerCharacterManager.GlobalPerms.GlobalTriggerPhrase;
        bool globalAllowSitRequests = _playerCharacterManager.GlobalPerms.GlobalAllowSitRequests;
        bool globalAllowMotionRequests = _playerCharacterManager.GlobalPerms.GlobalAllowMotionRequests;
        bool globalAllowAliasRequests = _playerCharacterManager.GlobalPerms.GlobalAllowAliasRequests;
        bool globalAllowAllRequests = _playerCharacterManager.GlobalPerms.GlobalAllowAllRequests;

        bool moodlesEnabled = _playerCharacterManager.GlobalPerms.MoodlesEnabled;

        bool toyboxEnabled = _playerCharacterManager.GlobalPerms.ToyboxEnabled;
        bool intifaceAutoConnect = _clientConfigs.GagspeakConfig.IntifaceAutoConnect;
        string intifaceConnectionAddr = _clientConfigs.GagspeakConfig.IntifaceConnectionSocket;
        bool vibeServerAutoConnect = _clientConfigs.GagspeakConfig.VibeServerAutoConnect;
        bool spatialVibratorAudio = _playerCharacterManager.GlobalPerms.SpatialVibratorAudio; // set here over client so that other players can reference if they should listen in or not.

        // pishock stuff.
        string piShockApiKey = _clientConfigs.GagspeakConfig.PiShockApiKey;
        string piShockUsername = _clientConfigs.GagspeakConfig.PiShockUsername;

        string globalShockCollarShareCode = _playerCharacterManager.GlobalPerms.GlobalShockShareCode;
        bool allowGlobalShockShockCollar = _playerCharacterManager.GlobalPerms.AllowShocks;
        bool allowGlobalVibrateShockCollar = _playerCharacterManager.GlobalPerms.AllowVibrations;
        bool allowGlobalBeepShockCollar = _playerCharacterManager.GlobalPerms.AllowBeeps;
        int maxGlobalShockCollarIntensity = _playerCharacterManager.GlobalPerms.MaxIntensity;
        TimeSpan maxGlobalShockDuration = _playerCharacterManager.GlobalPerms.GetTimespanFromDuration();
        int maxGlobalVibrateDuration = (int)_playerCharacterManager.GlobalPerms.GlobalShockVibrateDuration.TotalSeconds;

        _uiShared.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderGags);
        using (ImRaii.Disabled(liveChatGarblerLocked))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.LiveChatGarbler, ref liveChatGarblerActive))
            {
                // Perform a mediator call that we have updated a permission.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("LiveChatGarblerActive", liveChatGarblerActive), UpdateDir.Own));

            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.LiveChatGarblerTT);
        }

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagGlamours, ref itemAutoEquip))
        {
            _playerCharacterManager.GlobalPerms.ItemAutoEquip = itemAutoEquip;
            // if this creates a race condition down the line remove the above line.
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("ItemAutoEquip", itemAutoEquip), UpdateDir.Own));
            // perform recalculations to our cache.
            Mediator.Publish(new AppearanceImpactingSettingChanged());
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GagGlamoursTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagPadlockTimer, ref removeGagOnLockExpiration))
        {
            _clientConfigs.GagspeakConfig.RemoveGagUponLockExpiration = removeGagOnLockExpiration;
            _clientConfigs.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GagPadlockTimerTT);

        ImGui.Separator();
        _uiShared.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderWardrobe);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.WardrobeActive, ref wardrobeEnabled))
        {
            _playerCharacterManager.GlobalPerms.WardrobeEnabled = wardrobeEnabled;
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("WardrobeEnabled", wardrobeEnabled), UpdateDir.Own));

            // if this creates a race condition down the line remove the above line.
            if (wardrobeEnabled is false)
            {
                // turn off all respective children as well and push the update.
                _playerCharacterManager.GlobalPerms.RestraintSetAutoEquip = false;
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("RestraintSetAutoEquip", false), UpdateDir.Own));
                // disable other options respective to it.
                _clientConfigs.GagspeakConfig.DisableSetUponUnlock = false;
                _clientConfigs.GagspeakConfig.CursedDungeonLoot = false;
                _clientConfigs.Save();
            }
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.WardrobeActiveTT);

        using (ImRaii.Disabled(!wardrobeEnabled))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintSetGlamour, ref restraintSetAutoEquip))
            {
                _playerCharacterManager.GlobalPerms.RestraintSetAutoEquip = restraintSetAutoEquip;
                // if this creates a race condition down the line remove the above line.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData,
                MainHub.PlayerUserData, new KeyValuePair<string, object>("RestraintSetAutoEquip", restraintSetAutoEquip), UpdateDir.Own));
                // perform recalculations to our cache.
                Mediator.Publish(new AppearanceImpactingSettingChanged());
            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.RestraintSetGlamourTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintPadlockTimer, ref restraintSetDisableWhenUnlocked))
            {
                _clientConfigs.GagspeakConfig.DisableSetUponUnlock = restraintSetDisableWhenUnlocked;
                _clientConfigs.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.RestraintPadlockTimerTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.CursedLootActive, ref cursedDungeonLoot))
            {
                _clientConfigs.GagspeakConfig.CursedDungeonLoot = cursedDungeonLoot;
                _clientConfigs.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.CursedLootActiveTT);
        }


        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.MoodlesActive, ref moodlesEnabled))
        {
            _playerCharacterManager.GlobalPerms.MoodlesEnabled = moodlesEnabled;
            // if this creates a race condition down the line remove the above line.
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("MoodlesEnabled", moodlesEnabled), UpdateDir.Own));
            // perform recalculations to our cache.
            Mediator.Publish(new AppearanceImpactingSettingChanged());

        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.MoodlesActiveTT);

        // draw out revert style selection
        ImGui.Spacing();
        ImGui.TextUnformatted(GSLoc.Settings.MainOptions.RevertSelectionLabel);
        // Draw radio buttons for each RevertStyle enum value
        foreach (RevertStyle style in Enum.GetValues(typeof(RevertStyle)))
        {
            string label = (style is RevertStyle.RevertToGame or RevertStyle.RevertToAutomation) ? "Revert" : "Reapply";

            bool isSelected = _clientConfigs.GagspeakConfig.RevertStyle == style;
            if (ImGui.RadioButton(label + "##" + style.ToString(), isSelected))
            {
                _clientConfigs.GagspeakConfig.RevertStyle = style;
                _clientConfigs.Save();
            }
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(style.ToName());
            _uiShared.DrawHelpText(style.ToHelpText());
        }


        ImGui.Separator();
        _uiShared.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderPuppet);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.PuppeteerActive, ref puppeteerEnabled))
        {
            _playerCharacterManager.GlobalPerms.PuppeteerEnabled = puppeteerEnabled;
            // if this creates a race condition down the line remove the above line.
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("PuppeteerEnabled", puppeteerEnabled), UpdateDir.Own));

        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.PuppeteerActiveTT);

        using (ImRaii.Disabled(!puppeteerEnabled))
        {
            using var indent = ImRaii.PushIndent();

            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText(GSLoc.Settings.MainOptions.GlobalTriggerPhrase, ref globalTriggerPhrase, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _playerCharacterManager.GlobalPerms.GlobalTriggerPhrase = globalTriggerPhrase;
                // if this creates a race condition down the line remove the above line.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("GlobalTriggerPhrase", globalTriggerPhrase), UpdateDir.Own));

            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GlobalTriggerPhraseTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAllowSit, ref globalAllowSitRequests))
            {
                _playerCharacterManager.GlobalPerms.GlobalAllowSitRequests = globalAllowSitRequests;
                // if this creates a race condition down the line remove the above line.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("GlobalAllowSitRequests", globalAllowSitRequests), UpdateDir.Own));

            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GlobalAllowSitTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAllowMotion, ref globalAllowMotionRequests))
            {
                _playerCharacterManager.GlobalPerms.GlobalAllowMotionRequests = globalAllowMotionRequests;
                // if this creates a race condition down the line remove the above line.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("GlobalAllowMotionRequests", globalAllowMotionRequests), UpdateDir.Own));

            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GlobalAllowMotionTT);
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAllowAlias, ref globalAllowAliasRequests))
            {
                _playerCharacterManager.GlobalPerms.GlobalAllowAliasRequests = globalAllowAliasRequests;
                // if this creates a race condition down the line remove the above line.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("GlobalAllowAliasRequests", globalAllowAliasRequests), UpdateDir.Own));

            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GlobalAllowAliasTT);
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAllowAll, ref globalAllowAllRequests))
            {
                _playerCharacterManager.GlobalPerms.GlobalAllowAllRequests = globalAllowAllRequests;
                // if this creates a race condition down the line remove the above line.
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("GlobalAllowAllRequests", globalAllowAllRequests), UpdateDir.Own));

            }
            _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.GlobalAllowAllTT);
        }


        ImGui.Separator();
        _uiShared.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderToybox);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.ToyboxActive, ref toyboxEnabled))
        {
            _playerCharacterManager.GlobalPerms.ToyboxEnabled = toyboxEnabled;
            // if this creates a race condition down the line remove the above line.
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData,
            MainHub.PlayerUserData, new KeyValuePair<string, object>("ToyboxEnabled", toyboxEnabled), UpdateDir.Own));

        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.ToyboxActiveTT);


        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.IntifaceAutoConnect, ref intifaceAutoConnect))
        {
            _clientConfigs.GagspeakConfig.IntifaceAutoConnect = intifaceAutoConnect;
            _clientConfigs.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.IntifaceAutoConnectTT);

        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint($"Server Address##ConnectionWSaddr", "Leave blank for default...", ref intifaceConnectionAddr, 100))
        {
            if (!intifaceConnectionAddr.Contains("ws://"))
            {
                intifaceConnectionAddr = "ws://localhost:12345";
            }
            else
            {
                _clientConfigs.GagspeakConfig.IntifaceConnectionSocket = intifaceConnectionAddr;
                _clientConfigs.Save();
            }
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.IntifaceAddressTT);

        using (ImRaii.Disabled(true))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.VibeServerAutoConnect, ref vibeServerAutoConnect))
            {
                _clientConfigs.GagspeakConfig.VibeServerAutoConnect = vibeServerAutoConnect;
                _clientConfigs.Save();
            }
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.VibeServerAutoConnectTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.SpatialAudioActive, ref spatialVibratorAudio))
        {
            _playerCharacterManager.GlobalPerms.SpatialVibratorAudio = spatialVibratorAudio;
            // if this creates a race condition down the line remove the above line.
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData,
            new KeyValuePair<string, object>("SpatialVibratorAudio", spatialVibratorAudio), UpdateDir.Own));
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.SpatialAudioActiveTT);

        ImGui.Spacing();

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("PiShock API Key", ref piShockApiKey, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _clientConfigs.GagspeakConfig.PiShockApiKey = piShockApiKey;
            _clientConfigs.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.PiShockKeyTT);

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("PiShock Username", ref piShockUsername, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _clientConfigs.GagspeakConfig.PiShockUsername = piShockUsername;
            _clientConfigs.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.PiShockUsernameTT);


        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale - _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Sync, "Refresh") - ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGui.InputText("##Global PiShock Share Code", ref globalShockCollarShareCode, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _playerCharacterManager.GlobalPerms.GlobalShockShareCode = globalShockCollarShareCode;

            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>("GlobalShockShareCode", globalShockCollarShareCode), UpdateDir.Own));
        }
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Sync, "Refresh", null, false, DateTime.UtcNow - _lastRefresh < TimeSpan.FromSeconds(5)))
        {
            _lastRefresh = DateTime.UtcNow;
            // Send Mediator Event to grab updated settings for pair.
            Task.Run(async () =>
            {
                if (_playerCharacterManager.GlobalPerms is null)
                    return;

                var newPerms = await _shockProvider.GetPermissionsFromCode(_playerCharacterManager.GlobalPerms.GlobalShockShareCode);
                // set the new permissions.
                _playerCharacterManager.GlobalPerms.AllowShocks = newPerms.AllowShocks;
                _playerCharacterManager.GlobalPerms.AllowVibrations = newPerms.AllowVibrations;
                _playerCharacterManager.GlobalPerms.AllowBeeps = newPerms.AllowBeeps;
                _playerCharacterManager.GlobalPerms.MaxDuration = newPerms.MaxDuration;
                _playerCharacterManager.GlobalPerms.MaxIntensity = newPerms.MaxIntensity;
                // update the permissions.
                _ = _apiHubMain.UserPushAllGlobalPerms(new(MainHub.PlayerUserData, MainHub.PlayerUserData, _playerCharacterManager.GlobalPerms, UpdateDir.Own));
            });
        }
        UiSharedService.AttachToolTip(GSLoc.Settings.MainOptions.PiShockShareCodeRefreshTT);

        ImUtf8.SameLineInner();
        ImGui.TextUnformatted(GSLoc.Settings.MainOptions.PiShockShareCode);
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.PiShockShareCodeTT);

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderInt(GSLoc.Settings.MainOptions.PiShockVibeTime, ref maxGlobalVibrateDuration, 0, 30))
        {
            _playerCharacterManager.GlobalPerms.GlobalShockVibrateDuration = TimeSpan.FromSeconds(maxGlobalVibrateDuration);
        }
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            // Convert TimeSpan to ticks and send as UInt64
            ulong ticks = (ulong)_playerCharacterManager.GlobalPerms.GlobalShockVibrateDuration.Ticks;
            _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(MainHub.PlayerUserData, MainHub.PlayerUserData,
            new KeyValuePair<string, object>("GlobalShockVibrateDuration", ticks), UpdateDir.Own));
        }
        _uiShared.DrawHelpText(GSLoc.Settings.MainOptions.PiShockVibeTimeTT);

        // make this section readonly
        UiSharedService.ColorText(GSLoc.Settings.MainOptions.PiShockPermsLabel, ImGuiColors.ParsedGold);
        using (ImRaii.Disabled(true))
        {
            using (ImRaii.Group())
            {
                ImGui.Checkbox(GSLoc.Settings.MainOptions.PiShockAllowShocks, ref allowGlobalShockShockCollar);
                ImGui.SameLine();
                ImGui.Checkbox(GSLoc.Settings.MainOptions.PiShockAllowVibes, ref allowGlobalVibrateShockCollar);
                ImGui.SameLine();
                ImGui.Checkbox(GSLoc.Settings.MainOptions.PiShockAllowBeeps, ref allowGlobalBeepShockCollar);
            }
            ImGui.TextUnformatted(GSLoc.Settings.MainOptions.PiShockMaxShockIntensity);
            ImGui.SameLine();
            UiSharedService.ColorText(maxGlobalShockCollarIntensity.ToString() + "%", ImGuiColors.ParsedGold);

            ImGui.TextUnformatted(GSLoc.Settings.MainOptions.PiShockMaxShockDuration);
            ImGui.SameLine();
            UiSharedService.ColorText(maxGlobalShockDuration.Seconds.ToString() + "." + maxGlobalShockDuration.Milliseconds.ToString() + "s", ImGuiColors.ParsedGold);
        }
    }

    private void DrawChannelPreferences()
    {
        float width = ImGui.GetContentRegionAvail().X / 2;
        ImGui.Columns(2, "PreferencesColumns", true);
        ImGui.SetColumnWidth(0, width);
        // go to first column.
        _uiShared.GagspeakBigText("Live Chat Garbler");
        using (ImRaii.Group())
        {
            // display the channels
            var i = 0;
            foreach (var e in ChatChannel.GetOrderedChannels())
            {
                // See if it is already enabled by default
                var enabled = _configService.Current.ChannelsGagSpeak.Contains(e);
                // Create a new line after every 4 columns
                if (i != 0 && (i == 4 || i == 7 || i == 11 || i == 15 || i == 19))
                    ImGui.NewLine();

                if (ImGui.Checkbox($"{e}", ref enabled))
                {
                    if (enabled) _configService.Current.ChannelsGagSpeak.Add(e);
                    else _configService.Current.ChannelsGagSpeak.Remove(e);
                    _configService.Save();
                }

                ImGui.SameLine();
                i++;
            }

            ImGui.NewLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(GSLoc.Settings.Preferences.LangDialectLabel);
            ImGui.SameLine();
            _uiShared.DrawCombo("##Language", 65, LanguagesDialects.Keys.ToArray(), (item) => item, (i) =>
            {
                if (i is null || i == _configService.Current.Language) return;
                _configService.Current.Language = i;
                _currentDialects = LanguagesDialects[_configService.Current.Language]; // update the dialects for the new language
                _activeDialect = _currentDialects[0]; // set the active dialect to the first dialect of the new language
                SetConfigDialectFromDialect(_activeDialect);
            }, _configService.Current.Language, flags: ImGuiComboFlags.NoArrowButton);
            UiSharedService.AttachToolTip(GSLoc.Settings.Preferences.LangTT);

            ImGui.SameLine();
            _uiShared.DrawCombo("##Dialect", 55, LanguagesDialects[_configService.Current.Language], (item) => item, (i) =>
            {
                if (i is null || i == _activeDialect) return;
                _activeDialect = i;
                SetConfigDialectFromDialect(_activeDialect);
            }, _activeDialect, flags: ImGuiComboFlags.NoArrowButton);
            UiSharedService.AttachToolTip(GSLoc.Settings.Preferences.DialectTT);
        }
    }
    private void DrawPreferences()
    {
        DrawChannelPreferences();

        ImGui.NextColumn();
        _uiShared.GagspeakBigText(GSLoc.Settings.Preferences.HeaderPuppet);
        using (ImRaii.Group())
        {
            // display the channels
            var j = 0;
            foreach (var e in ChatChannel.GetOrderedChannels())
            {
                // See if it is already enabled by default
                var enabled = _configService.Current.ChannelsPuppeteer.Contains(e);

                // Create a new line after every 4 columns
                if (j != 0 && (j == 4 || j == 7 || j == 11 || j == 15 || j == 19))
                    ImGui.NewLine();

                if (ImGui.Checkbox($"{e}##{e}puppeteer", ref enabled))
                {
                    if (enabled)
                    {
                        if (!_configService.Current.ChannelsPuppeteer.Contains(e))
                            _configService.Current.ChannelsPuppeteer.Add(e);
                    }
                    else
                    {
                        _configService.Current.ChannelsPuppeteer.Remove(e);
                    }
                    _configService.Save();
                }

                ImGui.SameLine();
                j++;
            }
        }
        ImGui.Columns(1);

        ImGui.Separator();
        _uiShared.GagspeakBigText(GSLoc.Settings.Preferences.HeaderUiPrefs);

        var showMainUiOnStart = _configService.Current.OpenMainUiOnStartup;

        var enableDtrEntry = _configService.Current.EnableDtrEntry;
        var dtrPrivacyRadar = _configService.Current.ShowPrivacyRadar;
        var dtrActionNotifs = _configService.Current.ShowActionNotifs;
        var dtrVibeStatus = _configService.Current.ShowVibeStatus;

        var preferThreeCharaAnonName = _configService.Current.PreferThreeCharaAnonName;
        var preferNicknamesInsteadOfName = _configService.Current.PreferNicknamesOverNames;
        var showVisibleSeparate = _configService.Current.ShowVisibleUsersSeparately;
        var showOfflineSeparate = _configService.Current.ShowOfflineUsersSeparately;

        var showProfiles = _configService.Current.ShowProfiles;
        var profileDelay = _configService.Current.ProfileDelay;
        var showContextMenus = _configService.Current.ShowContextMenus;

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowMainUiOnStartLabel, ref showMainUiOnStart))
        {
            _configService.Current.OpenMainUiOnStartup = showMainUiOnStart;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ShowMainUiOnStartTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.EnableDtrLabel, ref enableDtrEntry))
        {
            _configService.Current.EnableDtrEntry = enableDtrEntry;
            if (enableDtrEntry is false)
            {
                _configService.Current.ShowPrivacyRadar = false;
                _configService.Current.ShowActionNotifs = false;
                _configService.Current.ShowVibeStatus = false;
            }
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.EnableDtrTT);

        using (ImRaii.Disabled(!enableDtrEntry))
        {
            ImGui.Indent();
            if (ImGui.Checkbox(GSLoc.Settings.Preferences.PrivacyRadarLabel, ref dtrPrivacyRadar))
            {
                _configService.Current.ShowPrivacyRadar = dtrPrivacyRadar;
                _configService.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Preferences.PrivacyRadarTT);

            if (ImGui.Checkbox(GSLoc.Settings.Preferences.ActionsNotifLabel, ref dtrActionNotifs))
            {
                _configService.Current.ShowActionNotifs = dtrActionNotifs;
                _configService.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ActionsNotifTT);

            if (ImGui.Checkbox(GSLoc.Settings.Preferences.VibeStatusLabel, ref dtrVibeStatus))
            {
                _configService.Current.ShowVibeStatus = dtrVibeStatus;
                _configService.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Preferences.VibeStatusTT);
            ImGui.Unindent();
        }

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowVisibleSeparateLabel, ref showVisibleSeparate))
        {
            _configService.Current.ShowVisibleUsersSeparately = showVisibleSeparate;
            _configService.Save();
            Mediator.Publish(new RefreshUiMessage());
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ShowVisibleSeparateTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowOfflineSeparateLabel, ref showOfflineSeparate))
        {
            _configService.Current.ShowOfflineUsersSeparately = showOfflineSeparate;
            _configService.Save();
            Mediator.Publish(new RefreshUiMessage());
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ShowOfflineSeparateTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.PrefThreeCharaAnonName, ref preferThreeCharaAnonName))
        {
            _configService.Current.PreferThreeCharaAnonName = preferThreeCharaAnonName;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.PrefThreeCharaAnonNameTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.PreferNicknamesLabel, ref preferNicknamesInsteadOfName))
        {
            _configService.Current.PreferNicknamesOverNames = preferNicknamesInsteadOfName;
            _configService.Save();
            Mediator.Publish(new RefreshUiMessage());
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.PreferNicknamesTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowProfilesLabel, ref showProfiles))
        {
            Mediator.Publish(new ClearProfileDataMessage());
            _configService.Current.ShowProfiles = showProfiles;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ShowProfilesTT);

        using (ImRaii.Disabled(!showProfiles))
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (ImGui.SliderFloat(GSLoc.Settings.Preferences.ProfileDelayLabel, ref profileDelay, 0.3f, 5))
            {
                _configService.Current.ProfileDelay = profileDelay;
                _configService.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ProfileDelayTT);
            ImGui.Unindent();
        }

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ContextMenusLabel, ref showContextMenus))
        {
            _configService.Current.ShowContextMenus = showContextMenus;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ContextMenusTT);

        /* --------------- Separator for moving onto the Notifications Section ----------- */
        ImGui.Separator();
        _uiShared.GagspeakBigText(GSLoc.Settings.Preferences.HeaderNotifications);

        var liveGarblerZoneChangeWarn = _configService.Current.LiveGarblerZoneChangeWarn;
        var serverConnectionNotifs = _configService.Current.NotifyForServerConnections;
        var onlineNotifs = _configService.Current.NotifyForOnlinePairs;
        var onlineNotifsNickLimited = _configService.Current.NotifyLimitToNickedPairs;

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ZoneChangeWarnLabel, ref liveGarblerZoneChangeWarn))
        {
            _configService.Current.LiveGarblerZoneChangeWarn = liveGarblerZoneChangeWarn;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ZoneChangeWarnTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ConnectedNotifLabel, ref serverConnectionNotifs))
        {
            _configService.Current.NotifyForServerConnections = serverConnectionNotifs;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.ConnectedNotifTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.OnlineNotifLabel, ref onlineNotifs))
        {
            _configService.Current.NotifyForOnlinePairs = onlineNotifs;
            if (!onlineNotifs) _configService.Current.NotifyLimitToNickedPairs = false;
            _configService.Save();
        }
        _uiShared.DrawHelpText(GSLoc.Settings.Preferences.OnlineNotifTT);

        using (ImRaii.Disabled(!onlineNotifs))
        {
            if (ImGui.Checkbox(GSLoc.Settings.Preferences.LimitForNicksLabel, ref onlineNotifsNickLimited))
            {
                _configService.Current.NotifyLimitToNickedPairs = onlineNotifsNickLimited;
                _configService.Save();
            }
            _uiShared.DrawHelpText(GSLoc.Settings.Preferences.LimitForNicksTT);
        }

        _uiShared.DrawCombo("Info Location##settingsUiInfo", 125f, (NotificationLocation[])Enum.GetValues(typeof(NotificationLocation)), (i) => i.ToString(),
        (i) => { _configService.Current.InfoNotification = i; _configService.Save(); }, _configService.Current.InfoNotification);
        _uiShared.DrawHelpText("The location where \"Info\" notifications will display."
                      + Environment.NewLine + "'Nowhere' will not show any Info notifications"
                      + Environment.NewLine + "'Chat' will print Info notifications in chat"
                      + Environment.NewLine + "'Toast' will show Warning toast notifications in the bottom right corner"
                      + Environment.NewLine + "'Both' will show chat as well as the toast notification");

        _uiShared.DrawCombo("Warning Location##settingsUiWarn", 125f, (NotificationLocation[])Enum.GetValues(typeof(NotificationLocation)), (i) => i.ToString(),
        (i) => { _configService.Current.WarningNotification = i; _configService.Save(); }, _configService.Current.WarningNotification);
        _uiShared.DrawHelpText("The location where \"Warning\" notifications will display."
                              + Environment.NewLine + "'Nowhere' will not show any Warning notifications"
                              + Environment.NewLine + "'Chat' will print Warning notifications in chat"
                              + Environment.NewLine + "'Toast' will show Warning toast notifications in the bottom right corner"
                              + Environment.NewLine + "'Both' will show chat as well as the toast notification");

        _uiShared.DrawCombo("Error Location##settingsUi", 125f, (NotificationLocation[])Enum.GetValues(typeof(NotificationLocation)), (i) => i.ToString(),
        (i) => { _configService.Current.ErrorNotification = i; _configService.Save(); }, _configService.Current.ErrorNotification);
        _uiShared.DrawHelpText("The location where \"Error\" notifications will display."
                              + Environment.NewLine + "'Nowhere' will not show any Error notifications"
                              + Environment.NewLine + "'Chat' will print Error notifications in chat"
                              + Environment.NewLine + "'Toast' will show Error toast notifications in the bottom right corner"
                              + Environment.NewLine + "'Both' will show chat as well as the toast notification");
    }
}
