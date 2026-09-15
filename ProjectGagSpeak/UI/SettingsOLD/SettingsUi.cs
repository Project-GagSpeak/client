//using CkCommons;
//using CkCommons.GarblerCore;
//using CkCommons.Gui;
//using CkCommons.Gui.Utility;
//using Dalamud.Bindings.ImGui;
//using Dalamud.Interface.Colors;
//using Dalamud.Interface.Utility;
//using Dalamud.Interface.Utility.Raii;
//using GagSpeak.GameInternals.Agents;
//using GagSpeak.Interop;
//using GagSpeak.Interop.Helpers;
//using GagSpeak.Localization;
//using GagSpeak.PlayerClient;
//using GagSpeak.Services;
//using GagSpeak.Services.Configs;
//using GagSpeak.Services.Mediator;
//using GagSpeak.State.Listeners;
//using GagSpeak.Utils;
//using GagSpeak.WebAPI;
//using GagspeakAPI.Attributes;
//using GagspeakAPI.Data.Permissions;
//using GagspeakAPI.Hub;
//using GagspeakAPI.User;
//using OtterGui;
//using OtterGui.Text;
//using System.Windows.Forms;
//using FFXIVClientStructs.FFXIV.Client.UI;

//namespace GagSpeak.Gui;

//public class SettingsUi : WindowMediatorSubscriberBase
//{
//    private readonly MainHub _hub;
//    private readonly MainConfig _config;
//    private readonly ProfilesTab _accountsTab;
//    private readonly DebugTab _debugTab;
//    private readonly PiShockProvider _shockProvider;
//    private readonly ClientDataListener _clientDatListener;
//    private readonly PluginGuideProvider _guideProvider;
//    private readonly UiFileDialogService _fileDialog;
//    private readonly HardcoreEscapeService _escape;

//    private static bool _isLinux;
//    private OptionalPlugin _expandedInfo = OptionalPlugin.None;

//    public SettingsUi(ILogger<SettingsUi> logger, GagspeakMediator mediator, MainHub hub,
//        MainConfig config, ProfilesTab accounts, DebugTab debug, PiShockProvider shockProvider,
//        ClientDataListener listener, PluginGuideProvider guide, UiFileDialogService fileDialog,
//        HardcoreEscapeService escape)
//        : base(logger, mediator, "GagSpeak Settings")
//    {
//        _hub = hub;
//        _config = config;
//        _accountsTab = accounts;
//        _debugTab = debug;
//        _shockProvider = shockProvider;
//        _clientDatListener = listener;
//        _guideProvider = guide;
//        _fileDialog = fileDialog;
//        _escape = escape;

//        Flags = WFlags.NoScrollbar;
//        this.PinningClickthroughFalse();
//        this.SetBoundaries(new Vector2(625, 400), ImGui.GetIO().DisplaySize);
//        _isLinux = Util.IsWine();

//        TitleBarButtons = new TitleBarButtonBuilder()
//            .Add(FAI.Tshirt, "Open Active State Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugActiveStateUI))))
//            .Add(FAI.PersonRays, "Open Personal Data Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugPersonalDataUI))))
//            .Add(FAI.Database, "Open Storages Debugger", () => Mediator.Publish(new UiToggleMessage(typeof(DebugStorageUI))))
//            .Add(FAI.Bell, "Actions Notifier", () => Mediator.Publish(new UiToggleMessage(typeof(InteractionEventsUI))))
//            .Build();
//    }

//    protected override void DrawInternal()
//    {
//        var minPos = ImGui.GetCursorPos();
//        var rWidth = ImGui.CalcTextSize("Configs").X + ImUtf8.FrameHeight + ImUtf8.ItemSpacing.X * 4;
//        var leftLength = ImGui.GetContentRegionAvail().X - rWidth;
//        var buttonPos = minPos + new Vector2(leftLength, 0);
//        using (ImRaii.Group())
//        {
//            ImGui.Text(GSLoc.Settings.OptionalPlugins);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("Sundouleia", IpcCallerSundouleia.APIAvailable, OptionalPlugin.Sundouleia, true);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("Penumbra", IpcCallerPenumbra.APIAvailable, OptionalPlugin.Penumbra, true);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("Glamourer", IpcCallerGlamourer.APIAvailable, OptionalPlugin.Glamourer, true);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("CPlus", IpcCallerCustomize.APIAvailable, OptionalPlugin.CustomizePlus, false);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("Loci", IpcCallerLoci.APIAvailable, OptionalPlugin.Loci, true);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("Lifestream", IpcCallerLifestream.APIAvailable, OptionalPlugin.Lifestream, false);
//            ImGui.SameLine();
//            DrawOptionalPluginButton("Intiface", IpcCallerIntiface.APIAvailable, OptionalPlugin.Intiface, false);

