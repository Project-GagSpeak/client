using CkCommons.GarblerCore;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Interop;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui;

public class SettingsUi : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly AccountManagerTab _accountsTab;
    private readonly DebugTab _debugTab;
    private readonly PiShockProvider _shockProvider;
    private readonly VfxSpawnManager _vfxSpawner;
    private readonly MainConfig _mainConfig;

    public SettingsUi(ILogger<SettingsUi> logger, GagspeakMediator mediator, MainHub hub,
        AccountManagerTab accounts, DebugTab debug, PiShockProvider shockProvider,
        VfxSpawnManager vfxSpawner, MainConfig config) : base(logger, mediator, "GagSpeak Settings")
    {
        _hub = hub;
        _accountsTab = accounts;
        _debugTab = debug;
        _shockProvider = shockProvider;
        _vfxSpawner = vfxSpawner;
        _mainConfig = config;

        Flags = WFlags.NoScrollbar;
        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
#if DEBUG
        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.Tshirt, "Open Active State Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugActiveStateUI))))
            .Add(FAI.PersonRays, "Open Personal Data Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugPersonalDataUI))))
            .Add(FAI.Database, "Open Storages Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugStorageUI))))
            .Build();
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
        var check = FAI.Check;
        var cross = FAI.SquareXmark;
        CkGui.ColorText(GSLoc.Settings.OptionalPlugins, ImGuiColors.ParsedGold);

        CkGui.TextInline("Penumbra");
        ImGui.SameLine();
        CkGui.IconText(IpcCallerPenumbra.APIAvailable ? check : cross, CkGui.GetBoolColor(IpcCallerPenumbra.APIAvailable));
        CkGui.AttachToolTip(IpcCallerPenumbra.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);

        CkGui.TextInline("Glamourer", false);
        ImGui.SameLine();
        CkGui.IconText(IpcCallerGlamourer.APIAvailable ? check : cross, CkGui.GetBoolColor(IpcCallerGlamourer.APIAvailable));
        CkGui.AttachToolTip(IpcCallerGlamourer.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);

        CkGui.TextInline("Customize +", false);
        ImGui.SameLine();
        CkGui.IconText(IpcCallerCustomize.APIAvailable ? check : cross, CkGui.GetBoolColor(IpcCallerCustomize.APIAvailable));
        CkGui.AttachToolTip(IpcCallerCustomize.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);

        CkGui.TextInline("Moodles", false);
        ImGui.SameLine();
        CkGui.IconText(IpcCallerMoodles.APIAvailable ? check : cross, CkGui.GetBoolColor(IpcCallerMoodles.APIAvailable));
        CkGui.AttachToolTip(IpcCallerMoodles.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);

        CkGui.TextInline("Lifestream", false);
        ImGui.SameLine();
        CkGui.IconText(IpcCallerLifestream.APIAvailable ? check : cross, CkGui.GetBoolColor(IpcCallerLifestream.APIAvailable));
        CkGui.AttachToolTip(IpcCallerLifestream.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);

        CkGui.TextInline("Intiface", false);
        ImGui.SameLine();
        CkGui.IconText(IpcCallerIntiface.APIAvailable ? check : cross, CkGui.GetBoolColor(IpcCallerIntiface.APIAvailable));
        CkGui.AttachToolTip(IpcCallerIntiface.APIAvailable ? GSLoc.Settings.PluginValid : GSLoc.Settings.PluginInvalid);


        CkGui.ColorText(GSLoc.Settings.AccountClaimText, ImGuiColors.ParsedGold);

        ImGui.SameLine();
        if (ImUtf8.SmallButton("CK Discord"))
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

    private void AssignGlobalPermChangeTask(string globalKey, object newValue)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _hub.ChangeOwnGlobalPerm(globalKey, newValue);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                _logger.LogError($"Failed to change global permission {globalKey} to {newValue}. Error: {res.ErrorCode}", LoggerType.UI);
        });
    }

    private void DrawGlobalSettings()
    {
        if (ClientData.Globals is not { } globals)
        {
            ImGui.Text("Global Perms is null! Safely returning early");
            return;
        }

        DrawGagSettings(globals);
        DrawWardrobeSettings(globals);
        DrawPuppeteerSettings(globals);
        DrawToyboxSettings(globals);
        DrawPiShockSettings(globals);
        DrawSpatialAudioSettings(globals);
    }

    private void DrawGagSettings(IReadOnlyGlobalPerms globals)
    {
        var liveChatGarblerActive = globals.ChatGarblerActive;
        var gaggedNamePlates = globals.GaggedNameplate;
        var gagVisuals = globals.GagVisuals;
        var removeGagOnLockExpiration = _mainConfig.Current.RemoveRestrictionOnTimerExpire;

        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderGags, UiFontService.UidFont);
        using (ImRaii.Disabled(globals.ChatGarblerLocked))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.LiveChatGarbler, ref liveChatGarblerActive))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.ChatGarblerActive), liveChatGarblerActive);
            CkGui.HelpText(GSLoc.Settings.MainOptions.LiveChatGarblerTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GaggedNameplates, ref gaggedNamePlates))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.GaggedNameplate), gaggedNamePlates);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GaggedNameplatesTT);
        }

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagGlamours, ref gagVisuals))
            AssignGlobalPermChangeTask(nameof(GlobalPerms.GagVisuals), gagVisuals);
        CkGui.HelpText(GSLoc.Settings.MainOptions.GagGlamoursTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagPadlockTimer, ref removeGagOnLockExpiration))
        {
            _mainConfig.Current.RemoveRestrictionOnTimerExpire = removeGagOnLockExpiration;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.GagPadlockTimerTT);
    }

    private void DrawWardrobeSettings(IReadOnlyGlobalPerms globals)
    {
        var wardrobeEnabled = globals.WardrobeEnabled;
        var restrictionVisuals = globals.RestrictionVisuals;
        var restraintSetVisuals = globals.RestraintSetVisuals;
        var cursedDungeonLoot = _mainConfig.Current.CursedLootPanel;
        var mimicsApplyTraits = _mainConfig.Current.CursedItemsApplyTraits;

        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderWardrobe, UiFontService.UidFont);
        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.WardrobeActive, ref wardrobeEnabled))
        {
            UiService.SetUITask(async () =>
            {
                var res = await _hub.ChangeOwnGlobalPerm(nameof(GlobalPerms.WardrobeEnabled), wardrobeEnabled);
                if (res.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogError($"Failed to change [WardrobeEnabled] to {wardrobeEnabled}. Error: {res.ErrorCode}", LoggerType.UI);
                    return;
                }

                // Otherwise, process the remaining permissions we should forcibly change if the new state is now false.
                if (!wardrobeEnabled)
                {
                    // If wardrobe is disabled, we should also disable the visuals.
                    var restrictionVisualsOff = await _hub.ChangeOwnGlobalPerm(nameof(GlobalPerms.RestrictionVisuals), false);
                    if (restrictionVisualsOff.ErrorCode is not GagSpeakApiEc.Success)
                    {
                        _logger.LogError($"Failed to change [RestrictionVisuals] to false. Error: {restrictionVisualsOff.ErrorCode}", LoggerType.UI);
                        return;
                    }
                    var restraintVisualsOff = await _hub.ChangeOwnGlobalPerm(nameof(GlobalPerms.RestraintSetVisuals), false);
                    if (restraintVisualsOff.ErrorCode is not GagSpeakApiEc.Success)
                    {
                        _logger.LogError($"Failed to change [RestraintSetVisuals] to false. Error: {restraintVisualsOff.ErrorCode}", LoggerType.UI);
                        return;
                    }
                }
            });
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.WardrobeActiveTT);

        using (ImRaii.Disabled(!wardrobeEnabled))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestrictionGlamours, ref restrictionVisuals))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.RestrictionVisuals), restrictionVisuals);
            CkGui.HelpText(GSLoc.Settings.MainOptions.RestrictionGlamoursTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintSetGlamour, ref restraintSetVisuals))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.RestraintSetVisuals), restraintSetVisuals);
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
    }

    private void DrawPuppeteerSettings(IReadOnlyGlobalPerms globals)
    {
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderPuppet, UiFontService.UidFont);

        var puppeteerEnabled = globals.PuppeteerEnabled;
        var globalTriggerPhrase = globals.TriggerPhrase;
        var globalPuppetPerms = globals.PuppetPerms;

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.PuppeteerActive, ref puppeteerEnabled))
            AssignGlobalPermChangeTask(nameof(GlobalPerms.PuppeteerEnabled), puppeteerEnabled);
        CkGui.HelpText(GSLoc.Settings.MainOptions.PuppeteerActiveTT);

        using (ImRaii.Disabled(!puppeteerEnabled))
        {
            using var indent = ImRaii.PushIndent();

            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText(GSLoc.Settings.MainOptions.GlobalTriggerPhrase, ref globalTriggerPhrase, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.TriggerPhrase), globalTriggerPhrase);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalTriggerPhraseTT);

            // Correct these!
            var refSits = (globalPuppetPerms & PuppetPerms.Sit) == PuppetPerms.Sit;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalSit, ref refSits))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Sit);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalSitTT);

            var refEmotes = (globalPuppetPerms & PuppetPerms.Emotes) == PuppetPerms.Emotes;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalMotion, ref refEmotes))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Emotes);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalMotionTT);

            var refAlias = (globalPuppetPerms & PuppetPerms.Alias) == PuppetPerms.Alias;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAlias, ref refAlias))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Alias);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalAliasTT);

            var refAllPerms = (globalPuppetPerms & PuppetPerms.All) == PuppetPerms.All;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAll, ref refAllPerms))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.All);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalAllTT);
        }
    }

    private void DrawToyboxSettings(IReadOnlyGlobalPerms globals)
    {
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderToybox, UiFontService.UidFont);

        var toyboxEnabled = globals.ToyboxEnabled;
        var emitSpatialAudio = globals.SpatialAudio;
        var vibeLobbyNickname = _mainConfig.Current.NicknameInVibeRooms;
        var intifaceAutoConnect = _mainConfig.Current.IntifaceAutoConnect;
        var intifaceConnectionAddr = _mainConfig.Current.IntifaceConnectionSocket;

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.ToyboxActive, ref toyboxEnabled))
            AssignGlobalPermChangeTask(nameof(GlobalPerms.ToyboxEnabled), toyboxEnabled);
        CkGui.HelpText(GSLoc.Settings.MainOptions.ToyboxActiveTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.SpatialAudioActive, ref emitSpatialAudio))
            AssignGlobalPermChangeTask(nameof(GlobalPerms.SpatialAudio), emitSpatialAudio);
        CkGui.HelpText(GSLoc.Settings.MainOptions.SpatialAudioActiveTT);

        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText(GSLoc.Settings.MainOptions.VibeLobbyNickname, ref vibeLobbyNickname, 25, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _mainConfig.Current.NicknameInVibeRooms = vibeLobbyNickname;
            _mainConfig.Save();
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.VibeLobbyNicknameTT);

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
                intifaceConnectionAddr = "ws://localhost:12345";
            else
            {
                _mainConfig.Current.IntifaceConnectionSocket = intifaceConnectionAddr;
                _mainConfig.Save();
            }
        }
        CkGui.HelpText(GSLoc.Settings.MainOptions.IntifaceAddressTT);


    }

    private void DrawPiShockSettings(IReadOnlyGlobalPerms globals)
    {
        var apiKey = _mainConfig.Current.PiShockApiKey;
        var username = _mainConfig.Current.PiShockUsername;
        var shareCode = globals.GlobalShockShareCode;
        var allowShocks = globals.AllowShocks;
        var allowVibrate = globals.AllowVibrations;
        var allowBeep = globals.AllowBeeps;
        var maxShockIntensity = globals.MaxIntensity;
        var maxShockTime = globals.GetTimespanFromDuration();
        var maxVibrateTime = (int)globals.ShockVibrateDuration.TotalSeconds;

        using var node = ImRaii.TreeNode("Pi-Shock Global Settings");
        if (node)
        {
            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText("PiShock API Key", ref apiKey, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _mainConfig.Current.PiShockApiKey = apiKey;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockKeyTT);

            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText("PiShock Username", ref username, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                _mainConfig.Current.PiShockUsername = username;
                _mainConfig.Save();
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockUsernameTT);


            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale - CkGui.IconTextButtonSize(FAI.Sync, "Refresh") - ImGui.GetStyle().ItemInnerSpacing.X);
            if (ImGui.InputText("##Global PiShock Share Code", ref shareCode, 100, ImGuiInputTextFlags.EnterReturnsTrue))
                AssignGlobalPermChangeTask(nameof(GlobalPerms.GlobalShockShareCode), shareCode);

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Sync, "Refresh", disabled: UiService.DisableUI))
            {
                UiService.SetUITask(async () =>
                {
                    if (ClientData.IsNull)
                        return;

                    var shareCodePerms = await _shockProvider.GetPermissionsFromCode(globals.GlobalShockShareCode);
                    var WithShockPerms = (GlobalPerms)ClientData.Globals! with 
                    {
                        AllowShocks = shareCodePerms.AllowShocks,
                        AllowVibrations = shareCodePerms.AllowVibrations,
                        AllowBeeps = shareCodePerms.AllowBeeps,
                        MaxDuration = shareCodePerms.MaxDuration,
                        MaxIntensity = shareCodePerms.MaxIntensity
                    };
                    await _hub.UserBulkChangeGlobal(new(MainHub.PlayerUserData, WithShockPerms, (HardcoreState)ClientData.Hardcore!));
                });
            }
            CkGui.AttachToolTip(GSLoc.Settings.MainOptions.PiShockShareCodeRefreshTT);

            ImUtf8.SameLineInner();
            ImGui.TextUnformatted(GSLoc.Settings.MainOptions.PiShockShareCode);
            CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockShareCodeTT);

            ImGui.SetNextItemWidth(250 * ImGuiHelpers.GlobalScale);
            ImGui.SliderInt(GSLoc.Settings.MainOptions.PiShockVibeTime, ref maxVibrateTime, 0, 30);
            if (ImGui.IsItemDeactivatedAfterEdit())
                AssignGlobalPermChangeTask(nameof(GlobalPerms.ShockVibrateDuration), (ulong)TimeSpan.FromSeconds(maxVibrateTime).Ticks);
            CkGui.HelpText(GSLoc.Settings.MainOptions.PiShockVibeTimeTT);

            CkGui.ColorText(GSLoc.Settings.MainOptions.PiShockPermsLabel, ImGuiColors.ParsedGold);
            using (ImRaii.Group())
            {
                CkGui.ColorTextBool(GSLoc.Settings.MainOptions.PiShockAllowShocks, allowShocks);
                ImGui.SameLine();
                CkGui.ColorTextBool(GSLoc.Settings.MainOptions.PiShockAllowVibes, allowVibrate);
                ImGui.SameLine();
                CkGui.ColorTextBool(GSLoc.Settings.MainOptions.PiShockAllowBeeps, allowBeep);

                CkGui.FrameVerticalSeparator(2);

                ImUtf8.TextFrameAligned(GSLoc.Settings.MainOptions.PiShockMaxShockIntensity);
                CkGui.ColorTextFrameAlignedInline(maxShockIntensity.ToString() + "%", ImGuiColors.ParsedGold);

                CkGui.FrameVerticalSeparator(2);

                ImUtf8.TextFrameAligned(GSLoc.Settings.MainOptions.PiShockMaxShockDuration);
                var maxGlobalShockDuration = globals.GetTimespanFromDuration();
                CkGui.ColorTextFrameAlignedInline($"{maxGlobalShockDuration.Seconds}.{maxGlobalShockDuration.Milliseconds}s", ImGuiColors.ParsedGold);
            }
        }
    }

    private void DrawSpatialAudioSettings(IReadOnlyGlobalPerms globals)
    {
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderAudio, UiFontService.UidFont);

        //if (CkGuiUtils.EnumCombo("##AudioType", 150f, _mainConfig.Current.AudioOutputType, out var newVal, defaultText: "Select Audio Type.."))
        //{
        //    _mainConfig.Current.AudioOutputType = newVal;
        //    _mainConfig.Save();
        //    AudioSystem.InitializeOutputDevice(newVal, _mainConfig.GetDefaultAudioDevice());
        //}

        //// the Dropdown based on the type.
        //switch (_mainConfig.Current.AudioOutputType)
        //{
        //    case OutputType.DirectSound:
        //        if (CkGuiUtils.GuidCombo("##DirectOutDevice", 150f, _mainConfig.Current.DirectOutDevice, out var newDirectDevice, AudioSystem.DirectSoundAudioDevices.Keys,
        //            d => AudioSystem.DirectSoundAudioDevices.GetValueOrDefault(d, "Unknown Device"), defaultText: "Select Device.."))
        //        {
        //            _mainConfig.Current.DirectOutDevice = newDirectDevice;
        //            _mainConfig.Save();
        //            AudioSystem.InitializeOutputDevice(_mainConfig.Current.AudioOutputType, newDirectDevice.ToString());
        //        }
        //        break;

        //    case OutputType.Asio:
        //        if (CkGuiUtils.StringCombo("##AsioDevice", 150f, _mainConfig.Current.AsioDevice, out var newAsioDevice, AudioSystem.AsioAudioDevices, "Select ASIO Device.."))
        //        {
        //            _mainConfig.Current.AsioDevice = newAsioDevice;
        //            _mainConfig.Save();
        //            AudioSystem.InitializeOutputDevice(_mainConfig.Current.AudioOutputType, newAsioDevice);
        //        }
        //        break;

        //    case OutputType.Wasapi:
        //        var deviceId = AudioSystem.WasapiAudioDevices.GetValueOrDefault(_mainConfig.Current.WasapiDevice, string.Empty);
        //        if (CkGuiUtils.StringCombo("##WasapiDevice", 150f, deviceId, out var newWasapiDevice, AudioSystem.WasapiAudioDevices.Values, "Select WASAPI Device.."))
        //        {
        //            // we got the value so we need to get its corrisponding key.
        //            var finalDeviceId = AudioSystem.WasapiAudioDevices.FirstOrDefault(x => x.Value == newWasapiDevice).Key;
        //            _mainConfig.Current.WasapiDevice = finalDeviceId;
        //            _mainConfig.Save();
        //            AudioSystem.InitializeOutputDevice(_mainConfig.Current.AudioOutputType, finalDeviceId);
        //        }
        //        break;
        //    default:
        //        throw new ArgumentOutOfRangeException();
        //}

        //if (CkGui.IconTextButton(FAI.Sync, "Refresh audio devices", disabled: UiService.DisableUI))
        //    AudioSystem.FetchLatestAudioDevices();
        //CkGui.AttachToolTip("Refreshes the list of audio devices available for selection.\n" +
        //                    "This is useful if you have changed your audio devices while the game was running.");

        ImGui.InputTextWithHint("##VfxPathFileLabel", "Vfx Path In Audio Folder", ref _currentVfxPath, 300, ITFlags.EnterReturnsTrue);

        _vfxSpawner.DrawVfxSpawnOptions(_currentVfxPath, false);
    }
    private string _currentVfxPath = string.Empty;

    private void DrawChannelPreferences()
    {
        // do not draw the preferences if the globalpermissions are null.
        if(ClientData.Globals is not { } globals)
        {
            ImGui.Text("Globals is null! Returning early");
            return;
        }

        var width = ImGui.GetContentRegionAvail().X / 2;
        ImGui.Columns(2, "PreferencesColumns", true);
        ImGui.SetColumnWidth(0, width);

        CkGui.FontText("Live Chat Garbler", UiFontService.UidFont);

        using (ImRaii.Group())
        {
            foreach (var (label, channels) in ChatLogAgent.SortedChannels)
            {
                ImGui.Text(label); // Show the group label

                for (var i = 0; i < channels.Length; i++)
                {
                    var channel = channels[i];
                    var enabled = globals.AllowedGarblerChannels.IsActiveChannel((int)channel);
                    var checkboxLabel = channel.ToString();

                    if (ImGui.Checkbox(checkboxLabel, ref enabled))
                    {
                        var newBitfield = globals.AllowedGarblerChannels.SetChannelState((int)channel, enabled);
                        AssignGlobalPermChangeTask(nameof(GlobalPerms.AllowedGarblerChannels), newBitfield);
                    }

                    // Only SameLine if not the third column
                    if ((i + 1) % 4 != 0 && (i + 1) != channels.Length)
                        ImGui.SameLine();
                }
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
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderPuppet, UiFontService.UidFont);
        using (ImRaii.Group())
        {
            foreach (var (label, channels) in ChatLogAgent.SortedChannels)
            {
                ImGui.Text(label); // Show the group label

                for (var i = 0; i < channels.Length; i++)
                {
                    var channel = channels[i];
                    var enabled = _mainConfig.Current.PuppeteerChannelsBitfield.IsActiveChannel((int)channel);
                    var checkboxLabel = channel.ToString();

                    if (ImGui.Checkbox(checkboxLabel, ref enabled))
                    {
                        var newBitfield = _mainConfig.Current.PuppeteerChannelsBitfield.SetChannelState((int)channel, enabled);
                        _mainConfig.Current.PuppeteerChannelsBitfield = newBitfield;
                        _mainConfig.Save();
                    }

                    // Only SameLine if not the third column
                    if ((i + 1) % 4 != 0 && (i + 1) != channels.Length)
                        ImGui.SameLine();
                }
            }
        }
        ImGui.Columns(1);

        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderUiPrefs, UiFontService.UidFont);

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
            Mediator.Publish(new RefreshUiKinkstersMessage());
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowVisibleSeparateTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowOfflineSeparateLabel, ref showOfflineSeparate))
        {
            _mainConfig.Current.ShowOfflineUsersSeparately = showOfflineSeparate;
            _mainConfig.Save();
            Mediator.Publish(new RefreshUiKinkstersMessage());
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
            Mediator.Publish(new RefreshUiKinkstersMessage());
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
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderNotifications, UiFontService.UidFont);

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
