using CkCommons;
using CkCommons.GarblerCore;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using GagspeakAPI.User;
using OtterGui;
using OtterGui.Text;
using System.Windows.Forms;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace GagSpeak.Gui;

public class SettingsUi : WindowMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly ProfilesTab _accountsTab;
    private readonly DebugTab _debugTab;
    private readonly PiShockProvider _shockProvider;
    private readonly ClientDataListener _clientDatListener;
    private readonly PluginGuideProvider _guideProvider;
    private readonly UiFileDialogService _fileDialog;
    private readonly HardcoreEscapeService _escape;

    private static bool _isLinux;
    private OptionalPlugin _expandedInfo = OptionalPlugin.None;

    public SettingsUi(ILogger<SettingsUi> logger, GagspeakMediator mediator, MainHub hub,
        MainConfig config, ProfilesTab accounts, DebugTab debug, PiShockProvider shockProvider,
        ClientDataListener listener, PluginGuideProvider guide, UiFileDialogService fileDialog,
        HardcoreEscapeService escape)
        : base(logger, mediator, "GagSpeak Settings")
    {
        _hub = hub;
        _config = config;
        _accountsTab = accounts;
        _debugTab = debug;
        _shockProvider = shockProvider;
        _clientDatListener = listener;
        _guideProvider = guide;
        _fileDialog = fileDialog;
        _escape = escape;

        Flags = WFlags.NoScrollbar;
        this.PinningClickthroughFalse();
        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
        _isLinux = Util.IsWine();

        TitleBarButtons = new TitleBarButtonBuilder()
            .Add(FAI.Tshirt, "Open Active State Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugActiveStateUI))))
            .Add(FAI.PersonRays, "Open Personal Data Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugPersonalDataUI))))
            .Add(FAI.Database, "Open Storages Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugStorageUI))))
            .Add(FAI.Bell, "Actions Notifier", () => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI))))
            .Build();
    }

    protected override void DrawInternal()
    {
        var minPos = ImGui.GetCursorPos();
        var rWidth = ImGui.CalcTextSize("Configs").X + ImUtf8.FrameHeight + ImUtf8.ItemSpacing.X * 4;
        var leftLength = ImGui.GetContentRegionAvail().X - rWidth;
        var buttonPos = minPos + new Vector2(leftLength, 0);
        using (ImRaii.Group())
        {
            ImGui.Text(GSLoc.Settings.OptionalPlugins);
            ImGui.SameLine();
            DrawOptionalPluginButton("Sundouleia", IpcCallerSundouleia.APIAvailable, OptionalPlugin.Sundouleia, true);
            ImGui.SameLine();
            DrawOptionalPluginButton("Penumbra", IpcCallerPenumbra.APIAvailable, OptionalPlugin.Penumbra, true);
            ImGui.SameLine();
            DrawOptionalPluginButton("Glamourer", IpcCallerGlamourer.APIAvailable, OptionalPlugin.Glamourer, true);
            ImGui.SameLine();
            DrawOptionalPluginButton("CPlus", IpcCallerCustomize.APIAvailable, OptionalPlugin.CustomizePlus, false);
            ImGui.SameLine();
            DrawOptionalPluginButton("Loci", IpcCallerLoci.APIAvailable, OptionalPlugin.Loci, true);
            ImGui.SameLine();
            DrawOptionalPluginButton("Lifestream", IpcCallerLifestream.APIAvailable, OptionalPlugin.Lifestream, false);
            ImGui.SameLine();
            DrawOptionalPluginButton("Intiface", IpcCallerIntiface.APIAvailable, OptionalPlugin.Intiface, false);

            // Below it, draw out the plugin details if we should.
            _guideProvider.DrawOptionalPluginDetails(_expandedInfo, leftLength);

            ImGui.Text(GSLoc.Settings.AccountClaimText);
            ImGui.SameLine();
            if (ImUtf8.SmallButton("CK Discord"))
                Util.OpenLink("https://discord.gg/kinkporium");
        }

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

                if (ImGui.BeginTabItem("Notifications"))
                {
                    DrawAlertLocations();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Online Users"))
                {
                    DrawOnlineUserOptions();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(GSLoc.Settings.TabsVanity))
                {
                    DrawVanity();
                    ImGui.EndTabItem();
                }
            }

            if (ImGui.BeginTabItem(GSLoc.Settings.TabsAccounts))
            {
                _accountsTab.DrawContent();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug"))
            {
                _debugTab.DrawDebugMain();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.SetCursorPos(buttonPos);
        using (ImRaii.Group())
        {
            if (CkGui.FancyButton(FAI.Palette, "Styler", rWidth, false))
                Mediator.Publish(new UiToggleMessage(typeof(StyleEditorUI)));
            CkGui.AttachTooltip("Edit Style (very WIP and incomplete, use at your own risk)");

            if (CkGui.FancyButton(FAI.Folder, "Configs", rWidth, false))
            {
                try { Process.Start(new ProcessStartInfo { FileName = GsFiles.ConfigDirectory, UseShellExecute = true }); }
                catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the config directory. {e.Message}"); }
            }
            CkGui.AttachTooltip("Opens the Config Folder.--NL--(Useful for debugging)");
        }
    }

    private void DrawOptionalPluginButton(string name, bool apiAvailable, OptionalPlugin plugin, bool recommended, string tooltip = "Click to see more info!")
    {
        var showWarn = !apiAvailable && recommended;
        using (ImRaii.Group())
        {
            CkGui.ColorTextBool(name, apiAvailable);
            // Show yellow caution if unavailable
            if (showWarn)
            {
                ImGui.SameLine(0, 1);
                CkGui.ColorText("⚠", ImGuiColors.DalamudYellow);
            }
        }

        var ttText = showWarn ? $"{tooltip}--SEP----COL--Recommended plugin {plugin} is not installed or up to date!--COL--" : tooltip;
        CkGui.AttachTooltip(ttText, ImGuiColors.DalamudYellow);

        // If this is our currently expanded plugin, draw a rect ring around it in yellow.
        if (_expandedInfo == plugin)
        {
            var min = ImGui.GetItemRectMin() - ImUtf8.FramePadding;
            var max = ImGui.GetItemRectMax() + ImUtf8.FramePadding;
            ImGui.GetWindowDrawList().AddRect(min, max, CkCol.Favorite.Uint(), 5f, DFlags.RoundCornersTop, 2);
        }

        // Otherwise, if clicked, toggle the info.
        if (ImGui.IsItemClicked())
            _expandedInfo = (_expandedInfo == plugin) ? OptionalPlugin.None : plugin;
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

    private void AssignGlobalPermChangeTask(GlobalPerms perms, string globalKey, object newValue)
        => UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, perms, globalKey, newValue));

    private void AssignShockPermBulkTask(GlobalPerms perms, GlobalPerms updated)
        => UiService.SetUITask(async () =>
        {
            if (ClientData.IsNull) return;
            var res = await _hub.UserBulkChangeGlobal(new(MainHub.OwnUserData, updated, ClientData.HardcoreClone() ?? new HardcoreState()));
            if (res.ErrorCode is GagSpeakApiEc.Success)
                _clientDatListener.ChangeAllGlobalPerms(updated);
        });

    // Do this better at some point!
    private void DrawGagSettings(GlobalPerms globals)
    {
        var liveChatGarblerActive = globals.ChatGarblerActive;
        var gaggedNamePlates = globals.GaggedNameplate;
        var gagVisuals = globals.GagVisuals;
        var removeGagOnLockExpiration = _config.Data.RemoveGagOnTimerExpire;
        var garbleWordsNotInDictionary = _config.Data.GarbleWordsNotInDictionary;

        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderGags, Fonts.SubtitleFont);
        using (ImRaii.Disabled(globals.ChatGarblerLocked))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.LiveChatGarbler, ref liveChatGarblerActive))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.ChatGarblerActive), liveChatGarblerActive);
            CkGui.HelpText(GSLoc.Settings.MainOptions.LiveChatGarblerTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GaggedNameplates, ref gaggedNamePlates))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.GaggedNameplate), gaggedNamePlates);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GaggedNameplatesTT);

            // TODO: This could be a global permission, not just a config option, but we'll tie it to garbler lock for now either way
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.NotInDictionaryGarbling, ref garbleWordsNotInDictionary))
            {
                _config.Data.GarbleWordsNotInDictionary = garbleWordsNotInDictionary;
                _config.Save();
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.NotInDictionaryGarblingTT);
        }

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagGlamours, ref gagVisuals))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.GagVisuals), gagVisuals);
        CkGui.HelpText(GSLoc.Settings.MainOptions.GagGlamoursTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GagPadlockTimer, ref removeGagOnLockExpiration))
        {
            _config.Data.RemoveGagOnTimerExpire = removeGagOnLockExpiration;
            _config.Save();
        }

        CkGui.HelpText(GSLoc.Settings.MainOptions.GagPadlockTimerTT);
    }

    private void DrawWardrobeSettings(GlobalPerms globals)
    {
        var wardrobeEnabled = globals.WardrobeEnabled;
        var restrictionVisuals = globals.RestrictionVisuals;
        var restraintSetVisuals = globals.RestraintSetVisuals;
        var cursedDungeonLoot = _config.Data.CursedLootUI;
        var mimicsApplyTraits = _config.Data.CursedItemsApplyTraits;
        var mimicsApplyOverlays = _config.Data.CursedItemsApplyOverlays;
        var removeRestrictionOnLockExpiration = _config.Data.RemoveRestrictionOnTimerExpire;
        var removeRestraintOnLockExpiration = _config.Data.RemoveRestraintOnTimerExpire;
        var blindfoldMaxOpacity = _config.Data.OverlayMaxOpacity;
        var hardcoreEscape = _config.Data.HardcoreEscape;

        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderWardrobe, Fonts.SubtitleFont);
        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.WardrobeActive, ref wardrobeEnabled))
        {
            UiService.SetUITask(async () =>
            {
                var success = await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.WardrobeEnabled), wardrobeEnabled);
                // Otherwise, process the remaining permissions we should forcibly change if the new state is now false.
                if (success && !wardrobeEnabled)
                {
                    // If wardrobe is disabled, we should also disable the visuals.
                    await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestrictionVisuals), false);
                    await PermHelper.ChangeOwnGlobal(_hub, globals, nameof(GlobalPerms.RestraintSetVisuals), false);
                }
            });
        }

        CkGui.HelpText(GSLoc.Settings.MainOptions.WardrobeActiveTT);

        using (ImRaii.Disabled(!wardrobeEnabled))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestrictionGlamours, ref restrictionVisuals))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.RestrictionVisuals), restrictionVisuals);
            CkGui.HelpText(GSLoc.Settings.MainOptions.RestrictionGlamoursTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestrictionPadlockTimer, ref removeRestrictionOnLockExpiration))
            {
                _config.Data.RemoveRestrictionOnTimerExpire = removeRestrictionOnLockExpiration;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.MainOptions.RestrictionPadlockTimerTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintSetGlamour, ref restraintSetVisuals))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.RestraintSetVisuals), restraintSetVisuals);
            CkGui.HelpText(GSLoc.Settings.MainOptions.RestraintSetGlamourTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.RestraintPadlockTimer, ref removeRestraintOnLockExpiration))
            {
                _config.Data.RemoveRestraintOnTimerExpire = removeRestraintOnLockExpiration;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.MainOptions.RestraintPadlockTimerTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.CursedLootActive, ref cursedDungeonLoot))
            {
                _config.Data.CursedLootUI = cursedDungeonLoot;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.MainOptions.CursedLootActiveTT);

            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.MimicsApplyTraits, ref mimicsApplyTraits))
            {
                _config.Data.CursedItemsApplyTraits = mimicsApplyTraits;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.MainOptions.MimicsApplyTraitsTT);
            
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.MimicsApplyOverlays, ref mimicsApplyOverlays))
            {
                _config.Data.CursedItemsApplyOverlays = mimicsApplyOverlays;
                _config.Save();
            }
            CkGui.HelpText(GSLoc.Settings.MainOptions.MimicsApplyOverlaysTT);

            blindfoldMaxOpacity *= 100; // show a prettier value for the end user
            ImGui.SetNextItemWidth(200f);
            if (ImGui.SliderFloat(GSLoc.Settings.MainOptions.OverlayMaxOpacity, ref blindfoldMaxOpacity, 0f, 100f, "%.1f%%", ImGuiSliderFlags.AlwaysClamp))
            {
                _config.Data.OverlayMaxOpacity = blindfoldMaxOpacity / 100;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.MainOptions.OverlayMaxOpacityTT);
        }

        using (ImRaii.Disabled(hardcoreEscape && !_escape.CanDisable))
        {
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.HardcoreEscape, ref hardcoreEscape))
            {
                _config.Data.HardcoreEscape = hardcoreEscape;
                _config.Save();
            }
        }

        CkGui.HelpText(GSLoc.Settings.MainOptions.HardcoreEscapeTT);
    }

    private void DrawPuppeteerSettings(GlobalPerms globals)
    {
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderPuppet, Fonts.SubtitleFont);

        var puppeteerEnabled = globals.PuppeteerEnabled;
        var globalTriggerPhrase = globals.TriggerPhrase;
        var globalPuppetPerms = globals.PuppetPerms;

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.PuppeteerActive, ref puppeteerEnabled))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppeteerEnabled), puppeteerEnabled);
        CkGui.HelpText(GSLoc.Settings.MainOptions.PuppeteerActiveTT);

        using (ImRaii.Disabled(!puppeteerEnabled))
        {
            using var indent = ImRaii.PushIndent();

            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint(GSLoc.Settings.MainOptions.GlobalTriggerPhrase, "Global Triggers...", ref globalTriggerPhrase, 150);
            if (ImGui.IsItemDeactivatedAfterEdit())
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.TriggerPhrase), globalTriggerPhrase);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalTriggerPhraseTT);

            // Correct these!
            var refSits = (globalPuppetPerms & PuppetPerms.Sit) == PuppetPerms.Sit;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalSit, ref refSits))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Sit);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalSitTT);

            var refEmotes = (globalPuppetPerms & PuppetPerms.Emotes) == PuppetPerms.Emotes;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalMotion, ref refEmotes))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Emotes);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalMotionTT);

            var refAlias = (globalPuppetPerms & PuppetPerms.Alias) == PuppetPerms.Alias;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAlias, ref refAlias))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.Alias);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalAliasTT);

            var refAllPerms = (globalPuppetPerms & PuppetPerms.All) == PuppetPerms.All;
            if (ImGui.Checkbox(GSLoc.Settings.MainOptions.GlobalAll, ref refAllPerms))
                AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.PuppetPerms), globalPuppetPerms ^ PuppetPerms.All);
            CkGui.HelpText(GSLoc.Settings.MainOptions.GlobalAllTT);
        }
    }

    private void DrawToyboxSettings(GlobalPerms globals)
    {
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderToybox, Fonts.SubtitleFont);

        var toyboxEnabled = globals.ToyboxEnabled;
        var emitSpatialAudio = globals.SpatialAudio;
        var vibeLobbyNickname = _config.Data.NicknameInVibeRooms;
        var intifaceAutoConnect = _config.Data.IntifaceAutoConnect;
        var intifaceConnectionAddr = _config.Data.IntifaceConnectionSocket;

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.ToyboxActive, ref toyboxEnabled))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.ToyboxEnabled), toyboxEnabled);
        CkGui.HelpText(GSLoc.Settings.MainOptions.ToyboxActiveTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.SpatialAudioActive, ref emitSpatialAudio))
            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.SpatialAudio), emitSpatialAudio);
        CkGui.HelpText(GSLoc.Settings.MainOptions.SpatialAudioActiveTT);

        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText(GSLoc.Settings.MainOptions.VibeLobbyNickname, ref vibeLobbyNickname, 25, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _config.Data.NicknameInVibeRooms = vibeLobbyNickname;
            _config.Save();
        }

        CkGui.HelpText(GSLoc.Settings.MainOptions.VibeLobbyNicknameTT);

        if (ImGui.Checkbox(GSLoc.Settings.MainOptions.IntifaceAutoConnect, ref intifaceAutoConnect))
        {
            _config.Data.IntifaceAutoConnect = intifaceAutoConnect;
            _config.Save();
        }

        CkGui.HelpText(GSLoc.Settings.MainOptions.IntifaceAutoConnectTT);

        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint($"Server Address##ConnectionWSaddr", "Leave blank for default...", ref intifaceConnectionAddr, 100))
        {
            if (!intifaceConnectionAddr.Contains("ws://"))
                intifaceConnectionAddr = "ws://localhost:12345";
            else
            {
                _config.Data.IntifaceConnectionSocket = intifaceConnectionAddr;
                _config.Save();
            }
        }

        CkGui.HelpText(GSLoc.Settings.MainOptions.IntifaceAddressTT);
    }

    private void DrawPiShockSettings(GlobalPerms globals)
    {
        var apiKey = _config.Data.PiShockApiKey;

        using var node = ImRaii.TreeNode("Pi-Shock Settings");
        if (!node) return;


        var inputWidth = 250 * ImGuiHelpers.GlobalScale;
        var saveWidth = CkGui.IconTextButtonSize(FAI.PlugCircleCheck, "Save & Connect");


        ImGui.SetNextItemWidth(inputWidth - saveWidth - ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.InputText("##PiShock API Key", ref apiKey, 100);
        CkGui.AttachTooltip(GSLoc.Settings.MainOptions.PiShockKeyTT);

        ImUtf8.SameLineInner();
        ImGui.TextUnformatted("API Key");

        ImUtf8.SameLineInner();
        if (CkGui.IconTextButton(FAI.PlugCircleCheck, "Save & Connect", disabled: UiService.DisableUI || string.IsNullOrEmpty(apiKey)))
        {
            _config.Data.PiShockApiKey = apiKey;
            _config.Save();
            UiService.SetUITask(async () => await _shockProvider.ConnectAsync());
        }
        CkGui.AttachTooltip("Save your API key and fetch your connected PiShock devices.");

        ImGui.Spacing();

        switch (_shockProvider.LastConnectState)
        {
            case PiShockProvider.ConnectState.NotAttempted:
                if (!_shockProvider.IsConfigured)
                    CkGui.ColorText("Enter your API Key, then click Save & Connect.", ImGuiColors.DalamudGrey);
                else
                    CkGui.ColorText("Click Save & Connect to detect your PiShock devices.", ImGuiColors.DalamudYellow);
                break;

            case PiShockProvider.ConnectState.AuthFailed:
                CkGui.ColorText("Authentication failed - check your API Key.", ImGuiColors.DalamudRed);
                break;

            case PiShockProvider.ConnectState.NetworkError:
                CkGui.ColorText("Connection error - check your internet or PiShock status.", ImGuiColors.DalamudRed);
                break;

            case PiShockProvider.ConnectState.Success when _shockProvider.ShockerCount == 0:
                CkGui.ColorText("Connected - no devices found. Check your PiShock account.", ImGuiColors.DalamudYellow);
                break;

            case PiShockProvider.ConnectState.Success:
            {
                var shockers = _shockProvider.CachedShockers;
                CkGui.ColorText($"{shockers.Count} Shocker(s) found. Default device for triggers:", ImGuiColors.DalamudGrey);

                var currentId   = _config.Data.GlobalShockerId;
                var currentName = shockers.FirstOrDefault(s => s.Id == currentId).Name ?? "Select a device...";
                ImGui.SetNextItemWidth(inputWidth);
                using (var combo = ImRaii.Combo("##GlobalShocker", currentName))
                {
                    if (combo)
                    {
                        foreach (var (id, name) in shockers)
                        {
                            if (ImGui.Selectable(name, id == currentId))
                            {
                                _config.Data.GlobalShockerId = id;
                                _config.Save();
                            }
                        }
                    }
                }
                CkGui.AttachTooltip("The device used when a trigger fires a shock action.");
                ImGui.Spacing();
                CkGui.ColorText("Configure a share code in each pair's permissions.", ImGuiColors.DalamudGrey);
                break;
            }
        }
    }

    private void DrawSpatialAudioSettings(GlobalPerms globals)
    {
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.MainOptions.HeaderAudio, Fonts.SubtitleFont);

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
        //CkGui.AttachTooltip("Refreshes the list of audio devices available for selection.\n" +
        //                    "This is useful if you have changed your audio devices while the game was running.");

        ImGui.InputTextWithHint("##VfxPathFileLabel", "Vfx Path In Audio Folder", ref _currentVfxPath, 300, ITFlags.EnterReturnsTrue);
    }

    private string _currentVfxPath = string.Empty;

    private void DrawChannelPreferences()
    {
        // do not draw the preferences if the globalpermissions are null.
        if (ClientData.Globals is not { } globals)
        {
            ImGui.Text("Globals is null! Returning early");
            return;
        }

        var width = ImGui.GetContentRegionAvail().X / 2;
        ImGui.Columns(2, "PreferencesColumns", true);
        ImGui.SetColumnWidth(0, width);

        CkGui.FontText("Live Chat Garbler", Fonts.SubtitleFont);

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

                    using (ImRaii.Disabled(globals.ChatGarblerLocked && enabled))
                    {
                        if (ImGui.Checkbox(checkboxLabel, ref enabled))
                        {
                            var newBitfield = globals.AllowedGarblerChannels.SetChannelState((int)channel, enabled);
                            AssignGlobalPermChangeTask(globals, nameof(GlobalPerms.AllowedGarblerChannels),
                                                       newBitfield);
                        }
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
            if (ImGuiUtil.GenericEnumCombo("##Language", 65, _config.Data.Language, out var newLang, i => i.ToName()))
            {
                if (newLang != _config.Data.Language)
                    _config.Data.LanguageDialect = newLang.GetDialects().First();

                _config.Data.Language = newLang;
                _config.Save();
            }

            CkGui.AttachTooltip(GSLoc.Settings.Preferences.LangTT);

            ImGui.SameLine();
            if (ImGuiUtil.GenericEnumCombo("##Dialect", 55, _config.Data.LanguageDialect, out var newDialect,
                _config.Data.Language.GetDialects(), i => i.ToName()))
            {
                _config.Data.LanguageDialect = newDialect;
                _config.Save();
            }

            CkGui.AttachTooltip(GSLoc.Settings.Preferences.DialectTT);
        }
    }

    private void DrawPreferences()
    {
        DrawChannelPreferences();

        ImGui.NextColumn();
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderPuppet, Fonts.SubtitleFont);
        using (ImRaii.Group())
        {
            foreach (var (label, channels) in ChatLogAgent.SortedChannels)
            {
                ImGui.Text(label); // Show the group label

                for (var i = 0; i < channels.Length; i++)
                {
                    var channel = channels[i];
                    var enabled = _config.Data.PuppeteerChannelsBitfield.IsActiveChannel((int)channel);
                    var checkboxLabel = channel.ToString() + " "; // space for unique name in ImGui to avoid conflict with garble channels

                    if (ImGui.Checkbox(checkboxLabel, ref enabled))
                    {
                        var newBitfield = _config.Data.PuppeteerChannelsBitfield.SetChannelState((int)channel, enabled);
                        _config.Data.PuppeteerChannelsBitfield = newBitfield;
                        _config.Save();
                    }

                    // Only SameLine if not the third column
                    if ((i + 1) % 4 != 0 && (i + 1) != channels.Length)
                        ImGui.SameLine();
                }
            }
        }

        ImGui.Columns(1);

        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderUiPrefs, Fonts.SubtitleFont);

        var showMainUiOnStart = _config.Data.OpenUiOnStartup;

        var dtrPrivacyRadar = _config.Data.DtrPrivacy;
        var dtrActionNotifs = _config.Data.DtrActionNotifs;
        var dtrVibeStatus = _config.Data.DtrVibeStatus;

        var preferThreeCharaAnonName = _config.Data.UseLegacyAnonName;

        var showProfiles = _config.Data.ShowProfiles;
        var profileDelay = _config.Data.ProfileDelay;
        var showContextMenus = _config.Data.ShowContextMenus;

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowMainUiOnStartLabel, ref showMainUiOnStart))
        {
            _config.Data.OpenUiOnStartup = showMainUiOnStart;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowMainUiOnStartTT);

        using (ImRaii.PushIndent())
        {
            if (ImGui.Checkbox(GSLoc.Settings.Preferences.PrivacyRadarLabel, ref dtrPrivacyRadar))
            {
                _config.Data.DtrPrivacy = dtrPrivacyRadar;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.Preferences.PrivacyRadarTT);

            if (ImGui.Checkbox(GSLoc.Settings.Preferences.ActionsNotifLabel, ref dtrActionNotifs))
            {
                _config.Data.DtrActionNotifs = dtrActionNotifs;
                _config.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.ActionsNotifTT);

            if (ImGui.Checkbox(GSLoc.Settings.Preferences.VibeStatusLabel, ref dtrVibeStatus))
            {
                _config.Data.DtrVibeStatus = dtrVibeStatus;
                _config.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.VibeStatusTT);
        }

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.PrefThreeCharaAnonName, ref preferThreeCharaAnonName))
        {
            _config.Data.UseLegacyAnonName = preferThreeCharaAnonName;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.PrefThreeCharaAnonNameTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ShowProfilesLabel, ref showProfiles))
        {
            Mediator.Publish(new ClearUserProfileMessage(MainHub.OwnUserData));
            _config.Data.ShowProfiles = showProfiles;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ShowProfilesTT);

        using (ImRaii.Disabled(!showProfiles))
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);
            if (ImGui.SliderFloat(GSLoc.Settings.Preferences.ProfileDelayLabel, ref profileDelay, 0.3f, 5))
            {
                _config.Data.ProfileDelay = profileDelay;
                _config.Save();
            }
            CkGui.HelpText(GSLoc.Settings.Preferences.ProfileDelayTT);
            ImGui.Unindent();
        }

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ContextMenusLabel, ref showContextMenus))
        {
            _config.Data.ShowContextMenus = showContextMenus;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ContextMenusTT);

        /* --------------- Separator for moving onto the Notifications Section ----------- */
        ImGui.Separator();
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderNotifications, Fonts.SubtitleFont);

        var liveGarblerZoneChangeWarn = _config.Data.LiveGarblerZoneChangeWarn;
        var serverConnectionNotifs = _config.Data.ConnectionAlertLocation > 0;
        var onlineNotifs = _config.Data.OnlineAlertLocation > 0;
        var onlineNotifsNickLimited = _config.Data.OnlineNotifyFilter.HasAny(OnlineFilter.Nicknamed);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ZoneChangeWarnLabel, ref liveGarblerZoneChangeWarn))
        {
            _config.Data.LiveGarblerZoneChangeWarn = liveGarblerZoneChangeWarn;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ZoneChangeWarnTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.ConnectedNotifLabel, ref serverConnectionNotifs))
        {
            _config.Data.ConnectionAlertLocation = serverConnectionNotifs ? AlertLocation.Toast : AlertLocation.Nowhere;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.ConnectedNotifTT);

        if (ImGui.Checkbox(GSLoc.Settings.Preferences.OnlineNotifLabel, ref onlineNotifs))
        {
            _config.Data.OnlineAlertLocation = onlineNotifs ? AlertLocation.Toast : AlertLocation.Nowhere;
            _config.Save();
        }
        CkGui.HelpText(GSLoc.Settings.Preferences.OnlineNotifTT);

        using (ImRaii.Disabled(!onlineNotifs))
        {
            if (ImGui.Checkbox(GSLoc.Settings.Preferences.LimitForNicksLabel, ref onlineNotifsNickLimited))
            {
                if (onlineNotifsNickLimited)
                    _config.Data.OnlineNotifyFilter |= OnlineFilter.Nicknamed;
                else
                    _config.Data.OnlineNotifyFilter &= ~OnlineFilter.Nicknamed;
                _config.Save();
            }

            CkGui.HelpText(GSLoc.Settings.Preferences.LimitForNicksTT);
        }
    }

    private void DrawAlertLocations()
    {
        CkGui.FontText(GSLoc.Settings.Preferences.HeaderNotifications, Fonts.SubtitleFont);

        if (AlertLocationCombo("Incoming Requests##notifReq", _config.Data.RequestAlertLocation, out var newReq))
        {
            _config.Data.RequestAlertLocation = newReq;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for incoming requests appear.", true);

        if (AlertLocationCombo("Online Users##notifOnline", _config.Data.OnlineAlertLocation, out var newOnline))
        {
            _config.Data.OnlineAlertLocation = newOnline;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for these online users display.", true);

        if (AlertLocationCombo("Info Messages##notifInfo", _config.Data.InfoNotification, out var newInfo))
        {
            _config.Data.InfoNotification = newInfo;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where Plugin \"Info\" notifications will display.", true);

        if (AlertLocationCombo("Warnings##notifWarn", _config.Data.WarningNotification, out var newWarn))
        {
            _config.Data.WarningNotification = newWarn;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where Plugin \"Warning\" notifications will display.", true);

        if (AlertLocationCombo("Errors##notifError", _config.Data.ErrorNotification, out var newError))
        {
            _config.Data.ErrorNotification = newError;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where Plugin \"Error\" notifications will display.", true);
    }

    private void DrawOnlineUserOptions()
    {
        CkGui.FontText("Online Users", Fonts.SubtitleFont);

        if (AlertLocationCombo("Alert Location##notifOnline", _config.Data.OnlineAlertLocation, out var newOnline))
        {
            _config.Data.OnlineAlertLocation = newOnline;
            _config.Save();
        }
        CkGui.HelpTextFramed("Where notifications for these online users display.", true);

        ImGui.Spacing();
        ImGui.Text("Match Filters");
        CkGui.HelpText("Who to show online notifications for.", true);
        var pingFilter = _config.Data.OnlineNotifyFilter;
        var tempPairs = pingFilter.HasAny(OnlineFilter.Temporary);
        if (ImGui.Checkbox("Temporary", ref tempPairs))
        {
            _config.Data.OnlineNotifyFilter ^= OnlineFilter.Temporary;
            _config.Save();
        }
        var nicked = pingFilter.HasAny(OnlineFilter.Nicknamed);
        if (ImGui.Checkbox("Nicknamed", ref nicked))
        {
            _config.Data.OnlineNotifyFilter ^= OnlineFilter.Nicknamed;
            _config.Save();
        }
        var favorited = pingFilter.HasAny(OnlineFilter.Favorited);
        if (ImGui.Checkbox("Favorited", ref favorited))
        {
            _config.Data.OnlineNotifyFilter ^= OnlineFilter.Favorited;
            _config.Save();
        }

        ImGui.Spacing();
        ImGui.Text("Match Policy:");
        CkGui.HelpText("If we are notified when matching any condition, or all selected.", true);
        var pingPolicy = _config.Data.OnlineNotifyPolicy;
        if (ImGui.RadioButton("Any Filter", pingPolicy == FilterPolicy.MatchAny))
        {
            _config.Data.OnlineNotifyPolicy = FilterPolicy.MatchAny;
            _config.Save();
        }
        if (ImGui.RadioButton("All Selected Filters", pingPolicy == FilterPolicy.MatchAll))
        {
            _config.Data.OnlineNotifyPolicy = FilterPolicy.MatchAll;
            _config.Save();
        }
    }

    private bool AlertLocationCombo(string label, AlertLocation cur, out AlertLocation newValue, float width = 150f)
    {
        newValue = cur;
        ImGui.SetNextItemWidth(width);
        using var combo = ImUtf8.Combo(label, cur.ToString(), CFlags.None);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            newValue = AlertLocation.Nowhere;
            return true;
        }

        if (!combo) return false;

        // Draw out the selectables indivdually.
        if (ImGui.Selectable(AlertLocation.Nowhere.ToString(), AlertLocation.Nowhere == cur))
            newValue = AlertLocation.Nowhere;
        CkGui.AttachTooltip("Notifications will not be shown.");

        if (ImGui.Selectable(AlertLocation.Chat.ToString(), AlertLocation.Chat == cur))
            newValue = AlertLocation.Chat;
        CkGui.AttachTooltip("Notifications will be printed in chat.");

        if (ImGui.Selectable(AlertLocation.Toast.ToString(), AlertLocation.Toast == cur))
            newValue = AlertLocation.Toast;
        CkGui.AttachTooltip("Notifications will be shown in the bottom right corner.");

        if (ImGui.Selectable(AlertLocation.Both.ToString(), AlertLocation.Both == cur))
            newValue = AlertLocation.Both;
        CkGui.AttachTooltip("Notifications will be printed in chat and shown in the bottom right corner.");

        return newValue != cur;
    }

    private string? _tmpAlias;
    private string? _tmpDispName;
    private NativeUiColor? _tmpColors;

    private void DrawVanity()
    {
        CkGui.FontText("Vanity Benefits", Fonts.DefaultScaled);
        if (MainHub.OwnUserData is not { } userData)
            return;

        // do a Lazy assignment
        _tmpAlias ??= userData.Alias ?? string.Empty;
        _tmpDispName ??= userData.VanityName ?? string.Empty;
        // locally store the saved colors 
        var prevSaved = new NativeUiColor(Foreground: userData.Color ?? default);
        _tmpColors ??= prevSaved;

        var isDonor = userData.Tier is not CkVanityTier.NoRole;

        // Alias
        CkGui.TextUnderlined("Alias");
        CkGui.HelpText("Used in place of your UID for the displayed name.", true);

        if (VanityAliasChanged() && userData.Alias is not null)
            CkGui.ColorTextInline($"(Current: {userData.Alias})", ImGuiColors.DalamudViolet);

        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##vanity-alias", "Alias..", ref _tmpAlias, 15);
        var validAlias = IsValidName(_tmpAlias);
        if (!validAlias)
            CkGui.ColorTextWrapped("Must be 4-15 characters with no spaces (underscores & dashes allowed)", ImGuiColors.DalamudYellow);

        ImGui.Spacing();
        CkGui.TextUnderlined("Vanity Name");
        CkGui.HelpText("Displayed in place of Anon-User names for Radar, RadarGroup, and RadarChat.", true);
        if (VanityNameChanged() && userData.VanityName is not null)
            CkGui.ColorTextInline($" (Current: {userData.VanityName})", ImGuiColors.DalamudViolet);

        using (ImRaii.Disabled(!isDonor))
        {
            ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint("##vanity-name", "Vanity name..", ref _tmpDispName, 10);
        }
        CkGui.AttachTooltip("Only supporters can set a vanity name", isDonor);

        var validVanityName = IsValidVanityName(_tmpDispName);
        if (!validVanityName)
            CkGui.ColorTextWrapped("Must be 4-10 characters with no spaces", ImGuiColors.DalamudYellow);

        ImGui.Spacing();
        CkGui.TextUnderlined("Name Appearance");
        CkGui.HelpText("Colors all displays of your VanityName/Alias/UID", true);

        using (ImRaii.Disabled(!isDonor))
        {
            var colors = _tmpColors.Value;
            if (CkGuiUtils.ColorEditNativeForeground("DisplayName Color", ref colors, GsCol.VibrantPink.Uint(), new(Foreground: uint.MinValue)))
                _tmpColors = colors;
        }
        if (!isDonor)
            CkGui.ColorTextWrapped("Only supporters can edit their DisplayName Color!", CkCol.TriStateCross.Vec4());

        var canSubmit = VanityAnythingChanged() && validAlias && validVanityName && !UiService.DisableUI;
        if (CkGui.IconTextButton(FAI.Sync, "Update UserData", disabled: !canSubmit || !MainHub.IsConnectionDataSynced))
        {
            UiService.SetUITask(async () =>
            {
                var aliasUpdate = GetAliasUpdate();
                var vanityUpdate = GetVanityNameUpdate();
                var colorUpdate = GetColorUpdate();
                var dto = new UserDataUpdate(aliasUpdate, vanityUpdate, colorUpdate, null);

                var ret = await _hub.UserUpdateData(dto).ConfigureAwait(false);
                if (ret.ErrorCode is not GagSpeakApiEc.Success)
                {
                    _logger.LogWarning($"Failed to set new VanityData: {ret.ErrorCode}");
                    _tmpDispName = null;
                    _tmpAlias = null;
                    _tmpColors = null;
                    return;
                }
                // Update local state conditionally.
                // If the update string was empty, set it to null locally. 
                // If it was null, retain the existing local value.
                var newUserData = MainHub.ConnectionResponse!.User with
                {
                    Alias = aliasUpdate != null ? (aliasUpdate == string.Empty ? null : aliasUpdate) : userData.Alias,
                    VanityName = vanityUpdate != null ? (vanityUpdate == string.Empty ? null : vanityUpdate) : userData.VanityName,
                    Color = colorUpdate ?? userData.Color,
                };
                MainHub.ConnectionResponse = MainHub.ConnectionResponse with
                {
                    User = newUserData
                };
                _tmpDispName = null;
                _tmpAlias = null;
                _tmpColors = null;
            });
        }

        string? GetAliasUpdate()
        {
            var current = userData.Alias ?? string.Empty;
            var tmp = _tmpAlias ?? string.Empty;
            return current == tmp ? null : tmp;
        }

        string? GetVanityNameUpdate()
        {
            var current = userData.VanityName ?? string.Empty;
            var tmp = _tmpDispName ?? string.Empty;
            return current == tmp ? null : tmp;
        }

        uint? GetColorUpdate()
        {
            return !_tmpColors.Value.Foreground.Equals(prevSaved.Foreground) ? _tmpColors.Value.Foreground : null;
        }

        bool VanityAliasChanged()
            => GetAliasUpdate() is not null;

        bool VanityNameChanged()
            => GetVanityNameUpdate() is not null;

        bool VanityColorChanged()
            => GetColorUpdate() is not null;

        bool VanityAnythingChanged()
            => VanityAliasChanged() || VanityNameChanged() || VanityColorChanged();

        bool IsValidName(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;

            return value.Length is >= 4 and <= 15 &&
                   value.All(c => char.IsLetterOrDigit(c) || c is '_' or '-');
        }

        bool IsValidVanityName(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return true;
            return value.Length is >= 4 and <= 10 && value.All(c => char.IsLetterOrDigit(c));
        }
    }

    private void DrawAudioOptions<T>(string id, IAudioConfig<T> config) where T : IAudioConfigData
    {
        var hasAudioAlerts = config.Data.AlertKind.HasAny(AlertKind.Audio);
        var isCustom = config.Data.AlertIsCustom;

        using var dis = ImRaii.Disabled(!hasAudioAlerts);
        using var ident = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            // Audio Type selection
            ImGui.SetNextItemWidth(125 * ImGuiHelpers.GlobalScale);
            int soundType = config.Data.AlertIsCustom ? 1 : 0;
            if (ImGui.Combo($"##audio-type", ref soundType, "Game Sound\0Custom Sound\0"))
            {
                config.Data.AlertIsCustom = soundType == 1;
                config.UpdateAudio();
            }
            CkGui.AttachTooltip("The type of audio to be played.");

            // Sampler
            CkGui.FrameSeparatorV();
            if (CkGui.IconTextButton(FAI.Play, "Test Notification Sound", disabled: !hasAudioAlerts))
                config.PlaySound();
        }
        var soundRowWidth = ImGui.GetItemRectSize().X;

        if (!isCustom)
        {
            var curGamesound = config.Data.AlertSoundbyte;
            if (CkGuiUtils.EnumCombo($"##gamesounds", soundRowWidth, curGamesound, out var newSound, _ => _.ToName(), flags: CFlags.None))
            {
                config.Data.AlertSoundbyte = newSound;
                unsafe { UIGlobals.PlaySoundEffect((uint)newSound); }
                config.UpdateAudio();
            }
            CkGui.AttachTooltip("The native soundbyte to play when receiving a mention");
            return;
        }

        DrawFolderPickerButton((newPath) =>
        {
            config.Data.AlertCustomPath = newPath;
            config.Save();
            config.UpdateAudio();
        });
        ImUtf8.SameLineInner();

        // Draw the custom path if custom.
        var soundInvalid = config.Data.AlertIsCustom && !config.IsAudioReady();
        using (ImRaii.PushColor(ImGuiCol.Border, 0xFF0000FF, soundInvalid))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2, soundInvalid))
        {
            var path = config.Data.AlertCustomPath;
            ImGui.SetNextItemWidth(soundRowWidth - ImUtf8.FrameHeight - ImUtf8.ItemInnerSpacing.X);
            if (ImGui.InputTextWithHint($"##custom-path", "Sound File Path..", ref path, 256))
            {
                config.Data.AlertCustomPath = path;
                config.Save();
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
                config.UpdateAudio();
        }
        CkGui.AttachTooltip(soundInvalid ? "--COL--Sound Path Invalid!--COL--"
            : "The filepath to the custom audio file.", ImGuiColors.DalamudRed);

        var volume = config.Data.AlertVolume;
        ImGui.SetNextItemWidth(soundRowWidth);
        if (ImGui.SliderFloat($"##volume", ref volume, 0, 1, $"Volume: {volume * 100:F1}%%"))
        {
            config.Data.AlertVolume = volume;
            config.UpdateAudio();
        }
        CkGui.AttachTooltip("How loud the custom sound is in playback");
    }

    private void DrawFolderPickerButton(Action<string> onSelected)
    {
        if (CkGui.IconButton(FAI.FolderOpen))
        {
            if (_isLinux)
                OpenDalamudAudioDialog(onSelected);
            else
                ImGui.OpenPopup("audio-import-options");
        }
        CkGui.AttachTooltip("Browse for an audio file (.mp3, .wav)");

        // Fancy dropdown options for Windows users
        var min = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        var popUpPos = min + new Vector2(0, size.Y);
        ImGui.SetNextWindowPos(popUpPos);
        ImGui.SetNextWindowSize(new Vector2(250 * ImGuiHelpers.GlobalScale, ImUtf8.FrameHeightSpacing + size.Y + 8 * ImGuiHelpers.GlobalScale));

        using var s = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f)
            .Push(ImGuiStyleVar.PopupRounding, 5f)
            .Push(ImGuiStyleVar.WindowPadding, ImGuiHelpers.ScaledVector2(4f));

        using var _ = ImRaii.Popup("audio-import-options", WFlags.NoMove | WFlags.NoResize | WFlags.NoCollapse | WFlags.NoScrollbar);
        if (!_) return;

        if (CkGui.IconTextButton(FAI.FolderOpen, "Import via FileDialog", 240 * ImGuiHelpers.GlobalScale, true))
        {
            OpenDalamudAudioDialog(onSelected);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip("Opens Dalamuds FileDialog window to select a file from.");

        if (CkGui.IconTextButton(FAI.FolderOpen, "Import via File Explorer", 240 * ImGuiHelpers.GlobalScale, true, _isLinux))
        {
            OpenWindowsAudioExplorer(onSelected);
            ImGui.CloseCurrentPopup();
        }
        CkGui.AttachTooltip("Open Windows File Explorer to select an audio file.");
    }

    private void OpenDalamudAudioDialog(Action<string> onSuccess)
    {
        // Filter strictly to mp3 and wav
        _fileDialog.OpenSingleFilePicker("Select Custom Alert Sound", ".mp3,.wav",
            (success, file) => { if (success) onSuccess?.Invoke(file); });
    }

    private void OpenWindowsAudioExplorer(Action<string> onSuccess, string? directory = null)
    {
        var thread = new Thread(() =>
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*";
                    dialog.Title = "Select Custom Alert Sound";

                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        dialog.InitialDirectory = directory;

                    if (dialog.ShowDialog() is DialogResult.OK)
                    {
                        Svc.Logger.Information($"Selected audio file {dialog.FileName}");
                        onSuccess?.Invoke(dialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Svc.Logger.Error($"There was an error while opening the File Browser: {ex.Message}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }
}