//            // Below it, draw out the plugin details if we should.
//            _guideProvider.DrawOptionalPluginDetails(_expandedInfo, leftLength);

//            ImGui.Text(GSLoc.Settings.AccountClaimText);
//            ImGui.SameLine();
//            if (ImUtf8.SmallButton("CK Discord"))
//                Util.OpenLink("https://discord.gg/kinkporium");
//        }

//        // draw out the tab bar for us.
//        if (ImGui.BeginTabBar("mainTabBar"))
//        {
//            if (MainHub.IsConnected)
//            {
//                if (ImGui.BeginTabItem(GSLoc.Settings.TabsGlobal))
//                {
//                    DrawGlobalSettings();
//                    ImGui.EndTabItem();
//                }
//            }
//            ImGui.EndTabBar();
//        }

//        ImGui.SetCursorPos(buttonPos);
//        using (ImRaii.Group())
//        {
//            if (CkGui.FancyButton(FAI.Palette, "Styler", rWidth, false))
//                Mediator.Publish(new UiToggleMessage(typeof(StyleEditorUI)));
//            CkGui.AttachTooltip("Edit Style (very WIP and incomplete, use at your own risk)");

//            if (CkGui.FancyButton(FAI.Folder, "Configs", rWidth, false))
//            {
//                try { Process.Start(new ProcessStartInfo { FileName = GsFiles.ConfigDirectory, UseShellExecute = true }); }
//                catch (Bagagwa e) { Svc.Logger.Error($"Failed to open the config directory. {e.Message}"); }
//            }
//            CkGui.AttachTooltip("Opens the Config Folder.--NL--(Useful for debugging)");
//        }
//    }

//    private void DrawOptionalPluginButton(string name, bool apiAvailable, OptionalPlugin plugin, bool recommended, string tooltip = "Click to see more info!")
//    {
//        var showWarn = !apiAvailable && recommended;
//        using (ImRaii.Group())
//        {
//            CkGui.ColorTextBool(name, apiAvailable);
//            // Show yellow caution if unavailable
//            if (showWarn)
//            {
//                ImGui.SameLine(0, 1);
//                CkGui.ColorText("⚠", ImGuiColors.DalamudYellow);
//            }
//        }

//        var ttText = showWarn ? $"{tooltip}--SEP----COL--Recommended plugin {plugin} is not installed or up to date!--COL--" : tooltip;
//        CkGui.AttachTooltip(ttText, ImGuiColors.DalamudYellow);

//        // If this is our currently expanded plugin, draw a rect ring around it in yellow.
//        if (_expandedInfo == plugin)
//        {
//            var min = ImGui.GetItemRectMin() - ImUtf8.FramePadding;
//            var max = ImGui.GetItemRectMax() + ImUtf8.FramePadding;
//            ImGui.GetWindowDrawList().AddRect(min, max, CkCol.Favorite.Uint(), 5f, DFlags.RoundCornersTop, 2);
//        }

//        // Otherwise, if clicked, toggle the info.
//        if (ImGui.IsItemClicked())
//            _expandedInfo = (_expandedInfo == plugin) ? OptionalPlugin.None : plugin;
//    }

//    private void AssignGlobalPermChangeTask(GlobalPerms perms, string globalKey, object newValue)
//        => UiService.SetUITask(async () => await PermHelper.ChangeOwnGlobal(_hub, perms, globalKey, newValue));

//    private void AssignShockPermBulkTask(GlobalPerms perms, GlobalPerms updated)
//        => UiService.SetUITask(async () =>
//        {
//            if (ClientData.IsNull) return;
//            var res = await _hub.UserBulkChangeGlobal(new(MainHub.OwnUserData, updated, ClientData.HardcoreClone() ?? new HardcoreState()));
//            if (res.ErrorCode is GagSpeakApiEc.Success)
//                _clientDatListener.ChangeAllGlobalPerms(updated);
//        });
//}
