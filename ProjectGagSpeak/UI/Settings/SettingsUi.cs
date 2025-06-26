using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.CkCommons.GarblerCore;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class SettingsUi : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly GlobalPermissions _global;
    private readonly SettingsHardcore _hardcoreSettingsUI;
    private readonly AccountManagerTab _accountsTab;
    private readonly DebugTab _debugTab;
    private readonly PiShockProvider _shockProvider;
    private readonly MainConfig _mainConfig;

    public SettingsUi(
        ILogger<SettingsUi> logger,
        GagspeakMediator mediator,
        MainHub hub,
        GlobalPermissions global,
        SettingsHardcore hardcoreTab,
        AccountManagerTab accounts,
        DebugTab debug,
        PiShockProvider shockProvider,
        MainConfig config) : base(logger, mediator, "GagSpeak Settings")
    {
        _hub = hub;
        _global = global;
        _hardcoreSettingsUI = hardcoreTab;
        _accountsTab = accounts;
        _debugTab = debug;
        _shockProvider = shockProvider;
        _mainConfig = config;

        Flags = WFlags.NoScrollbar;
        AllowClickthrough = false;
        AllowPinning = false;

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(625, 400),
            MaximumSize = ImGui.GetIO().DisplaySize,
        };

#if DEBUG
        TitleBarButtons = new()
        {
            new TitleBarButton()
            {
                Icon = FAI.Tshirt,
                Click = (msg) => Mediator.Publish(new UiToggleMessage(typeof(DebugActiveStateUI))),
                IconOffset = new(2,1),
                ShowTooltip = () => CkGui.AttachToolTip("Open Active State Debugger")
            },
            new TitleBarButton()
            {
                Icon = FAI.PersonRays,
                Click = (msg) => Mediator.Publish(new UiToggleMessage(typeof(DebugPersonalDataUI))),
                IconOffset = new(2,1),
                ShowTooltip = () => CkGui.AttachToolTip("Open Personal Data Debugger")  
            },
            new TitleBarButton()
            {
                Icon = FAI.Database,
                Click = (msg) => Mediator.Publish(new UiToggleMessage(typeof(DebugStorageUI))),
                IconOffset = new(2,1),
                ShowTooltip = () => CkGui.AttachToolTip("Open Storages Debugger")
            },
        };
#endif
    }

    private bool ThemePushed = false;

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
        CkGui.DrawOptionalPlugins();

        ImUtf8.TextFrameAligned(GSLoc.Settings.AccountClaimText);
        
        ImGui.SameLine();
        if (ImGui.Button("CK Discord"))
            Util.OpenLink("https://discord.gg/kinkporium");

        // draw out the tab bar for us.
        if (ImGui.BeginTabBar("mainTabBar"))
        {
            if (MainHub.IsConnected)
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

            ImGui.EndTabBar();
        }
    }

    private DateTime _lastRefresh = DateTime.MinValue;
    private void DrawGlobalSettings()
    {
        if(_global.Current is not { } globals)
        {
            ImGui.Text("Global Perms is null! Safely returning early");
            return;
        }

        var liveChatGarblerActive = globals!.ChatGarblerActive;
        var liveChatGarblerLocked = globals.ChatGarblerLocked;
        var removeGagOnLockExpiration = _mainConfig.Current.RemoveRestrictionOnTimerExpire;

        var wardrobeEnabled = globals.WardrobeEnabled;
        var gagVisuals = globals.GagVisuals;
        var restrictionVisuals = globals.RestrictionVisuals;
        var restraintSetVisuals = globals.RestraintSetVisuals;
        var cursedDungeonLoot = _mainConfig.Current.CursedLootPanel;
        var mimicsApplyTraits = _mainConfig.Current.CursedItemsApplyTraits;

        var puppeteerEnabled = globals.PuppeteerEnabled;
        var globalTriggerPhrase = globals.TriggerPhrase;
        var globalPuppetPerms = globals.PuppetPerms;

        var toyboxEnabled = globals.ToyboxEnabled;
        var intifaceAutoConnect = _mainConfig.Current.IntifaceAutoConnect;
        var intifaceConnectionAddr = _mainConfig.Current.IntifaceConnectionSocket;
        var spatialVibratorAudio = globals.SpatialAudio;

        // pishock stuff.
        var piShockApiKey = _mainConfig.Current.PiShockApiKey;
        var piShockUsername = _mainConfig.Current.PiShockUsername;

        var globalPiShockShareCode = globals.GlobalShockShareCode;
        var allowGlobalShockShockCollar = globals.AllowShocks;
        var allowGlobalVibrateShockCollar = globals.AllowVibrations;
        var allowGlobalBeepShockCollar = globals.AllowBeeps;
        var maxGlobalShockCollarIntensity = globals.MaxIntensity;
        var maxGlobalShockDuration = globals.GetTimespanFromDuration();
        var maxGlobalVibrateDuration = (int)globals.ShockVibrateDuration.TotalSeconds;

        CkGui.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderGags);
        using (ImRaii.Disabled(liveChatGarblerLocked))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.LiveChatGarbler, ref liveChatGarblerActive))
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.ChatGarblerActive), liveChatGarblerActive).ConfigureAwait(false);
            CkGui.HelpText(GSLoc.Settings.MainOptions.LiveChatGarblerTT);
        }

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagGlamours, ref gagVisuals))
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.GagVisuals), gagVisuals).ConfigureAwait(false);
        CkGui.HelpText(GSLoc.Settings.MainOptions.GagGlamoursTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagPadlockTimer, ref removeGagOnLockExpiration))
        {
            _mainConfig.Current.RemoveRestrictionOnTimerExpire = removeGagOnLockExpiration;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.GagPadlockTimerTT);

        ImGui.Separator();
        CkGui.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderWardrobe);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.WardrobeActive, ref wardrobeEnabled))
        {
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.WardrobeEnabled), wardrobeEnabled).ConfigureAwait(false);
            if (wardrobeEnabled is false)
            {
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestrictionVisuals), false).ConfigureAwait(false);
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestraintSetVisuals), false).ConfigureAwait(false);
                _mainConfig.Current.CursedLootPanel = false;
                _mainConfig.Save();
            }
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.WardrobeActiveTT);

        using (ImRaii.Disabled(!wardrobeEnabled))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintSetGlamour, ref restrictionVisuals))
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestrictionVisuals), restrictionVisuals).ConfigureAwait(false);
            CkGui.HelpText(GSLoc.Settings.MainOptions.RestraintSetGlamourTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintSetGlamour, ref restraintSetVisuals))
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestraintSetVisuals), restraintSetVisuals).ConfigureAwait(false);
            CkGui.HelpText(GSLoc.Settings.MainOptions.RestraintSetGlamourTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.CursedLootActive, ref cursedDungeonLoot))
            {
                _mainConfig.Current.CursedLootPanel = cursedDungeonLoot;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.CursedLootActiveTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.MimicsApplyTraits, ref mimicsApplyTraits))
            {
                _mainConfig.Current.CursedItemsApplyTraits = mimicsApplyTraits;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.MimicsApplyTraitsTT);
        }

        CkGui.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderPuppet);
        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.PuppeteerActive, ref puppeteerEnabled))
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.PuppeteerEnabled), puppeteerEnabled).ConfigureAwait(false);
        CkGui.HelpText(GSLoc.Settings.MainOptions.PuppeteerActiveTT);

        using (ImRaii.Disabled(!puppeteerEnabled))
        {
            using var indent = ImRaii.PushIndent();

            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText(GSLoc.Settings.MainOptions.GlobalTriggerPhrase, ref globalTriggerPhrase, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.TriggerPhrase), globalTriggerPhrase).ConfigureAwait(false);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalTriggerPhraseTT);

            // Correct these!
            var refSits = (globalPuppetPerms & PuppetPerms.Sit) == PuppetPerms.Sit;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalSit, ref refSits))
            {
                var newPerms = globalPuppetPerms ^ PuppetPerms.Sit;
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.PuppetPerms), newPerms).ConfigureAwait(false);
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalSitTT);

            var refEmotes = (globalPuppetPerms & PuppetPerms.Emotes) == PuppetPerms.Emotes;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalMotion, ref refEmotes))
            {
                var newPerms = globalPuppetPerms ^ PuppetPerms.Emotes;
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.PuppetPerms), newPerms).ConfigureAwait(false);
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalMotionTT);

            var refAlias = (globalPuppetPerms & PuppetPerms.Alias) == PuppetPerms.Alias;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAlias, ref refAlias))
            {
                var newPerms = globalPuppetPerms ^ PuppetPerms.Alias;
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.PuppetPerms), newPerms).ConfigureAwait(false);
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalAliasTT);

            var refAllPerms = (globalPuppetPerms & PuppetPerms.All) == PuppetPerms.All;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAll, ref refAllPerms))
            {
                var newPerms = globalPuppetPerms ^ PuppetPerms.All;
                PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.PuppetPerms), newPerms).ConfigureAwait(false);
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalAllTT);
        }

        ImGui.Separator();
        CkGui.GagspeakBigText(GSLoc.Settings.MainOptions.HeaderToybox);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.ToyboxActive, ref toyboxEnabled))
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.ToyboxEnabled), toyboxEnabled).ConfigureAwait(false);
        CkGui.HelpText(GSLoc.Settings.MainOptions.ToyboxActiveTT);


        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.IntifaceAutoConnect, ref intifaceAutoConnect))
        {
            _mainConfig.Current.IntifaceAutoConnect = intifaceAutoConnect;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.IntifaceAutoConnectTT);

        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint($"Server Address##ConnectionWSaddr", "Leave blank for default...", ref intifaceConnectionAddr, 100))
        {
            if (!intifaceConnectionAddr.Contains("ws://"))
            {
                intifaceConnectionAddr = "ws://localhost:12345";
            }
            else
            {
                _mainConfig.Current.IntifaceConnectionSocket = intifaceConnectionAddr;
                _mainConfig.Save();
            }
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.IntifaceAddressTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.SpatialAudioActive, ref spatialVibratorAudio))
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.SpatialAudio), spatialVibratorAudio).ConfigureAwait(false);
        CkGui.HelpText(GSLoc.Settings.MainOptions.SpatialAudioActiveTT);

        ImGui.Spacing();

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("PiShock API Key", ref piShockApiKey, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _mainConfig.Current.PiShockApiKey = piShockApiKey;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockKeyTT);

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("PiShock Username", ref piShockUsername, 100, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _mainConfig.Current.PiShockUsername = piShockUsername;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockUsernameTT);


        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale - CkGui.IconTextButtonSize(FAI.Sync, "Refresh") - ImGui.GetStyle().ItemInnerSpacing.X);
        if (ImGui.InputText("##Global PiShock Share Code", ref globalPiShockShareCode, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.GlobalShockShareCode), globalPiShockShareCode).ConfigureAwait(false);

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.Sync, "Refresh", null, false, DateTime.UtcNow - _lastRefresh < TimeSpan.FromSeconds(5)))
        {
            _lastRefresh = DateTime.UtcNow;
            // Send Mediator Event to grab updated settings for pair.
            Task.Run(async () =>
            {
                if (globals is null)
                    return;

                var newPerms = await _shockProvider.GetPermissionsFromCode(globals.GlobalShockShareCode);
                // set the new permissions, without affecting the original.
                var newGlobalPerms = globals with
                {
                    AllowShocks = newPerms.AllowShocks,
                    AllowVibrations = newPerms.AllowVibrations,
                    AllowBeeps = newPerms.AllowBeeps,
                    MaxDuration = newPerms.MaxDuration,
                    MaxIntensity = newPerms.MaxIntensity,
                };
                await _hub.UserBulkChangeGlobal(new(MainHub.PlayerUserData, newGlobalPerms));
            });
        }
        CkGui.AttachToolTip(GSLoc.Settings.MainOptions.PiShockShareCodeRefreshTT);

        ImUtf8.SameLineInner();
        ImGui.TextUnformatted(GSLoc.Settings.MainOptions.PiShockShareCode);
        CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockShareCodeTT);

        ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
        ImGui.SliderInt(GSLoc.Settings.MainOptions.PiShockVibeTime, ref maxGlobalVibrateDuration, 0, 30);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            // Convert TimeSpan to ticks and send as UInt64
            var ticks = (ulong)TimeSpan.FromSeconds(maxGlobalVibrateDuration).Ticks;
            PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.ShockVibrateDuration), ticks).ConfigureAwait(false);
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockVibeTimeTT);

        // make this section readonly
        CkGui.ColorText(GSLoc.Settings.MainOptions.PiShockPermsLabel, ImGuiColors.ParsedGold);
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
            CkGui.ColorText(maxGlobalShockCollarIntensity.ToString() + "%", ImGuiColors.ParsedGold);

            ImGui.TextUnformatted(GSLoc.Settings.MainOptions.PiShockMaxShockDuration);
            ImGui.SameLine();
            CkGui.ColorText(maxGlobalShockDuration.Seconds.ToString() + "." + maxGlobalShockDuration.Milliseconds.ToString() + "s", ImGuiColors.ParsedGold);
        }
    }

    private void DrawChannelPreferences()
    {
        // do not draw the preferences if the globalpermissions are null.
        if(_global.Current is not { } globals)
        {
            ImGui.Text("Globals is null! Returning early");
            return;
        }

        var width = ImGui.GetContentRegionAvail().X / 2;
        ImGui.Columns(2, "PreferencesColumns", true);
        ImGui.SetColumnWidth(0, width);

        CkGui.GagspeakBigText("Live Chat Garbler");
        using (ImRaii.Group())
        {
            foreach (var (label, channels) in ChatLogAgent.SortedChannels)
            {
                ImGui.Text(label); // Show the group label

                for (int i = 0; i < channels.Length; i++)
                {
                    var channel = channels[i];
                    var enabled = channel.IsChannelEnabled(globals.ChatGarblerChannelsBitfield);
                    string checkboxLabel = channel.ToString();

                    if (ImGui.Checkbox(checkboxLabel, ref enabled))
                    {
                        var newBitfield = channel.SetChannelState(globals.ChatGarblerChannelsBitfield, enabled);
                        PermissionHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.ChatGarblerChannelsBitfield), newBitfield)
                            .ConfigureAwait(false);
                    }

                    // Only SameLine if not the third column
                    if ((i + 1) % 3 != 0 && (i + 1) != channels.Length)
                        ImGui.SameLine();
                }

                ImGui.NewLine();
            }

            ImGui.NewLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(GSLoc.Settings.Preferences.LangDialectLabel);
            ImGui.SameLine();

            // voodoo magic from old code i cant be asked to polish.
            if(ImGuiUtil.GenericEnumCombo("##Language", 65, _mainConfig.Current.Language, out var newLang, i => i.ToName()))
            {
                if(newLang != _mainConfig.Current.Language)
                    _mainConfig.Current.LanguageDialect = newLang.GetDialects().First();

                _mainConfig.Current.Language = newLang;
                _mainConfig.Save();
            }
            CkGui.AttachToolTip(GSLoc.Settings.Preferences.LangTT);

            ImGui.SameLine();
            if(ImGuiUtil.GenericEnumCombo("##Dialect", 55, _mainConfig.Current.LanguageDialect, out var newDialect,
                _mainConfig.Current.Language.GetDialects(), i => i.ToName()))
            {
                _mainConfig.Current.LanguageDialect = newDialect;
                _mainConfig.Save();
            }
            CkGui.AttachToolTip(GSLoc.Settings.Preferences.DialectTT);
        }
    }
    private void DrawPreferences()
    {
        DrawChannelPreferences();

        ImGui.NextColumn();
        CkGui.GagspeakBigText(GSLoc.Settings.Preferences.HeaderPuppet);
        using (ImRaii.Group())
        {
            foreach (var (label, channels) in ChatLogAgent.SortedChannels)
            {
                ImGui.Text(label); // Show the group label

                for (int i = 0; i < channels.Length; i++)
                {
                    var channel = channels[i];
                    var enabled = channel.IsChannelEnabled(_mainConfig.Current.PuppeteerChannelsBitfield);
                    string checkboxLabel = channel.ToString();

                    if (ImGui.Checkbox(checkboxLabel, ref enabled))
                    {
                        var newBitfield = channel.SetChannelState(_mainConfig.Current.PuppeteerChannelsBitfield, enabled);
                        _mainConfig.Current.PuppeteerChannelsBitfield = newBitfield;
                        _mainConfig.Save();
                    }

                    // Only SameLine if not the third column
                    if ((i + 1) % 3 != 0 && (i + 1) != channels.Length)
                        ImGui.SameLine();
                }

                ImGui.NewLine();
            }
        }
        ImGui.Columns(1);

        ImGui.Separator();
        CkGui.GagspeakBigText(GSLoc.Settings.Preferences.HeaderUiPrefs);

        var showMainUiOnStart = _mainConfig.Current.OpenMainUiOnStartup;

        var enableDtrEntry = _mainConfig.Current.EnableDtrEntry;
        var dtrPrivacyRadar = _mainConfig.Current.ShowPrivacyRadar;
        var dtrActionNotifs = _mainConfig.Current.ShowActionNotifs;
        var dtrVibeStatus = _mainConfig.Current.ShowVibeStatus;

        var preferThreeCharaAnonName = _mainConfig.Current.PreferThreeCharaAnonName;
        var preferNicknamesInsteadOfName = _mainConfig.Current.PreferNicknamesOverNames;
        var showVisibleSeparate = _mainConfig.Current.ShowVisibleUsersSeparately;
        var showOfflineSeparate = _mainConfig.Current.ShowOfflineUsersSeparately;

        var showProfiles = _mainConfig.Current.ShowProfiles;
        var profileDelay = _mainConfig.Current.ProfileDelay;
        var showContextMenus = _mainConfig.Current.ShowContextMenus;

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowMainUiOnStartLabel, ref showMainUiOnStart))
        {
            _mainConfig.Current.OpenMainUiOnStartup = showMainUiOnStart;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowMainUiOnStartTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.EnableDtrLabel, ref enableDtrEntry))
        {
            _mainConfig.Current.EnableDtrEntry = enableDtrEntry;
            if (enableDtrEntry is false)
            {
                _mainConfig.Current.ShowPrivacyRadar = false;
                _mainConfig.Current.ShowActionNotifs = false;
                _mainConfig.Current.ShowVibeStatus = false;
            }
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.EnableDtrTT);

        using (ImRaii.Disabled(!enableDtrEntry))
        {
            ImGui.Indent();
            if (ImGui.Checkbox(GSLoc.Settings.Preferences.PrivacyRadarLabel, ref dtrPrivacyRadar))
            {
                _mainConfig.Current.ShowPrivacyRadar = dtrPrivacyRadar;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.PrivacyRadarTT);

            if (ImGui.Checkbox(GSLoc.Settings.Preferences.ActionsNotifLabel, ref dtrActionNotifs))
            {
                _mainConfig.Current.ShowActionNotifs = dtrActionNotifs;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.ActionsNotifTT);

            if (ImGui.Checkbox(GSLoc.Settings.Preferences.VibeStatusLabel, ref dtrVibeStatus))
            {
                _mainConfig.Current.ShowVibeStatus = dtrVibeStatus;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.VibeStatusTT);
            ImGui.Unindent();
        }

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowVisibleSeparateLabel, ref showVisibleSeparate))
        {
            _mainConfig.Current.ShowVisibleUsersSeparately = showVisibleSeparate;
            _mainConfig.Save();
            Mediator.Publish(new RefreshUiMessage());
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowVisibleSeparateTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowOfflineSeparateLabel, ref showOfflineSeparate))
        {
            _mainConfig.Current.ShowOfflineUsersSeparately = showOfflineSeparate;
            _mainConfig.Save();
            Mediator.Publish(new RefreshUiMessage());
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowOfflineSeparateTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.PrefThreeCharaAnonName, ref preferThreeCharaAnonName))
        {
            _mainConfig.Current.PreferThreeCharaAnonName = preferThreeCharaAnonName;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.PrefThreeCharaAnonNameTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.PreferNicknamesLabel, ref preferNicknamesInsteadOfName))
        {
            _mainConfig.Current.PreferNicknamesOverNames = preferNicknamesInsteadOfName;
            _mainConfig.Save();
            Mediator.Publish(new RefreshUiMessage());
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.PreferNicknamesTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowProfilesLabel, ref showProfiles))
        {
            Mediator.Publish(new ClearProfileDataMessage());
            _mainConfig.Current.ShowProfiles = showProfiles;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowProfilesTT);

        using (ImRaii.Disabled(!showProfiles))
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (ImGui.SliderFloat(GSLoc.Settings.Preferences.ProfileDelayLabel, ref profileDelay, 0.3f, 5))
            {
                _mainConfig.Current.ProfileDelay = profileDelay;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.ProfileDelayTT);
            ImGui.Unindent();
        }

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ContextMenusLabel, ref showContextMenus))
        {
            _mainConfig.Current.ShowContextMenus = showContextMenus;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ContextMenusTT);

        /* --------------- Separator for moving onto the Notifications Section ----------- */
        ImGui.Separator();
        CkGui.GagspeakBigText(GSLoc.Settings.Preferences.HeaderNotifications);

        var liveGarblerZoneChangeWarn = _mainConfig.Current.LiveGarblerZoneChangeWarn;
        var serverConnectionNotifs = _mainConfig.Current.NotifyForServerConnections;
        var onlineNotifs = _mainConfig.Current.NotifyForOnlinePairs;
        var onlineNotifsNickLimited = _mainConfig.Current.NotifyLimitToNickedPairs;

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ZoneChangeWarnLabel, ref liveGarblerZoneChangeWarn))
        {
            _mainConfig.Current.LiveGarblerZoneChangeWarn = liveGarblerZoneChangeWarn;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ZoneChangeWarnTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ConnectedNotifLabel, ref serverConnectionNotifs))
        {
            _mainConfig.Current.NotifyForServerConnections = serverConnectionNotifs;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ConnectedNotifTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.OnlineNotifLabel, ref onlineNotifs))
        {
            _mainConfig.Current.NotifyForOnlinePairs = onlineNotifs;
            if (!onlineNotifs) _mainConfig.Current.NotifyLimitToNickedPairs = false;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.OnlineNotifTT);

        using (ImRaii.Disabled(!onlineNotifs))
        {
            if (ImGui.Checkbox(GSLoc.Settings.Preferences.LimitForNicksLabel, ref onlineNotifsNickLimited))
            {
                _mainConfig.Current.NotifyLimitToNickedPairs = onlineNotifsNickLimited;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.LimitForNicksTT);
        }

        if(ImGuiUtil.GenericEnumCombo("Info Location##notifInfo", 125f, _mainConfig.Current.InfoNotification, out var newInfo, i => i.ToString()))
        {
            _mainConfig.Current.InfoNotification = newInfo;
            _mainConfig.Save();
        }
        CkGui.HelpText("The location where \"Info\" notifications will display."
                      + Environment.NewLine + "'Nowhere' will not show any Info notifications"
                      + Environment.NewLine + "'Chat' will print Info notifications in chat"
                      + Environment.NewLine + "'Toast' will show Warning toast notifications in the bottom right corner"
                      + Environment.NewLine + "'Both' will show chat as well as the toast notification");

        if (ImGuiUtil.GenericEnumCombo("Warning Location##notifWarn", 125f, _mainConfig.Current.WarningNotification, out var newWarn, i => i.ToString()))
        {
            _mainConfig.Current.WarningNotification = newWarn;
            _mainConfig.Save();
        }
        CkGui.HelpText("The location where \"Warning\" notifications will display."
                              + Environment.NewLine + "'Nowhere' will not show any Warning notifications"
                              + Environment.NewLine + "'Chat' will print Warning notifications in chat"
                              + Environment.NewLine + "'Toast' will show Warning toast notifications in the bottom right corner"
                              + Environment.NewLine + "'Both' will show chat as well as the toast notification");

        if (ImGuiUtil.GenericEnumCombo("Error Location##notifError", 125f, _mainConfig.Current.ErrorNotification, out var newError, i => i.ToString()))
        {
            _mainConfig.Current.ErrorNotification = newError;
            _mainConfig.Save();
        }
        CkGui.HelpText("The location where \"Error\" notifications will display."
                              + Environment.NewLine + "'Nowhere' will not show any Error notifications"
                              + Environment.NewLine + "'Chat' will print Error notifications in chat"
                              + Environment.NewLine + "'Toast' will show Error toast notifications in the bottom right corner"
                              + Environment.NewLine + "'Both' will show chat as well as the toast notification");
    }
}
